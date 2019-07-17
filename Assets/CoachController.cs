using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Barracuda;
using UnityEngine.UI;

public class CoachController : MonoBehaviour
{
    private IWorker Worker { get; set; }
    private Tensor Output { get; set; }

    public RawImage Image;

    public void TakePhoto()
    {
        var inputs = new Dictionary<string, Tensor>();

        Texture2D image = Image.texture as Texture2D;
        Tensor imageTensor = new Tensor(image);
        inputs.Add("lambda_input_input", imageTensor);

        // Await execution
        Worker.Execute(inputs);

        // Get the output
        Output = Worker.Fetch("softmax_input/Softmax");
        Debug.LogWarning(Output[0]);
    }


    // Start is called before the first frame update
    void Start()
    {
        // Load the model and spawn the worker
        var model = ModelLoader.LoadFromStreamingAssets("flowers.bytes");
        Worker = BarracudaWorkerFactory.CreateWorker(BarracudaWorkerFactory.Type.Compute, model);
    }

    void Destroy() {
        Output.Dispose();
        Worker.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
