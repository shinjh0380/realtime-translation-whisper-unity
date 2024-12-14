using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperManager : MonoBehaviour
{
    private const string API_URL = "https://api.openai.com/v1/audio/transcriptions";
    public GPTManager gptManager;
    
    public class ResponseData
    {
        public string text;
        public string language;
    }

    private void Awake()
    {
        gptManager = FindObjectOfType<GPTManager>();
    }

    public void SendAudioToWhisper(string path)
    {
        if (File.Exists(path))
        {
            StartCoroutine(SendToWhisperAPI(path));
        }
        else
        {
            Debug.LogError("Audio file not found: " + path);
        }
    }

    private IEnumerator SendToWhisperAPI(string path)
    {
        WWWForm form = new WWWForm();
        byte[] audioData = File.ReadAllBytes(path);
        form.AddBinaryData("file", audioData, Path.GetFileName(path), "audio/wav");
        form.AddField("model", "whisper-1");
        form.AddField("task", "transcribe");
        
        UnityWebRequest www = UnityWebRequest.Post(API_URL, form);
        www.SetRequestHeader("Authorization", "Bearer " + KeyManager.OPENAI_API_KEY);
        
        yield return www.SendWebRequest();

        // 응답 처리
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
            Debug.LogError("Response: " + www.downloadHandler.text);
        }
        else
        {
            string jsonResponse = www.downloadHandler.text;
            Debug.Log("Raw JSON Response: " + jsonResponse);

            try
            {
                // JSON 응답을 객체로 변환
                ResponseData responseData = JsonConvert.DeserializeObject<ResponseData>(jsonResponse);
                if (responseData != null)
                {
                    Debug.Log("Whisper API Response Text: " + responseData.text + " Language: " + responseData.language);
                    gptManager.OnSubmitPrompt(responseData.text);
                }
                else
                {
                    Debug.LogError("Failed to parse JSON response.");
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError("JSON Parsing Error: " + ex.Message);
            }
        }
    }
}
