using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public struct ResponseMessage
{
    public string role;
    public string content;
}
[System.Serializable]
public struct ResponseChoice
{
    public int index;
    public ResponseMessage message;
}
[System.Serializable]
public struct Response
{
    public string id;
    public ResponseChoice[] choices;
}
[System.Serializable]
public struct RequestMessage
{
    public string role;
    public string content;
}
[System.Serializable]
public struct Request
{
    public string model;
    public int max_tokens;
    public float temperature;
    public RequestMessage[] messages;
}

public class GPTManager : MonoBehaviour
{
    const string API_KEY = "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";

    [SerializeField] private Text promptText;
    
    private readonly List<RequestMessage> messageHistory = new();

    private void Start()
    {
        promptText.text = string.Empty;
        // btn.onClick.AddListener(() => OnSubmitPrompt(promptText.text));

        string projectDescription = @"You are an AI that helps translate. When English comes in, you need to translate it into Korean, and vice versa, when Korean comes in, you need to translate it into English. You don't need to say anything else, just translate English sentences into Korean and answer them, and Korean sentences into English and answer them.";
        messageHistory.Add(new RequestMessage { role = "system", content = projectDescription });
    }
    public void OnSubmitPrompt(string value)
    {
        if (!string.IsNullOrEmpty(value))
        { 
            StartCoroutine(OnSubmitPromptCoroutine(value));
        }
        else
        {
            promptText.text = "질문을 입력해주세요.";
        }
    }

    private IEnumerator OnSubmitPromptCoroutine(string prompt)
    {
        promptText.text = "...";

        messageHistory.Add(new RequestMessage { role = "user", content ="You are an AI that helps translate. When English comes in, you need to translate it into Korean, and vice versa, when Korean comes in, you need to translate it into English. You don't need to say anything else, just translate English sentences into Korean and answer them, and Korean sentences into English and answer them. Translate: " + prompt});

        var request = new Request
        {
            model = "gpt-3.5-turbo-1106",
            max_tokens = 140,
            temperature = 0.3f,
            messages = messageHistory.ToArray()
        };

        string jsonData = JsonUtility.ToJson(request);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        var webRequest = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST")
        { 
            uploadHandler = new UploadHandlerRaw(jsonToSend),
            downloadHandler = new DownloadHandlerBuffer()
        };
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("Authorization", "Bearer " + API_KEY);
        webRequest.timeout = 10;

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            HandleResponse(webRequest.downloadHandler.text);
        }
        else
        {
            Debug.LogError(webRequest.error);
            promptText.text = $"<color=red>{webRequest.error}</color>";
        }
    }

    private void HandleResponse(string responseText)
    {
        var response = JsonUtility.FromJson<Response>(responseText);
        if (response.choices is { Length: > 0 })
        {
            string assistantResponse = response.choices[0].message.content;
            this.promptText.text = assistantResponse;
            messageHistory.Add(new RequestMessage { role = "assistant", content = assistantResponse });
            
            Debug.Log(assistantResponse);
        }
        else
        {
            this.promptText.text = "유효한 응답을 받지 못했습니다.";
            Debug.LogWarning("유효한 응답을 받지 못했습니다.");
        }
    }
}
