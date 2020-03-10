using System.Collections.Generic;
using UnityEngine;

namespace KochFractals
{
    public class KochGenerator : MonoBehaviour
    {
        #region Data Structures

        protected enum Axis
        {
            XAxis,
            YAxis,
            ZAxis
        }

        protected enum InitiatorMode
        {
            Triangle,
            Square,
            Pentagon,
            Hexagon,
            Heptagon,
            Octagon
        }

        private struct Rotation
        {
            public Vector3 direction;
            public Vector3 axis;

            public Rotation(Vector3 direction, Vector3 axis)
            {
                this.direction = direction;
                this.axis = axis;
            }
        }

        protected struct InitiatorInfo
        {
            public int edgeCount;
            public float initialRotation;

            public InitiatorInfo(int edgeCount, float initialRotation)
            {
                this.edgeCount = edgeCount;
                this.initialRotation = initialRotation;
            }
        }

        public struct LineSegment
        {
            public Vector3 start;
            public Vector3 end;
            public Vector3 direction;
            public float length;
        }

        [System.Serializable]
        public struct StartGen
        {
            public bool outwards;
            public float scale;
        }

        private Dictionary<Axis, Rotation> _rotations = new Dictionary<Axis, Rotation>()
        {
            { Axis.XAxis, new Rotation(new Vector3(1, 0, 0), new Vector3(0, 0, 1)) },
            { Axis.YAxis, new Rotation(new Vector3(0, 1, 0), new Vector3(1, 0, 0)) },
            { Axis.ZAxis, new Rotation(new Vector3(0, 0, 1), new Vector3(0, 1, 0)) }
        };


        private static Dictionary<InitiatorMode, InitiatorInfo> _initiators = new Dictionary<InitiatorMode, InitiatorInfo>()
        {
            { InitiatorMode.Triangle, new InitiatorInfo(3, 0f) },
            { InitiatorMode.Square, new InitiatorInfo(4, 45f) },
            { InitiatorMode.Pentagon, new InitiatorInfo(5, 36f) },
            { InitiatorMode.Hexagon, new InitiatorInfo(6, 30f) },
            { InitiatorMode.Heptagon, new InitiatorInfo(7, 25.71428f) },
            { InitiatorMode.Octagon, new InitiatorInfo(8, 22.5f) }
        };

        #endregion Data Structures

        #region Serialized Fields

        [SerializeField]
        private Axis _axis = Axis.ZAxis;

        [SerializeField]
        private InitiatorMode _initiatorType = InitiatorMode.Triangle;

        [SerializeField]
        private float _initiatorSize = 1f;

        [SerializeField]
        private AnimationCurve _generator = null;

        [SerializeField]
        private StartGen[] _startGen = null;

        [Header("Bezier Curves")]
        [SerializeField]
        protected bool _useBezierCurves = false;

        [Range(4, 32)]
        [SerializeField]
        protected int _bezierVertexCount = 8;

        [Header("Utility")]
        [SerializeField]
        private float _sideLength = 0f;

        #endregion Serialized Fields

        #region Private Fields

        private InitiatorInfo _initiator = default;

        private Rotation _rotation = default;

        protected Vector3[] _currentPositions = null;
        protected Vector3[] _targetPositions = null;
        protected Vector3[] _bezierPositions = null;

        private List<LineSegment> _lineSegments = null;
        private Keyframe[] _keys = null;

        protected int _generationSteps = 0;

        #endregion Private Fields

        #region Properties

        protected InitiatorInfo Initiator => _initiators[_initiatorType];

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            InitializeGenerator();
            InitializeSegments();
            InitializePositions();
        }

        #endregion MonoBehaviour

        #region Public Methods
        #endregion Public Methods

        #region Private Methods

        private void InitializeGenerator()
        {
            _initiator = _initiators[_initiatorType];
            _rotation = _rotations[_axis];
        }

        private void InitializeSegments()
        {
            _lineSegments = new List<LineSegment>();
            _keys = _generator.keys;
        }

        private void InitializePositions()
        {
            /// Fetch initiator and axis info
            _currentPositions = new Vector3[_initiator.edgeCount + 1];
            _targetPositions = new Vector3[_initiator.edgeCount + 1];

            /// Apply initial rotation to align with axis
            _rotation.direction = Quaternion.AngleAxis(_initiator.initialRotation, _rotation.axis) * _rotation.direction;

            /// Create shape edges
            for (int i = 0; i < _initiator.edgeCount; i++)
            {
                _currentPositions[i] = _rotation.direction * _initiatorSize;
                _rotation.direction = Quaternion.AngleAxis(360f / _initiator.edgeCount, _rotation.axis) * _rotation.direction;
            }

            /// Resolve last edge
            _currentPositions[_initiator.edgeCount] = _currentPositions[0];
            _targetPositions = _currentPositions;

            /// Start generation
            foreach (StartGen gen in _startGen)
            {
                Generate(_targetPositions, gen.outwards, gen.scale);
            }
        }

        protected void Generate(Vector3[] positions, bool outwards, float sizeMultiplier)
        {
            _lineSegments.Clear();

            for (int i = 0; i < positions.Length - 1; i++)
            {
                LineSegment segment = new LineSegment
                {
                    start = positions[i],
                    end = (i == positions.Length - 1) ? positions[0] : positions[i + 1]
                };

                segment.direction = (segment.end - segment.start).normalized;
                segment.length = Vector3.Distance(segment.end, segment.start);
                _lineSegments.Add(segment);
            }

            List<Vector3> newPos = new List<Vector3>();
            List<Vector3> targetPos = new List<Vector3>();

            for (int i = 0, length = _lineSegments.Count; i < length; i++)
            {
                LineSegment currentSegment = _lineSegments[i];

                newPos.Add(currentSegment.start);
                targetPos.Add(currentSegment.start);

                /// Resolve new segment positions with animation curve.
                for (int j = 1, keyCount = _keys.Length - 1; j < keyCount; j++)
                {
                    Keyframe currentKey = _keys[j];

                    float moveAmount = currentSegment.length * currentKey.time;
                    float heightAmount = (currentSegment.length * currentKey.value) * sizeMultiplier;

                    Vector3 movePos = currentSegment.start + (currentSegment.direction * moveAmount);
                    Vector3 moveDir = Quaternion.AngleAxis(outwards ? -90 : 90, _rotation.axis) * currentSegment.direction;

                    newPos.Add(movePos);
                    targetPos.Add(movePos + (moveDir * heightAmount));
                }
            }

            /// Resolve last segment
            newPos.Add(_lineSegments[0].start);
            targetPos.Add(_lineSegments[0].start);

            /// Apply changes to edge position arrays
            _currentPositions = new Vector3[newPos.Count];
            _targetPositions = new Vector3[targetPos.Count];
            _currentPositions = newPos.ToArray();
            _targetPositions = targetPos.ToArray();
            _bezierPositions = BezierCurve(_targetPositions, _bezierVertexCount);

            /// Increment steps
            _generationSteps++;
        }

        protected Vector3[] BezierCurve(Vector3[] positions, int vertexCount)
        {
            List<Vector3> resultingPoints = new List<Vector3>();

            for (int i = 0, length = positions.Length; i < length; i += 2)
            {
                if (i + 2 <= positions.Length - 1)
                {
                    for (float ratio = 0f; ratio <= 1f; ratio += 1.0f / vertexCount)
                    {
                        Vector3 tangentLineVertex1 = Vector3.Lerp(positions[i], positions[i + 1], ratio);
                        Vector3 tangentLineVertex2 = Vector3.Lerp(positions[i + 1], positions[i + 2], ratio);
                        Vector3 bezierPoint = Vector3.Lerp(tangentLineVertex1, tangentLineVertex2, ratio);
                        resultingPoints.Add(bezierPoint);
                    }
                }
            }

            return resultingPoints.ToArray();
        }

        #endregion Private Methods

        #region Editor

        private void OnDrawGizmos()
        {
            InitiatorInfo initiator = _initiators[_initiatorType];
            Vector3[] edges = new Vector3[initiator.edgeCount];
            Rotation rotation = _rotations[_axis];

            /// Apply initial rotation to align with axis
            rotation.direction = Quaternion.AngleAxis(initiator.initialRotation, rotation.axis) * rotation.direction;

            /// Create shape edges
            for (int i = 0; i < initiator.edgeCount; i++)
            {
                edges[i] = rotation.direction * _initiatorSize;
                rotation.direction = Quaternion.AngleAxis(360f / initiator.edgeCount, rotation.axis) * rotation.direction;
            }

            /// Initialize gizmos
            Gizmos.color = Color.white;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            /// Draw shape edges
            for (int i = 0; i < initiator.edgeCount; i++)
            {
                Vector3 end = (i < initiator.edgeCount - 1) ? edges[i + 1] : edges[0];
                Gizmos.DrawLine(edges[i], end);
            }

            _sideLength = Vector3.Distance(edges[0], edges[1]) * 0.5f;
        }

        #endregion Editor
    }
}
