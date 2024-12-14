using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using dev.SJH.Utill;

public class STTManager : MonoBehaviour
{
    public MicrophoneRecord microphoneRecord;
    public WhisperManager whisperManager;

    public bool streamSegments = true;
    
    [Header("UI")]
    public Button button;
    public Text buttonText;

    private string _buffer;

    private void Awake()
    {
        microphoneRecord = FindObjectOfType<MicrophoneRecord>();
        whisperManager = FindObjectOfType<WhisperManager>();
        button.onClick.AddListener(OnButtonPressed);
    }

    private void OnEnable()
    {
        microphoneRecord.OnChunkExportMp3 += OnExportMP3;
        microphoneRecord.OnChunkReady += OnChunkReady;
        microphoneRecord.OnRecordStop += OnRecordStop;
    }

    private void OnDisable()
    {
        microphoneRecord.OnChunkExportMp3 -= OnExportMP3;
        microphoneRecord.OnChunkReady -= OnChunkReady;
        microphoneRecord.OnRecordStop -= OnRecordStop;
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
    
    private void OnExportMP3(AudioChunk recordedAudio)
    {
        _buffer = "";

        try
        {
            string filename = "final_recording"; // 녹음 종료 WAV 파일 이름
            whisperManager.SendAudioToWhisper(SavWav.SaveFromAudioChunk(filename, recordedAudio));
            Debug.Log($"녹음 종료 WAV 저장 완료: {filename}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"녹음 종료 WAV 저장 중 오류 발생: {ex.Message}");
        }
    }
    
    private void OnRecordStop(AudioChunk recordedAudio)
    {
        buttonText.text = "Record";
        _buffer = "";
    }
    
    
    
}
