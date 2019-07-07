using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Barracuda;

public class CoachController : MonoBehaviour
{
    public Webcam Webcam;

    public void TakePhoto()
    {
        Texture2D image = Webcam.GetPhoto();
        Tensor imageTensor = new Tensor(image);

        // Load barricuda model
        var model = ModelLoader.Load("flowers.bytes");
        var worker = BarracudaWorkerFactory.CreateWorker(BarracudaWorkerFactory.Type.ComputePrecompiled, model);

        var inputs = new Dictionary<string, Tensor>();
        inputs.Add("myinput", imageTensor);

        worker.Execute(inputs);
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
