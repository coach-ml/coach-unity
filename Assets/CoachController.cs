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
        var coach = await new CoachClient().Login("");
        await coach.CacheModel("flowers");

        // var coach = new CoachClient();
        Model = coach.GetModel("flowers");
    }

    void Destroy() {
        Model.CleanUp();
    }
}