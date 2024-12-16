using UnityEngine;

namespace dev.SJH.Utill
{
    public static class AudioUtils
    {
        public static bool SimpleVad(float[] data, int sampleRate, float lastSec, float vadThd, float freqThd)
        {
            // https://github.com/ggerganov/whisper.cpp/blob/a792c4079ce61358134da4c9bc589c15a03b04ad/examples/common.cpp#L697
            var nSamples = data.Length;
            var nSamplesLast = (int) (sampleRate * lastSec);
            
            if (nSamplesLast >= nSamples) 
            {
                return false;
            }
            
            if (freqThd > 0.0f) 
                HighPassFilter(data, freqThd, sampleRate);
            
            var energyAll = 0.0f;
            var energyLast = 0.0f;
            
            for (var i = 0; i < nSamples; i++) 
            {
                energyAll += Mathf.Abs(data[i]);

                if (i >= nSamples - nSamplesLast) 
                    energyLast += Mathf.Abs(data[i]);
            }
            
            energyAll /= nSamples;
            energyLast /= nSamplesLast;
            
            return energyLast >  vadThd * energyAll;
        }
        
        public static void HighPassFilter(float[] data, float cutoff, int sampleRate)
        {
            // https://github.com/ggerganov/whisper.cpp/blob/a792c4079ce61358134da4c9bc589c15a03b04ad/examples/common.cpp#L684
            if (data.Length == 0)
                return;

            var rc = 1.0f / (2.0f * Mathf.PI * cutoff); 
            var dt = 1.0f / sampleRate;
            var alpha = dt / (rc + dt);
            
            var y = data[0];
            for (var i = 1; i < data.Length; i++) 
            {
                y = alpha * (y + data[i] - data[i - 1]);
                data[i] = y;
            }
        }

    }
}