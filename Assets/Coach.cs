using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using Barracuda;

namespace Coach
{
    public struct ImageDims
    {
        public int InputSize { get; set; }
        public int ImageMean { get; set; }
        public float ImageStd { get; set; }

        public ImageDims(int inputSize, int imageMean, float imageStd)
        {
            this.InputSize = inputSize;
            this.ImageMean = imageMean;
            this.ImageStd = imageStd;
        }
    }

    public static class ImageUtil
    {
        public static Texture2D TextureFromFile(string filePath)
        {
            return TextureFromBytes(File.ReadAllBytes(filePath));
        }

        public static Texture2D TextureFromBytes(byte[] input)
        {
            Texture2D tex = new Texture2D(0, 0);
            tex.LoadImage(input);

            return tex;
        }

        public static Tensor TensorFromTexture(Texture2D image, ImageDims dims)
        {
            Tensor imageTensor = new Tensor(image);
            return imageTensor.Reshape(
                new TensorShape(1, dims.InputSize, dims.InputSize, 3)
            );
        }
    }

    public class CoachResult
    {
        public List<LabelProbability> Results { get; private set; }
        public List<LabelProbability> SortedResults { get; private set; }

        public CoachResult(string[] labels, Tensor output)
        {
            for (var i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                float probability = output[i];

                Results.Add(new LabelProbability()
                {
                    Label = label,
                    Confidence = probability
                });
            }
            SortedResults = Results.OrderByDescending(r => r.Confidence).ToList();
        }

        public LabelProbability Best()
        {
            return SortedResults.FirstOrDefault();
        }

        public LabelProbability Worst()
        {
            return SortedResults.LastOrDefault();
        }
    }

    public struct LabelProbability
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
    }

    public class CoachModel
    {
        private readonly float COACH_VERSION = 1f;

        private string[] Labels { get; set; }
        private ImageDims ImageDims { get; set; }

        private IWorker Worker { get; set; }

        public CoachModel(Model model, string[] labels, string module, float coachVersion)
        {
            if (COACH_VERSION != coachVersion)
            {
                throw new Exception($"Coach model v{coachVersion} incompatible with SDK version {COACH_VERSION}");
            }

            Worker = BarracudaWorkerFactory.CreateWorker(BarracudaWorkerFactory.Type.Compute, model);

            this.Labels = labels;

            int size = int.Parse(module.Substring(module.Length - 3, 3));
            // TODO: Normalization
            this.ImageDims = new ImageDims(size, 0, 1);
        }

        private Tensor ReadTensorFromTexture(Texture2D texture)
        {
            return ImageUtil.TensorFromTexture(texture, this.ImageDims);
        }

        private Tensor ReadTensorFromBytes(byte[] image)
        {
            var texture = ImageUtil.TextureFromBytes(image);
            return ImageUtil.TensorFromTexture(texture, this.ImageDims);
        }

        private Tensor ReadTensorFromFile(string filePath)
        {
            var texture = ImageUtil.TextureFromFile(filePath);
            return ImageUtil.TensorFromTexture(texture, this.ImageDims);
        }

        public CoachResult Predict(Texture2D texture, string inputName = "input", string outputName = "output")
        {
            var imageTensor = ReadTensorFromTexture(texture);
            return GetModelResult(imageTensor, inputName, outputName);
        }

        public CoachResult Predict(string image, string inputName = "input", string outputName = "output")
        {
            var imageTensor = ReadTensorFromFile(image);
            return GetModelResult(imageTensor, inputName, outputName);
        }

        public CoachResult Predict(byte[] image, string inputName = "input", string outputName = "output")
        {
            var imageTensor = ReadTensorFromBytes(image);
            return GetModelResult(imageTensor, inputName, outputName);
        }

        private CoachResult GetModelResult(Tensor imageTensor, string inputName = "input", string outputName = "output")
        {
            var inputs = new Dictionary<string, Tensor>();
            inputs.Add(inputName, imageTensor);

            // Await execution
            Worker.Execute(inputs);

            // Get the output
            var output = Worker.Fetch(outputName);
            output.Dispose();

            return new CoachResult(Labels, output);
        }

        public void CleanUp()
        {
            Worker.Dispose();
        }
    }

    public struct ModelDef
    {
        public string name { get; set; }
        public string status { get; set; }
        public int version { get; set; }
        public string module { get; set; }
        public string[] labels { get; set; }
        public float coachVersion { get; set; }
    }

    public class Profile
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("bucket")]
        public string Bucket { get; set; }

        [JsonProperty("models")]
        public ModelDef[] Models { get; set; }
    }

    public enum ModelType
    {
        Frozen, Unity, Mobile
    }

    public class CoachClient
    {
        public bool IsDebug { get; private set; }
        private Profile Profile { get; set; }
        private string ApiKey { get; set; }

        public CoachClient(bool isDebug = false)
        {
            this.IsDebug = isDebug;
        }

        public async Task<CoachClient> Login(string apiKey)
        {
            if (apiKey == String.Empty)
            {
                throw new Exception("Invalid API Key");
            }
            this.ApiKey = apiKey;
            this.Profile = await GetProfile();

            return this;
        }

        private bool IsAuthenticated()
        {
            return this.Profile != null;
        }

        private ModelDef ReadManifest(string path)
        {
            return JsonConvert.DeserializeObject<ModelDef>(File.ReadAllText(path));
        }

        private async Task<Profile> GetProfile()
        {
            var id = this.ApiKey.Substring(0, 5);
            var url = $"https://2hhn1oxz51.execute-api.us-east-1.amazonaws.com/prod/{id}";

            var request = new HttpClient();
            request.DefaultRequestHeaders.Add("X-Api-Key", this.ApiKey);

            var response = await request.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            var profile = JsonConvert.DeserializeObject<Profile>(responseBody);
            return profile;
        }

        public async Task CacheModel(string modelName, string path = ".", bool skipMatch = true, ModelType modelType = ModelType.Frozen)
        {
            if (!IsAuthenticated())
                throw new Exception("User is not authenticated");

            ModelDef model = this.Profile.Models.Single(m => m.name == modelName);
            int version = model.version;

            string modelDir = Path.Combine(path, modelName);
            string profileManifest = Path.Combine(modelDir, "manifest.json");

            if (File.Exists(profileManifest))
            {
                // Load existing model manifest
                ModelDef manifest = ReadManifest(profileManifest);

                int manifestVersion = manifest.version;
                int profileVersion = model.version;

                if (profileVersion == manifestVersion && skipMatch)
                {
                    if (this.IsDebug)
                    {
                        Console.WriteLine("Version match, skipping model download");
                    }
                    return;
                }
            }
            else if (!Directory.Exists(modelDir))
            {
                Directory.CreateDirectory(modelDir);
            }

            // Write downloaded manifest
            var json = JsonConvert.SerializeObject(model);
            File.WriteAllText(profileManifest, json);

            var baseUrl = $"https://la41byvnkj.execute-api.us-east-1.amazonaws.com/prod/{this.Profile.Bucket}/model-bin?object=trained/{modelName}/{version}/model";

            var modelFile = "unity.bytes";
            var modelUrl = $"{baseUrl}/{modelFile}";

            var request = new HttpClient();
            request.DefaultRequestHeaders.Add("X-Api-Key", this.ApiKey);
            request.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, modelUrl);
            requestMessage.Content = new StringContent(String.Empty, Encoding.UTF8, "application/octet-stream");

            var response = await request.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            byte[] modelBytes = await response.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(Path.Combine(path, modelName, modelFile), modelBytes);
        }

        public CoachModel GetModel(string path)
        {
            var modelPath = Path.Combine(path, "unity.bytes");
            var labelPath = Path.Combine(path, "manifest.json");

            // Load the model
            Model model = ModelLoader.LoadFromStreamingAssets(modelPath);

            var manifest = ReadManifest(labelPath);

            string[] labels = manifest.labels;
            string baseModule = manifest.module;
            float coachVersion = manifest.coachVersion;

            return new CoachModel(model, labels, baseModule, coachVersion);
        }

        public async Task<CoachModel> GetModelRemote(string modelName, string path = ".")
        {
            await CacheModel(modelName, path);
            return GetModel(Path.Combine(modelName, path));
        }
    }
}