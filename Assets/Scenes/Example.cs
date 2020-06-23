using Coach;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class Example : MonoBehaviour
{
    private CoachModel model;
    private Texture2D texture;

    public RawImage image;
    public Text resultLabel;
    public Text buttonText;

    public List<Texture> textures;
    
    int index = 0;
    public Text paginationLabel;

    bool sync = false;
    
    async void Start()
    {
        image.texture = textures[0];
        paginationLabel.text = $"1/{this.textures.Count}";
        await Init();
    }

    async Task Init()
    {
        var coach = new CoachClient(isDebug: true);
        coach = await coach.Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("flowers", workers: 4);

        texture = image.texture as Texture2D;
        buttonText.text = "Using " + (sync ? "sync" : "async");
    }

    private void OnDestroy()
    {
        model.CleanUp();
    }

    public void ToggleSync()
    {
        sync = !sync;
        buttonText.text = "Using " + (sync ? "sync" : "async");

        model.CleanUp();
        Init();
    }

    public void MoveRight()
    {
        if (index + 1 < this.textures.Count) {
            index++;
            paginationLabel.text = $"{index + 1}/{this.textures.Count}";
            image.texture = this.textures[index];

            texture = image.texture as Texture2D;
        }
    }

    public void MoveLeft()
    {
        if (index - 1 >= 0)
        {
            index--;
            paginationLabel.text = $"{index + 1}/{this.textures.Count}";
            image.texture = this.textures[index];

            texture = image.texture as Texture2D;
        }
    }

    private void Update()
    {
        if (this.model != null && this.texture != null)
        {
            CoachResult prediction;
            if (sync)
            {
                prediction = model.Predict(this.texture);
            }
            else
            {
                prediction = model.GetPredictionResultAsync();
                StartCoroutine(model.PredictAsync(this.texture));
            }

            if (prediction != null)
            {
                var result = prediction.Best();
                var printedResult = $"{result.Label}: {result.Confidence.ToString()}";
                Debug.Log((sync ? "sync" : "async") + " || " + printedResult);
                resultLabel.text = printedResult;
            }
        }
    }
}