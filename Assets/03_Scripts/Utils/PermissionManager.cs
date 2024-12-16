using UnityEngine;
using UnityEngine.Android;

public class PermissionManager : MonoBehaviour
{
    private void Start()
    {
        RequestPermissions();
    }

    private void RequestPermissions()
    {
        StartCoroutine(RequestPermissionsCoroutine());
    }

    private System.Collections.IEnumerator RequestPermissionsCoroutine()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);

            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                yield return null;
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);

            while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                yield return null;
            }
        }
    }
}