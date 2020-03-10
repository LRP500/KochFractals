using AudioVisualization;
using UnityEngine;

namespace KochFractals
{
    [RequireComponent(typeof(LineRenderer))]
    public class KochLine : KochGenerator
    {
        [Header("Audio")]
        [SerializeField]
        private AudioPeer _audioPeer = null;

        [SerializeField]
        private int[] _audioBands = null;

        [SerializeField]
        private Material _material = null;

        [SerializeField]
        private Color _color = default;

        [SerializeField]
        private float _emissionMultiplier = 1f;

        [SerializeField]
        private int _audioBandMaterial = 0;

        private LineRenderer _lineRenderer = null;

        private Vector3[] _lerpedPositions = null;

        private float[] _lerpedAudio = null;

        private Material _materialInstance = null;

        private void Start()
        {
            _materialInstance = new Material(_material);

            _lerpedPositions = new Vector3[_currentPositions.Length];
            _lerpedAudio = new float[Initiator.edgeCount];

            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.loop = true;
            _lineRenderer.enabled = true;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.material = _materialInstance;
            _lineRenderer.positionCount = _currentPositions.Length;
            _lineRenderer.SetPositions(_currentPositions);
        }

        private void Update()
        {
            /// Set line renderer material color.
            Color emission = _color * _audioPeer.AudioBandBuffers[_audioBandMaterial] * _emissionMultiplier;
            _materialInstance.SetColor("_EmissionColor", emission);

            /// Lerp current positions towards target positions.
            if (_generationSteps != 0)
            {
                int count = 0;
                for (int i = 0, length = Initiator.edgeCount; i < length; i++)
                {
                    _lerpedAudio[i] = _audioPeer.AudioBandBuffers[_audioBands[i]];

                    for (int j = 0; j < (_currentPositions.Length - 1) / length; j++)
                    {
                        _lerpedPositions[count] = Vector3.Lerp(_currentPositions[count], _targetPositions[count], _lerpedAudio[i]);
                        count++;
                    }
                }

                _lerpedPositions[count] = Vector3.Lerp(_currentPositions[count], _targetPositions[count], _lerpedAudio[Initiator.edgeCount - 1]);

                /// Bezier curves
                if (_useBezierCurves)
                {
                    _bezierPositions = BezierCurve(_lerpedPositions, _bezierVertexCount);
                    _lineRenderer.positionCount = _bezierPositions.Length;
                    _lineRenderer.SetPositions(_bezierPositions);
                }
                else
                {
                    _lineRenderer.positionCount = _lerpedPositions.Length;
                    _lineRenderer.SetPositions(_lerpedPositions);
                }
            }
        }
    }
}
