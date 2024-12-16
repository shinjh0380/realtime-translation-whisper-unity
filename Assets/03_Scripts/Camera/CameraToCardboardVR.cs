using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class CameraToCardboardVR : MonoBehaviour
{
    public RawImage eyeImage;
    private WebCamTexture _webcamTexture;

    private void Update()
    {
        if (_webcamTexture == null && Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            _webcamTexture = new WebCamTexture();

            _webcamTexture.Play();
            if (eyeImage != null)
                eyeImage.texture = _webcamTexture;
        }
    }

    private void OnDestroy()
    {
        if (_webcamTexture != null)
            _webcamTexture.Stop();
    }
}
