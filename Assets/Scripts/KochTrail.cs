﻿using AudioVisualization;
using System.Collections.Generic;
using UnityEngine;

namespace KochFractals
{
    public class KochTrail : KochGenerator
    {
        public class Trail
        {
            public GameObject gameObject;
            public TrailRenderer renderer;
            public int targetIndex;
            public Vector3 targetPosition;
            public Color emission;
        }

        [Header("Trail")]

        [SerializeField]
        private GameObject _trailPrefab = null;

        [SerializeField]
        private AnimationCurve _trailWidthCurve = null;

        [Range(0, 8)]
        [SerializeField]
        private int _trailEndCapVertices = 0;

        [SerializeField]
        private Material _trailMaterial = null;

        [SerializeField]
        private Gradient _trailColor = null;

        [Header("Audio")]

        [SerializeField]
        private AudioPeer _audioPeer = null;

        [SerializeField]
        private int[] _audioBands = null;

        [SerializeField]
        private Vector2 _speedMinMax = default;

        [SerializeField]
        private Vector2 _widthMinMax = default;

        [SerializeField]
        private Vector2 _trailTimeMinMax = default;

        [SerializeField]
        private float _colorMultiplier = 1f;

        private float _lerpSpeed = 0;
        private float _distanceSnap = 0;

        private Color _startColor = default;
        private Color _endColor = default;

        public List<Trail> Trails { get; private set; } = null;

        private void Start()
        {
            _startColor = new Color(0, 0, 0, 0);
            _endColor = new Color(0, 0, 0, 1);

            Trails = new List<Trail>();

            for (int i = 0, length = Initiator.edgeCount; i < length; i++)
            {
                GameObject instance = Instantiate(_trailPrefab, transform);

                Trail trail = new Trail
                {
                    gameObject = instance,
                    renderer = instance.GetComponent<TrailRenderer>(),
                    emission = _trailColor.Evaluate(i * (1.0f / length))
                };

                trail.renderer.material = new Material(_trailMaterial);
                trail.renderer.material.SetColor("_EmissionColor", trail.emission);
                trail.renderer.numCapVertices = _trailEndCapVertices;
                trail.renderer.widthCurve = _trailWidthCurve;

                Vector3 position;
                if (_generationSteps > 0)
                {
                    int step;
                    if (_useBezierCurves)
                    {
                        step = _bezierPositions.Length / Initiator.edgeCount;
                        position = _bezierPositions[i * step];
                        trail.targetIndex = (i * step) + 1;
                        trail.targetPosition = _bezierPositions[trail.targetIndex];
                    }
                    else
                    {
                        step = _currentPositions.Length / Initiator.edgeCount;
                        position = _currentPositions[i * step];
                        trail.targetIndex = (i * step) + 1;
                        trail.targetPosition = _currentPositions[trail.targetIndex];
                    }
                }
                else
                {
                    position = _currentPositions[i];
                    trail.targetIndex = i + 1;
                    trail.targetPosition = _currentPositions[trail.targetIndex];
                }

                trail.gameObject.transform.localPosition = position;
                Trails.Add(trail);
            }
        }

        private void Update()
        {
            ProcessMovement();
            AudioBehaviour();
        }

        private void AudioBehaviour()
        {
            for (int i = 0, length = Initiator.edgeCount; i < length; i++)
            {
                /// Lerp trail color and emission
                float sample = _audioPeer.AudioBandBuffers[_audioBands[i]];
                Color colorLerp = Color.Lerp(_startColor, Trails[i].emission * _colorMultiplier, sample);
                Trails[i].renderer.material.SetColor("_EmissionColor", colorLerp);
                colorLerp = Color.Lerp(_startColor, _endColor, sample);
                Trails[i].renderer.material.SetColor("_Color", colorLerp);

                /// Lerp trail width multiplier
                float widthLerp = Mathf.Lerp(_widthMinMax.x, _widthMinMax.y, sample);
                Trails[i].renderer.widthMultiplier = widthLerp;

                /// Lerp trail time
                float timeLerp = Mathf.Lerp(_trailTimeMinMax.x, _trailTimeMinMax.y, sample);
                Trails[i].renderer.time = timeLerp;
            }
        }

        private void ProcessMovement()
        {
            _lerpSpeed = Mathf.Lerp(_speedMinMax.x, _speedMinMax.y, _audioPeer.Amplitude);

            for (int i = 0; i < Trails.Count; i++)
            {
                Trail trail = Trails[i];

                _distanceSnap = Vector3.Distance(trail.gameObject.transform.localPosition, trail.targetPosition);

                if (_distanceSnap < 0.05f)
                {
                    trail.gameObject.transform.localPosition = trail.targetPosition;

                    if (_useBezierCurves && _generationSteps > 0)
                    {
                        if (trail.targetIndex < _bezierPositions.Length - 1)
                        {
                            trail.targetIndex += 1;
                        }
                        else
                        {
                            trail.targetIndex = 0;
                        }

                        trail.targetPosition = _bezierPositions[trail.targetIndex];
                    }
                    else
                    {
                        if (trail.targetIndex < _currentPositions.Length - 1)
                        {
                            trail.targetIndex += 1;
                        }
                        else
                        {
                            trail.targetIndex = 0;
                        }

                        trail.targetPosition = _targetPositions[trail.targetIndex];
                    }
                }

                trail.gameObject.transform.localPosition = Vector3.MoveTowards(
                    trail.gameObject.transform.position, trail.targetPosition, Time.deltaTime * _lerpSpeed);
            }
        }
    }
}
