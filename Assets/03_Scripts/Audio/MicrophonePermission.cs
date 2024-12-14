using UnityEngine;
using UnityEngine.Android;

public class MicrophonePermission : MonoBehaviour
{
    void Start()
    {
        // 마이크 권한 요청
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
    }
}