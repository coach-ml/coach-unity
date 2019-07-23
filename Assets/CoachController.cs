using UnityEngine;
using UnityEngine.UI;
using Coach;

public class CoachController : MonoBehaviour
{
    public RawImage Image;
    private CoachModel Model { get; set; }
    public void TakePhoto()
    {
        var results = Model.Predict(Image.texture as Texture2D);
        var best = results.Best();
        Debug.Log(best.Label + ": " + best.Confidence);
    }

    // Start is called before the first frame update
    async void Start()
    {
        var coach = await new CoachClient().Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        await coach.CacheModel("flowers");

        // var coach = new CoachClient();
        Model = coach.GetModel("flowers");
    }

    void Destroy() {
        Model.CleanUp();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public static class TextureTools
{
    /// <summary>
    /// Scales the texture data of the given texture.
    /// </summary>
    /// <param name="tex">Texure to scale</param>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    /// <param name="mode">Filtering mode</param>
    public static Texture2D Scale(this Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        GpuScale(tex, width, height, mode);

        // Update new texture
        tex.Resize(width, height);
        tex.ReadPixels(texR, 0, 0, true);
        tex.Apply(true);

        return tex;
    }

    // Internal unility that renders the source texture into the RTT - the scaling method itself.
    static void GpuScale(Texture2D src, int width, int height, FilterMode fmode)
    {
        //We need the source texture in VRAM because we render with it
        src.filterMode = fmode;
        src.Apply(true);

        //Using RTT for best quality and performance. Thanks, Unity 5
        RenderTexture rtt = new RenderTexture(width, height, 32);

        //Set the RTT in order to render to it
        Graphics.SetRenderTarget(rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix(0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
    }

    /*public static TFTensor ToTensor(this Texture2D tex)
    {
        var pic = tex.GetPixels32();
        Object.Destroy(tex);

        const int INPUT_SIZE = 128;
        const int IMAGE_MEAN = 128;
        const float IMAGE_STD = 128;

        float[] floatValues = new float[(INPUT_SIZE * INPUT_SIZE) * 3];

        for (int i = 0; i < pic.Length; i++)
        {
            var color = pic[i];

            floatValues[i * 3] = (color.r - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
        }

        Barracuda.TensorShape shape = new TensorShape(1, INPUT_SIZE, INPUT_SIZE, 3);
        return new Tensor()

        TFShape shape = new TFShape(1, INPUT_SIZE, INPUT_SIZE, 3);
        return TFTensor.FromBuffer(shape, floatValues, 0, floatValues.Length);
    }*/
}