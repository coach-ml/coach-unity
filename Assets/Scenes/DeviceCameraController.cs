using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using Coach;
using System.Threading.Tasks;

public class DeviceCameraController : MonoBehaviour
{
    public RawImage image;
    public RectTransform imageParent;
    public AspectRatioFitter imageFitter;
    public Text resultLabel;

    // Device cameras
    WebCamDevice activeCameraDevice;
    WebCamTexture activeCameraTexture;

    // Image rotation
    Vector3 rotationVector = new Vector3(0f, 0f, 0f);

    // Image uvRect
    Rect defaultRect = new Rect(0f, 0f, 1f, 1f);
    Rect fixedRect = new Rect(0f, 1f, 1f, -1f);

    // Image Parent's scale
    Vector3 defaultScale = new Vector3(1f, 1f, 1f);
    Vector3 fixedScale = new Vector3(-1f, 1f, 1f);

    IEnumerator Start()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        // Check for device cameras
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.Log("No devices cameras found");
            yield break;
        }

        // Get the device's cameras and create WebCamTextures with them
#if UNITY_EDITOR
        if (WebCamTexture.devices.Any(d => d.name.Contains("Remote")))
        {
            activeCameraDevice = WebCamTexture.devices.Last(d => d.name.Contains("Remote"));
        }
        else
        {
            activeCameraDevice = WebCamTexture.devices.Last();
        }
#else
        activeCameraDevice = WebCamTexture.devices.Last();

        foreach (var cDevice in WebCamTexture.devices)
        {
            if (activeCameraDevice.isFrontFacing && !cDevice.isFrontFacing)
            {
                activeCameraDevice = cDevice;
            }
        }
#endif

        var camTexture = new WebCamTexture(activeCameraDevice.name);

        // Set camera filter modes for a smoother looking image
        camTexture.filterMode = FilterMode.Trilinear;

        // Set the camera to use by default
        SetActiveCamera(camTexture);

        StartCoach();
    }


    private CoachModel model;
    private Texture2D texture;
    public async Task StartCoach()
    {
        var coach = new CoachClient(isDebug: true);
        coach = await coach.Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("flowers", workers: 4);

        texture = image.texture as Texture2D;
    }
    
    public void evaluate()
    {
        if (this.texture != null)
        {
            var result = model.Predict(this.texture).Best();
            resultLabel.text = $"{result.Label}: {result.Confidence.ToString()}";
        }
    }

    public Texture2D GetWebcamPhoto()
    {
        if (activeCameraTexture != null)
        {
            Texture2D photo = new Texture2D(activeCameraTexture.width, activeCameraTexture.height);
            photo.SetPixels(activeCameraTexture.GetPixels());
            photo.Apply();

            return photo;
        }

        return null;
    }

    public void Play()
    {
        if (activeCameraTexture != null)
        {
            activeCameraTexture.Play();
        }
    }

    public void Stop()
    {
        if (activeCameraTexture != null)
        {
            activeCameraTexture.Stop();
        }
    }

    // Set the device camera to use and start it
    public void SetActiveCamera(WebCamTexture cameraToUse)
    {
        if (activeCameraTexture != null)
        {
            activeCameraTexture.Stop();
        }

        activeCameraTexture = cameraToUse;
        activeCameraDevice = WebCamTexture.devices.FirstOrDefault(device =>
            device.name == cameraToUse.deviceName);

        image.texture = activeCameraTexture;
        //image.material.mainTexture = activeCameraTexture;

        activeCameraTexture.Play();
    }

    // Make adjustments to image every frame to be safe, since Unity isn't 
    // guaranteed to report correct data as soon as device camera is started

    void Update()
    {
        // Skip making adjustment for incorrect camera data
        if (activeCameraTexture.width < 100)
        {
            return;
        }

        // Rotate image to show correct orientation 
        rotationVector.z = -activeCameraTexture.videoRotationAngle;
        image.rectTransform.localEulerAngles = rotationVector;

        //image.rectTransform.sizeDelta = new Vector2(image.rectTransform.sizeDelta.x, Screen.height);

        // Set AspectRatioFitter's ratio
        float videoRatio =
            (float)activeCameraTexture.width / (float)activeCameraTexture.height;
        imageFitter.aspectRatio = videoRatio;

        // Unflip if vertically flipped
        image.uvRect =
            activeCameraTexture.videoVerticallyMirrored ? fixedRect : defaultRect;

        // Mirror front-facing camera's image horizontally to look more natural
        imageParent.localScale =
            activeCameraDevice.isFrontFacing ? fixedScale : defaultScale;

        evaluate();
    }

    public void Dispose()
    {
        Stop();
        model.CleanUp();
        Texture2D.Destroy(activeCameraTexture);
        Destroy(this);
    }
}