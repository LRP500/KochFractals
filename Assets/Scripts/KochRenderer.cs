using UnityEngine;

namespace KochFractals
{
    [RequireComponent(typeof(LineRenderer))]
    public class KochRenderer : KochGenerator
    {
        [Range(0, 1)]
        [SerializeField]
        private float _lerpAmount = 0f;

        [SerializeField]
        private float _generationSizeMultiplier = 1f;

        [Space]
        [SerializeField]
        private KeyCode _generateInwards = KeyCode.I;

        [SerializeField]
        private KeyCode _generateOutwards = KeyCode.O;

        private LineRenderer _lineRenderer = null;

        private Vector3[] _lerpedPositions = null;

        private void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.loop = true;
            _lineRenderer.enabled = true;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.positionCount = _currentPositions.Length;
            _lineRenderer.SetPositions(_currentPositions);
            _lerpedPositions = new Vector3[_currentPositions.Length];
        }

        private void Update()
        {
            /// Lerp current positions towards target positions.
            if (_generationSteps != 0)
            {
                for (int i = 0, length = _currentPositions.Length; i < length; i++)
                {
                    _lerpedPositions[i] = Vector3.Lerp(_currentPositions[i], _targetPositions[i], _lerpAmount);
                }

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
