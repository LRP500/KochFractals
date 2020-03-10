using UnityEngine;
using UnityEngine.Audio;

namespace AudioVisualization
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioPeer : MonoBehaviour
    {
        public static readonly int SampleCount = 512;

        public enum Channel
        {
            Stereo,
            Left,
            Right
        }

        /// <summary>
        /// Average value added to frequency highests at start
        /// to smooth frequency bands interval gap.
        /// </summary>
        [SerializeField]
        private float _audioProfile = 0f;

        [SerializeField]
        private Channel _channel = Channel.Stereo;

        [SerializeField]
        private AudioClip _audioClip = null;

        [SerializeField]
        private bool _useMicrophone = false;

        [SerializeField]
        private string _inputDevice = string.Empty;

        [SerializeField]
        public AudioMixerGroup _microphoneGroup = null;

        [SerializeField]
        public AudioMixerGroup _masterGroup = null;

        private AudioSource _audioSource = null;

        private float[] _leftSamples = new float[SampleCount];
        private float[] _rightSamples = new float[SampleCount];

        private float[] _frequencyBands = new float[8];
        private float[] _frequencyBandBuffers = new float[8];
        private float[] _bufferDecrease = new float[8];
        private float[] _frequencyBandPeaks = new float[8];

        private float[] _frequencyBands64 = new float[64];
        private float[] _frequencyBandBuffers64 = new float[64];
        private float[] _bufferDecrease64 = new float[64];
        private float[] _frequencyBandPeaks64 = new float[64];

        private float _amplitudeHighest = 0f;

        public float[] AudioBands { get; private set; } = null;
        public float[] AudioBandBuffers { get; private set; } = null;
        public float[] AudioBands64 { get; private set; } = null;
        public float[] AudioBandBuffers64 { get; private set; } = null;

        public float Amplitude { get; private set; } = 0f;
        public float AmplitudeBuffer { get; private set; } = 0f;

        private void Awake()
        {
            AudioBands = new float[8];
            AudioBandBuffers = new float[8];
            AudioBands64 = new float[64];
            AudioBandBuffers64 = new float[64];

            AudioProfile(_audioProfile);

            InitializeAudioInput();
        }

        private void Update()
        {
            GetSpectrumAudioSource();

            CreateFrequencyBands();
            FrequencyBandBuffer();
            CreateAudioBands();

            CreateFrequencyBands64();
            FrequencyBandBuffer64();
            CreateAudioBands64();

            CalculateAmplitude();
        }

        private void InitializeAudioInput()
        {
            _audioSource = GetComponent<AudioSource>();

            if (_useMicrophone)
            {
                if (Microphone.devices.Length > 0)
                {
                    _inputDevice = Microphone.devices[0].ToString();
                    _audioSource.outputAudioMixerGroup = _microphoneGroup;
                    _audioSource.clip = Microphone.Start(_inputDevice, true, 1000, AudioSettings.outputSampleRate);
                }
                else
                {
                    _useMicrophone = false;
                }
            }
            else
            {
                _audioSource.clip = _audioClip;
                _audioSource.outputAudioMixerGroup = _masterGroup;
            }

            _audioSource.Play();
        }

        private void CreateAudioBands()
        {
            for (int i = 0; i < 8; i++)
            {
                if (_frequencyBands[i] > _frequencyBandPeaks[i])
                {
                    _frequencyBandPeaks[i] = _frequencyBands[i];
                }

                AudioBands[i] = _frequencyBands[i] / _frequencyBandPeaks[i];
                AudioBandBuffers[i] = (_frequencyBandBuffers[i] / _frequencyBandPeaks[i]);
            }
        }

        private void CreateAudioBands64()
        {
            for (int i = 0; i < 64; i++)
            {
                if (_frequencyBands64[i] > _frequencyBandPeaks64[i])
                {
                    _frequencyBandPeaks64[i] = _frequencyBands64[i];
                }

                AudioBands64[i] = _frequencyBands64[i] / _frequencyBandPeaks64[i];
                AudioBandBuffers64[i] = (_frequencyBandBuffers64[i] / _frequencyBandPeaks64[i]);
            }
        }

        private void CalculateAmplitude()
        {
            float currentAmplitude = 0f;
            float currentAmplitudeBuffer = 0f;

            for (int i = 0; i < 8; i++)
            {
                currentAmplitude += AudioBands[i];
                currentAmplitudeBuffer += AudioBandBuffers[i];
            }

            _amplitudeHighest = Mathf.Max(_amplitudeHighest, currentAmplitude);
            Amplitude = currentAmplitude / _amplitudeHighest;
            AmplitudeBuffer = currentAmplitudeBuffer / _amplitudeHighest;
        }

        /// <summary>
        /// Smooth frequency bands interval gap on first few seconds
        /// of the simulation by initializing their highests to an average value.
        /// </summary>
        /// <param name="value"></param>
        private void AudioProfile(float value)
        {
            for (int i = 0; i < 8; i++)
            {
                _frequencyBandPeaks[i] = value;
            }
        }

        private void FrequencyBandBuffer()
        {
            for (int i = 0; i < 8; i++)
            {
                if (_frequencyBands[i] > _frequencyBandBuffers[i])
                {
                    _frequencyBandBuffers[i] = _frequencyBands[i];
                    _bufferDecrease[i] = 0.005f;
                }

                if (_frequencyBands[i] < _frequencyBandBuffers[i])
                {
                    _frequencyBandBuffers[i] -= _bufferDecrease[i];
                    _bufferDecrease[i] *= 1.2f;
                }
            }
        }

        private void FrequencyBandBuffer64()
        {
            for (int i = 0; i < 64; i++)
            {
                if (_frequencyBands64[i] > _frequencyBandBuffers64[i])
                {
                    _frequencyBandBuffers64[i] = _frequencyBands64[i];
                    _bufferDecrease64[i] = 0.005f;
                }

                if (_frequencyBands64[i] < _frequencyBandBuffers64[i])
                {
                    _frequencyBandBuffers64[i] -= _bufferDecrease64[i];
                    _bufferDecrease64[i] *= 1.2f;
                }
            }
        }

        private void GetSpectrumAudioSource()
        {
            _audioSource.GetSpectrumData(_leftSamples, 0, FFTWindow.Blackman);
            _audioSource.GetSpectrumData(_rightSamples, 1, FFTWindow.Blackman);
        }

        private void CreateFrequencyBands()
        {
            /// 22050 / 512 = 43Hz per sample
            /// 20-60Hz
            /// 60-250Hz
            /// 250-500Hz
            /// 500-2000Hz
            /// 2000-4000Hz
            /// 4000-6000Hz
            /// 6000-20000Hz
            /// 0 - 2 samples = 86Hz
            /// 1 - 4 samples = 172Hz (87-258Hz)
            /// 2 - 8 samples = 344Hz (259-602Hz)
            /// 3 - 16 samples = 688Hz (603-1290Hz)
            /// 4 - 32 samples = 1376Hz (1291-2666Hz)
            /// 5 - 64 samples = 2752Hz (2667-5418Hz)
            /// 6 - 128 samples = 5504Hz (5419-10922Hz)
            /// 7 - 256 samples = 11008Hz (10923-21930Hz)
            /// Total = 510Hz

            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                int sampleCount = (int)Mathf.Pow(2, i) * 2;
                float average = 0;

                if (i == 7 /*FrequencyBandCount - 1*/)
                {
                    //int samples = (int)Mathf.Pow(2, FrequencyBandCount) * (22050 / SampleCount);
                    //sampleCount += SampleCount - samples;
                    //Debug.Log($"{SampleCount} - {samples} = {sampleCount}");

                    sampleCount += 2;
                }

                for (int j = 0; j < sampleCount; j++)
                {
                    /// Stereo
                    if (_channel == Channel.Stereo)
                    {
                        average += (_leftSamples[count] + _rightSamples[count]) * (count + 1);
                    }
                    /// Left 
                    else if (_channel == Channel.Left)
                    {
                        average += _leftSamples[count] * (count + 1);
                    }
                    /// Right
                    else if (_channel == Channel.Right)
                    {
                        average += _rightSamples[count] * (count + 1);
                    }

                    count++;
                }

                average /= count;
                _frequencyBands[i] = average * 10;
            }
        }

        private void CreateFrequencyBands64()
        {
            /// 0-15 = 1 sample =       16
            /// 16-31 = 2 samples =     32
            /// 32-39 = 4 samples =     32
            /// 40-47 = 6 samples =     48
            /// 48-55 = 16 samples =    128
            /// 56-63 - 32 samples =    256 +
            ///                         ---                
            ///                         512

            int count = 0;
            int sampleCount = 1;
            int power = 0;

            for (int i = 0; i < 64; i++)
            {
                float average = 0;

                if (i == 16 || i == 32 || i == 40 || i == 48 || i == 56)
                {
                    power++;
                    sampleCount = (int)Mathf.Pow(2, power);

                    if (power == 3)
                    {
                        sampleCount -= 2;
                    }
                }

                for (int j = 0; j < sampleCount; j++)
                {
                    /// Stereo
                    if (_channel == Channel.Stereo)
                    {
                        average += (_leftSamples[count] + _rightSamples[count]) * (count + 1);
                    }
                    /// Left 
                    else if (_channel == Channel.Left)
                    {
                        average += _leftSamples[count] * (count + 1);
                    }
                    /// Right
                    else if (_channel == Channel.Right)
                    {
                        average += _rightSamples[count] * (count + 1);
                    }

                    count++;
                }

                average /= count;
                _frequencyBands64[i] = average * 80;
            }
        }
    }
}
