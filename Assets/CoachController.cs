using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Barracuda;

public class CoachController : MonoBehaviour
{
    public Webcam Webcam;
    private Dictionary<string, Tensor> Inputs { get; set; }
    private IWorker Worker { get; set; }
    private Tensor Output { get; set; }

    public void TakePhoto()
    {
        Texture2D image = Webcam.GetPhoto();
        Tensor imageTensor = new Tensor(image);
        Inputs.Add("import/lambda_input_input", imageTensor);

        // Await execution
        Worker.Execute(Inputs);

        // Get the output
        Output = Worker.Fetch("import/softmax_input/Softmax");
        Debug.Log(Output[0]);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Load the model and spawn the worker
        var model = ModelLoader.LoadFromStreamingAssets("flowers.bytes");
        Worker = BarracudaWorkerFactory.CreateWorker(BarracudaWorkerFactory.Type.Compute, model);
        
        Inputs = new Dictionary<string, Tensor>();
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
