using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace dev.SJH.Utill
{
 /// <summary>
    /// 녹음된 오디오 클립의 일부를 나타냅니다.
    /// </summary>
    public struct AudioChunk
    {
        public float[] Data;
        public int Frequency;
        public int Channels;
        public float Length;
        public bool IsVoiceDetected;
    }
    
    public delegate void OnVadChangedDelegate(bool isSpeechDetected);
    public delegate void OnChunkReadyDelegate(AudioChunk chunk);
    public delegate void OnChunkExportMp3Delegate(AudioChunk chunk);
    public delegate void OnRecordStopDelegate(AudioChunk recordedAudio);
    
    /// <summary>
    /// 마이크 입력 설정 및 녹음을 제어합니다.
    /// </summary>
    public partial class MicrophoneRecord : MonoBehaviour
    {
        [Tooltip("마이크로 녹음된 오디오의 최대 길이(초 단위)")]
        public int maxLengthSec = 60;
        [Tooltip("최대 길이에 도달한 후에도 녹음을 계속할지 여부")]
        public bool loop;
        [Tooltip("마이크 샘플링 속도")]
        public int frequency = 16000;
        [Tooltip("오디오 청크의 길이(초 단위). 스트리밍에 유용")]
        public float chunksLengthSec = 0.5f;
        [Tooltip("녹음 완료 후 마이크가 에코를 재생해야 하는지 여부")]
        public bool echo = true;
        
        [Header("음성 활동 감지(VAD)")]
        [Tooltip("마이크가 오디오 입력에 음성이 있는지 확인해야 하는지 여부")]
        public bool useVad = true;
        [Tooltip("VAD가 현재 오디오 청크에 음성이 있는지 확인하는 빈도")]
        public float vadUpdateRateSec = 0.1f;
        [Tooltip("VAD가 청크에 음성이 있는지 확인할 때 사용하는 녹음 오디오 시간(초 단위)")]
        public float vadContextSec = 30f;
        [Tooltip("VAD가 음성을 감지하려고 시도하는 시간 창(초 단위)")]
        public float vadLastSec = 1.25f;
        [Tooltip("VAD 에너지 활성화 임계값")]
        public float vadThd = 1.0f;
        [Tooltip("VAD 필터 주파수 임계값")]
        public float vadFreqThd = 100.0f;
        [Tooltip("음성 감지 시 색상이 변경되는 선택적 인디케이터")]
        [CanBeNull] public Image vadIndicatorImage;
        
        [Header("VAD 종료")]
        [Tooltip("음성이 감지되지 않을 때 마이크 녹음을 중지할지 여부")]
        public bool vadStop;
        [Tooltip("VAD를 사용할 때 침묵이 감지된 마지막 오디오를 제거할지 여부")]
        public bool dropVadPart = true;
        [Tooltip("침묵 후 몇 초가 지나야 마이크 녹음이 중지되는지 설정")]
        public float vadStopTime = 3f;

        [Header("VAD 일정 시간 종료 감지 시 MP3 EXPORT")]
        public bool exportMp3VadStop;
        public bool exportMp3DropVadPart = true;
        public float exportMp3VadStopTime = 3f;
        
        [Header("마이크 선택(옵션)")] 
        [Tooltip("사용 가능한 모든 마이크 입력을 포함한 선택적 UI 드롭다운")]
        [CanBeNull] public Dropdown microphoneDropdown;
        [Tooltip("드롭다운에서 기본 마이크 입력의 라벨")]
        public string microphoneDefaultLabel = "Default microphone";

        /// <summary>
        /// VAD 상태가 변경될 때 발생합니다.
        /// </summary>
        public event OnVadChangedDelegate OnVadChanged;
        /// <summary>
        /// 마이크에서 새로운 오디오 청크가 준비될 때 발생합니다.
        /// </summary>
        public event OnChunkReadyDelegate OnChunkReady;
        public event OnChunkExportMp3Delegate OnChunkExportMp3;
        /// <summary>
        /// 마이크 녹음이 중지될 때 발생합니다.
        /// <see cref="maxLengthSec"/> 또는 그 이하의 녹음된 오디오를 반환합니다.
        /// </summary>
        public event OnRecordStopDelegate OnRecordStop;
        
        private int _lastVadPos;
        private AudioClip _clip;
        private float _length;
        private int _lastChunkPos;
        private int _chunksLength;
        private float? _vadStopBegin;
        private int _lastMicPos;
        private bool _madeLoopLap;
        private readonly List<float> _exportBuffer = new List<float>();

        private string _selectedMicDevice;

        public string SelectedMicDevice
        {
            get => _selectedMicDevice;
            set
            {
                if (value != null && !AvailableMicDevices.Contains(value))
                    throw new ArgumentException("마이크 장치를 찾을 수 없습니다.");
                _selectedMicDevice = value;
            }
        }

        public int ClipSamples => _clip.samples * _clip.channels;

        public string RecordStartMicDevice { get; private set; }
        public bool IsRecording { get; private set; }
        public bool IsVoiceDetected { get; private set; }

        public IEnumerable<string> AvailableMicDevices => Microphone.devices;

        private void Awake()
        {
            if(microphoneDropdown != null)
            {
                microphoneDropdown.options = AvailableMicDevices
                    .Prepend(microphoneDefaultLabel)
                    .Select(text => new Dropdown.OptionData(text))
                    .ToList();
                microphoneDropdown.value = microphoneDropdown.options
                    .FindIndex(op => op.text == microphoneDefaultLabel);
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
        }

        private void Update()
        {
            if (!IsRecording)
                return;
            
            // 현재 마이크 위치 시간 확인
            var micPos = Microphone.GetPosition(RecordStartMicDevice);
            if (micPos < _lastMicPos)
            {
                // 마이크가 루프를 시작한 것으로 보임
                // 이를 허용하지 않는 경우 중지
                _madeLoopLap = true;
                if (!loop)
                {
                    LogUtils.Verbose($"녹음을 중지합니다. 마이크 위치가 {micPos}로 돌아갔습니다.");
                    StopRecord();
                    return;
                }
                
                // 루프가 허용되면 계속 실행
                LogUtils.Verbose("마이크가 새로운 루프를 시작했습니다. 녹음을 계속합니다.");
            }
            _lastMicPos = micPos;

            // 녹음 중 - 청크 및 VAD 업데이트
            UpdateChunks(micPos);
            UpdateVad(micPos);
        }
        
        private void UpdateChunks(int micPos)
        {
            // 청크 처리를 구독한 사람이 있는지 확인
            if (OnChunkReady == null)
                return;

            // 청크 길이가 유효한지 확인
            if (_chunksLength <= 0)
                return;
            
            // 현재 청크 길이를 가져오기
            var chunk = GetMicPosDist(_lastChunkPos, micPos);
            
            // 청크 크기가 유효한 경우 새 청크 전송
            while (chunk > _chunksLength)
            {
                var origData = new float[_chunksLength];
                _clip.GetData(origData, _lastChunkPos);

                var chunkStruct = new AudioChunk()
                {
                    Data = origData,
                    Frequency = _clip.frequency,
                    Channels = _clip.channels,
                    Length = chunksLengthSec,
                    IsVoiceDetected = IsVoiceDetected
                };
                OnChunkReady(chunkStruct);
                if (chunkStruct.IsVoiceDetected)
                {
                    _exportBuffer.AddRange(chunkStruct.Data);
                }

                _lastChunkPos = (_lastChunkPos + _chunksLength) % ClipSamples;
                chunk = GetMicPosDist(_lastChunkPos, micPos);
            }
        }
        
        private void UpdateVad(int micPos)
        {
            if (!useVad)
                return;
            
            // 현재 녹음된 클립 길이 가져오기
            var samplesCount = GetMicBufferLength(micPos);
            if (samplesCount <= 0)
                return;

            // 업데이트 시간 확인
            var vadUpdateRateSamples = vadUpdateRateSec * _clip.frequency;
            var dt = GetMicPosDist(_lastVadPos, micPos);
            if (dt < vadUpdateRateSamples)
                return;
            _lastVadPos = samplesCount;
            
            // 음성 감지를 위한 샘플 가져오기
            var data = GetMicBufferLast(micPos, vadContextSec);
            var vad = AudioUtils.SimpleVad(data, _clip.frequency, vadLastSec, vadThd, vadFreqThd);

            // VAD 상태가 변경되면 이벤트 발생
            if (vad != IsVoiceDetected)
            {
                _vadStopBegin = !vad ? Time.realtimeSinceStartup : (float?) null;
                IsVoiceDetected = vad;
                OnVadChanged?.Invoke(vad);   
            }
            
            // VAD 인디케이터 업데이트
            if (vadIndicatorImage)
            {
                var color = vad ? Color.green : Color.red;
                vadIndicatorImage.color = color;
                Debug.Log(vad);
            }

            UpdateVadStop();
            UpdateExportMP3VadStop();
        }
        
        private void UpdateVadStop()
        {
            if (!vadStop || _vadStopBegin == null)
                return;

            var passedTime = Time.realtimeSinceStartup - _vadStopBegin;
            if (passedTime > vadStopTime)
            {
                var dropTime = dropVadPart ? vadStopTime : 0f;
                StopRecord(dropTime);
            }
        }
        
        private void UpdateExportMP3VadStop()
        {
            if (!exportMp3VadStop || _vadStopBegin == null)
                return;

            var passedTime = Time.realtimeSinceStartup - _vadStopBegin;
            if (passedTime > exportMp3VadStopTime)
            {
                var dropTime = exportMp3DropVadPart ? exportMp3VadStopTime : 0f;
                ExportMp3();
            }
        }

        private void ExportMp3()
        {
            if (!IsRecording)
                return;
            
            var finalAudio = new AudioChunk()
            {
                Data = _exportBuffer.ToArray(),
                Channels = _clip.channels,
                Frequency = _clip.frequency,
                IsVoiceDetected = IsVoiceDetected,
                Length = (float) _exportBuffer.ToArray().Length / (_clip.frequency * _clip.channels)
            };
            
            _vadStopBegin = null;
            
            ClearMicBuffer();
            
            LogUtils.Verbose($"Export MP3. 최종 오디오 길이: " + $"{finalAudio.Length}초 ({finalAudio.Data.Length} 샘플)");

            OnChunkExportMp3?.Invoke(finalAudio);
        }

        private void OnMicrophoneChanged(int ind)
        {
            if (microphoneDropdown == null) return;
            var opt = microphoneDropdown.options[ind];
            SelectedMicDevice = opt.text == microphoneDefaultLabel ? null : opt.text;
        }

        public void StartRecord()
        {
            if (IsRecording)
                return;
            
            RecordStartMicDevice = SelectedMicDevice;
            _clip = Microphone.Start(RecordStartMicDevice, loop, maxLengthSec, frequency);
            IsRecording = true;

            _lastMicPos = 0;
            _madeLoopLap = false;
            _lastChunkPos = 0;
            _lastVadPos = 0;
            _vadStopBegin = null;
            _chunksLength = (int) (_clip.frequency * _clip.channels * chunksLengthSec);
        }

        public void StopRecord(float dropTimeSec = 0f)
        {
            if (!IsRecording)
                return;
            
            var data = GetMicBuffer(dropTimeSec);
            var finalAudio = new AudioChunk()
            {
                Data = data,
                Channels = _clip.channels,
                Frequency = _clip.frequency,
                IsVoiceDetected = IsVoiceDetected,
                Length = (float) data.Length / (_clip.frequency * _clip.channels)
            };
            
            Microphone.End(RecordStartMicDevice);
            IsRecording = false;
            Destroy(_clip);
            LogUtils.Verbose($"마이크 녹음이 중지되었습니다. 최종 오디오 길이: " + $"{finalAudio.Length}초 ({finalAudio.Data.Length} 샘플)");

            if (IsVoiceDetected)
            {
                IsVoiceDetected = false;
                OnVadChanged?.Invoke(false);   
            }
            
            if (echo)
            {
                var echoClip = AudioClip.Create("echo", data.Length,
                    _clip.channels, _clip.frequency, false);
                echoClip.SetData(data, 0);
                PlayAudioAndDestroy.Play(echoClip, Vector3.zero);
            }

            OnRecordStop?.Invoke(finalAudio);
        }
    }

    public partial class MicrophoneRecord
    {
        private float[] GetMicBuffer(float dropTimeSec = 0f)
        {
            var micPos = Microphone.GetPosition(RecordStartMicDevice);
            var len = GetMicBufferLength(micPos);
            if (len == 0) return Array.Empty<float>();
            
            var dropTimeSamples = (int) (_clip.frequency * dropTimeSec);
            len = Math.Max(0, len - dropTimeSamples);
            
            var data = new float[len];
            var offset = _madeLoopLap ? micPos : 0;
            _clip.GetData(data, offset);
            
            return data;
        }
        private float[] GetMicBufferLast(int micPos, float lastSec)
        {
            var len = GetMicBufferLength(micPos);
            if (len == 0) 
                return Array.Empty<float>();
            
            var lastSamples = (int) (_clip.frequency * lastSec);
            var dataLength = Math.Min(lastSamples, len);
            var offset = micPos - dataLength;
            if (offset < 0) offset = len + offset;

            var data = new float[dataLength];
            _clip.GetData(data, offset);
            return data;
        }
        private int GetMicBufferLength(int micPos)
        {
            if (micPos == 0 && !_madeLoopLap) 
                return 0;
            
            var len = _madeLoopLap ? ClipSamples : micPos;
            return len;
        }
        private int GetMicPosDist(int prevPos, int newPos)
        {
            if (newPos >= prevPos)
                return newPos - prevPos;

            return ClipSamples - prevPos + newPos;
        }
        private void ClearMicBuffer()
        {
            if (!IsRecording)
                return;

            // // 현재 마이크 녹음을 중지합니다.
            // Microphone.End(RecordStartMicDevice);
            // LogUtils.Verbose("마이크 녹음 버퍼를 초기화하기 위해 녹음을 중지했습니다.");
            //
            // // 새로운 녹음을 시작하여 버퍼를 초기화합니다.
            // _clip = Microphone.Start(RecordStartMicDevice, loop, maxLengthSec, frequency);
            // LogUtils.Verbose("마이크 녹음을 다시 시작하여 버퍼를 초기화했습니다.");

            // 내부 추적 변수들을 초기화합니다.
            _exportBuffer.Clear();
            _lastMicPos = 0;
            _madeLoopLap = false;
            _lastChunkPos = 0;
            _lastVadPos = 0;
            _vadStopBegin = null;
        }
    }
}