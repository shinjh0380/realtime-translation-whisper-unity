using System;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using dev.SJH.Utill;

public class STTManager : MonoBehaviour
{
    public MicrophoneRecord microphoneRecord;
    public GPTManager gptManager;

    public bool streamSegments = true;
    
    [Header("UI")]
    public Button button;
    public Text buttonText;

    private string _buffer;

    private string _recordedFilePath = "final_audio.mp3"; // 녹음 완료 파일 경로
    private string _chunkedFilePath = "realtime_audio.mp3"; // 실시간 처리 파일 경로

    private void Start()
    {
        microphoneRecord = FindObjectOfType<MicrophoneRecord>();
        gptManager = FindObjectOfType<GPTManager>();
        
        microphoneRecord.OnChunkExportMp3 += OnExportMP3;
        microphoneRecord.OnChunkReady += OnChunkReady;
        microphoneRecord.OnRecordStop += OnRecordStop;

        button.onClick.AddListener(OnButtonPressed);

        // 기존 파일 제거 (테스트 환경)
        if (File.Exists(_recordedFilePath)) File.Delete(_recordedFilePath);
        if (File.Exists(_chunkedFilePath)) File.Delete(_chunkedFilePath);
    }

    private void OnButtonPressed()
    {
        if (Microphone.devices.Length == 0)
        {
            print("마이크 장치가 없습니다.");
            return;
        }
        
        if (!microphoneRecord.IsRecording)
        {
            microphoneRecord.StartRecord();
        }
        else
            microphoneRecord.StopRecord();
        
        buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
    }

    // 실시간 MP3 변환
    private void OnChunkReady(AudioChunk chunk)
    {
        try
        {
            Debug.Log($"실시간 청크 생성: {chunk}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"실시간 청크 WAV 저장 중 오류 발생: {ex.Message}");
        }
    }

    // 녹음 완료 후 MP3 변환
    private void OnExportMP3(AudioChunk recordedAudio)
    {
        _buffer = "";

        try
        {
            string filename = "final_recording.wav"; // 녹음 종료 WAV 파일 이름
            SavWav.SaveFromAudioChunk(filename, recordedAudio);

            Debug.Log($"녹음 종료 WAV 저장 완료: {filename}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"녹음 종료 WAV 저장 중 오류 발생: {ex.Message}");
        }

        // 이후 GPTManager 처리
        // gptManager.OnSubmitPrompt(res.Result);
    }
    
    private void OnRecordStop(AudioChunk recordedAudio)
    {
        buttonText.text = "Record";
        _buffer = "";
    }
}
