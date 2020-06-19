using Coach;
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

    bool sync = true;
    
    async void Start()
    {
        await init();
    }

    async Task init()
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

    public void toggleSync()
    {
        sync = !sync;
        buttonText.text = "Using " + (sync ? "sync" : "async");

        model.CleanUp();
        init();
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
