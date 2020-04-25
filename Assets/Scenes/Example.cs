using Coach;
using UnityEngine;
using UnityEngine.UI;

public class Example : MonoBehaviour
{
    private CoachModel model;
    private Texture2D texture;

    public RawImage image;
    public Text resultLabel;
    
    async void Start()
    {
        var coach = new CoachClient(isDebug: true);
        coach = await coach.Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("flowers");

        texture = image.texture as Texture2D;
    }

    private void OnDestroy()
    {
        model.CleanUp();
    }

    public void evaluateRose()
    {
        var result = model.Predict(this.texture).Best();
        resultLabel.text = $"{result.Label}: {result.Confidence.ToString()}";
    }
}
