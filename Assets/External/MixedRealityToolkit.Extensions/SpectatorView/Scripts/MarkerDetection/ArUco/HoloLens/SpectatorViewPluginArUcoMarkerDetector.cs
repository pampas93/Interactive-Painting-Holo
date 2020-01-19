﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.using UnityEngine;

using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Extensions.Experimental.MarkerDetection;
using Microsoft.MixedReality.Toolkit.Extensions.PhotoCapture;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.MarkerDetection
{
    /// <summary>
    /// Class implementing <see cref="Microsoft.MixedReality.Toolkit.Extensions.Experimental.MarkerDetection.IMarkerDetector"/> capable of detecting ArUco markers
    /// </summary>
    public class SpectatorViewPluginArUcoMarkerDetector : MonoBehaviour,
        IMarkerDetector
    {
#pragma warning disable 67
        /// <inheritdoc/>
        public event MarkersUpdatedHandler MarkersUpdated;
#pragma warning restore 67

        /// <summary>
        /// Physical size of markers being detected
        /// </summary>
        [Tooltip("Physical size of markers being detected")]
        [SerializeField]
        private float _markerSize = 0.03f;

        [Tooltip("Whether or not the marker is stationary or moving during detection")]
        [SerializeField]
        private MarkerPositionBehavior _markerPositionBehavior = MarkerPositionBehavior.Moving;

        private HoloLensCamera _holoLensCamera;
        private SpectatorViewPluginAPI _api;
        private bool _detecting = false;
        private Dictionary<int, List<Marker>> _markerObservations;
        private Dictionary<int, Marker> _nextMarkerUpdate;
        private MarkerDetectionCompletionStrategy _detectionCompletionStrategy;

        [HideInInspector]
        public int RequiredObservations = 5;

        [HideInInspector]
        public int RequiredInlierCount = 5;

        [HideInInspector]
        public int MaximumMarkerSampleCount = 15;

        [HideInInspector]
        public float MaximumPositionDistanceStandardDeviation = 0.01f;

        [HideInInspector]
        public float MaximumRotationAngleStandardDeviation = 0.75f;

        [HideInInspector]
        public float MarkerInlierStandardDeviationThreshold = 1.5f;

        /// <inheritdoc />
        public MarkerPositionBehavior MarkerPositionBehavior
        {
            get
            {
                return _markerPositionBehavior;
            }
            set
            {
                if (_markerPositionBehavior != value)
                {
                    _markerPositionBehavior = value;

                    UpdateDetectionCompletionStrategy();
                }
            }
        }

        private void Awake()
        {
            UpdateDetectionCompletionStrategy();
        }

        protected void Start()
        {
#if UNITY_WSA
            _api = new SpectatorViewPluginAPI();
            if (!_api.Initialize(_markerSize))
            {
                Debug.LogError("Issue loading SpectatorViewPlugin dll");
            }

            _markerObservations = new Dictionary<int, List<Marker>>();
#endif
        }

        protected void Update()
        {
            if (_detecting)
            {
                if (_holoLensCamera.State == CameraState.Ready &&
                    !_holoLensCamera.TakeSingle())
                {
                    Debug.LogError("Failed to take photo with HoloLensCamera, Camera State: " + _holoLensCamera.State.ToString());
                }
            }

            if (_nextMarkerUpdate != null)
            {
                MarkersUpdated?.Invoke(_nextMarkerUpdate);
                _nextMarkerUpdate = null;
            }
        }

        /// <inheritdoc/>
        public void StartDetecting()
        {
#if UNITY_WSA
            if (!_detecting)
            {
                _detecting = true;
                _markerObservations.Clear();
                Debug.Log("Starting ArUco marker detection");
                SetupCamera();
            }
#else
            Debug.LogError("Capturing is not supported on this platform");
#endif
        }

        /// <inheritdoc/>
        public void StopDetecting()
        {
            _detecting = false;
#if UNITY_WSA
            Debug.Log("Stopping ArUco marker detection");
            CleanUpCamera();
#else
            Debug.LogError("Capturing is not supported on this platform");
#endif
        }

        /// <inheritdoc/>
        public void SetMarkerSize(float size)
        {
            _markerSize = size;
            _api.SetMarkerSize(size);
        }

        /// <inheritdoc />
        public bool TryGetMarkerSize(int markerId, out float size)
        {
            size = 0.0f;
            return false;
        }

        private async void SetupCamera()
        {
            Debug.Log("Setting up HoloLensCamera");
            if (_holoLensCamera == null)
                _holoLensCamera = new HoloLensCamera(CaptureMode.SingleLowLatency, PixelFormat.BGRA8);

            _holoLensCamera.OnCameraInitialized += CameraInitialized;
            _holoLensCamera.OnCameraStarted += CameraStarted;
            _holoLensCamera.OnFrameCaptured += FrameCaptured;

            await _holoLensCamera.Initialize();
        }

        private void CleanUpCamera()
        {
            Debug.Log("Cleaning up HoloLensCamera");
            if (_holoLensCamera != null)
            {
                _holoLensCamera.Dispose();
                _holoLensCamera.OnCameraInitialized -= CameraInitialized;
                _holoLensCamera.OnCameraStarted -= CameraStarted;
                _holoLensCamera.OnFrameCaptured -= FrameCaptured;
                _holoLensCamera = null;
            }
        }

        private void CameraInitialized(HoloLensCamera sender, bool initializeSuccessful)
        {
            Debug.Log("HoloLensCamera initialized");
            StreamDescription streamDesc = sender.StreamSelector.Select(StreamCompare.EqualTo, 1280, 720).StreamDescriptions[0];
            sender.Start(streamDesc);
        }

        private void CameraStarted(HoloLensCamera sender, bool startSuccessful)
        {
            if (startSuccessful)
            {
                Debug.Log("HoloLensCamera successfully started");
            }
            else
            {
                Debug.LogError("Error: HoloLensCamera failed to start");
            }
        }

        private void FrameCaptured(HoloLensCamera sender, CameraFrame frame)
        {
#if UNITY_WSA
            Debug.Log("Image obtained from HoloLens");
            if (_api != null &&
                _api.IsInitialized)
            {
                var pixelFormat = frame.PixelFormat;
                var imageWidth = frame.Resolution.Width;
                var imageHeight = frame.Resolution.Height;
                var imageData = frame.PixelData;

                var intrinsics = frame.Intrinsics;
                var extrinsics = frame.Extrinsics;

                var dictionary = _api.ProcessImage(imageData, imageWidth, imageHeight, pixelFormat, intrinsics, extrinsics);
                if (_markerObservations != null)
                {
                    foreach (var markerPair in dictionary)
                    {
                        if (!_markerObservations.ContainsKey(markerPair.Key))
                        {
                            _markerObservations.Add(markerPair.Key, new List<Marker>());
                        }

                        _markerObservations[markerPair.Key].Add(markerPair.Value);
                        if (_markerObservations[markerPair.Key].Count > _detectionCompletionStrategy.MaximumMarkerSampleCount)
                        {
                            _markerObservations[markerPair.Key].RemoveAt(0);
                        }
                    }

                    var validMarkers = new Dictionary<int, Marker>();
                    foreach (var observationPair in _markerObservations)
                    {
                        Marker completedMarker;
                        if (_detectionCompletionStrategy.TryCompleteDetection(observationPair.Value, out completedMarker))
                        {
                            validMarkers[completedMarker.Id] = completedMarker;
                            observationPair.Value.Clear();
                        }
                    }

                    _nextMarkerUpdate = validMarkers;
                }
            }
#endif
        }

        private void UpdateDetectionCompletionStrategy()
        {
            if (MarkerPositionBehavior == MarkerPositionBehavior.Moving)
            {
                _detectionCompletionStrategy = new MovingMarkerDetectionCompletionStrategy(this);
            }
            else
            {
                _detectionCompletionStrategy = new StationaryMarkerDetectionCompletionStrategy(this);
            }
        }

        private static void LogMessagesAboutMarker(string markerState, IReadOnlyList<Marker> allMarkers, IReadOnlyList<Marker> inlierMarkers, Marker averageMarker, Marker averageInlierMarker)
        {
            double positionStandardDeviation = StandardDeviation(allMarkers, averageMarker, marker => (marker.Position - averageMarker.Position).magnitude);
            double rotationStandardDeviation = StandardDeviation(allMarkers, averageMarker, marker => Quaternion.Angle(marker.Rotation, averageMarker.Rotation));

            double inlierPositionStandardDeviation = StandardDeviation(inlierMarkers, averageInlierMarker, marker => (marker.Position - averageInlierMarker.Position).magnitude);
            double inlierRotationStandardDeviation = StandardDeviation(inlierMarkers, averageInlierMarker, marker => Quaternion.Angle(marker.Rotation, averageInlierMarker.Rotation));

            Debug.Log($"Calculated {markerState} marker position with {inlierMarkers.Count} markers out of {allMarkers.Count} available. Initial position standard deviation was {positionStandardDeviation} and rotation was {rotationStandardDeviation}. After outliers, position deviation was {inlierPositionStandardDeviation} and rotation was {inlierRotationStandardDeviation}. Final position was {averageInlierMarker.Position} which is {(averageInlierMarker.Position - averageMarker.Position).magnitude} away from original pose.");
        }

        private static Marker CalculateAverageMarker(IReadOnlyList<Marker> markers)
        {
            var count = (float)markers.Count;
            var averagePos = Vector3.zero;
            int id = -1;
            List<Quaternion> rotations = new List<Quaternion>();
            foreach (var marker in markers)
            {
                averagePos += marker.Position / count;
                rotations.Add(marker.Rotation);
                id = marker.Id;
            }

            var averageRot = CalculateAverageQuaternion(rotations.ToArray());
            return new Marker(id, averagePos, averageRot);
        }

        private static Quaternion CalculateAverageQuaternion(Quaternion[] quaternions)
        {
            Quaternion mean = quaternions[0];
            var text = "Quaternions: ";
            for (int i = 1; i < quaternions.Length; i++)
            {
                text += "{" + quaternions[i].x + ", " + quaternions[i].y + ", " + quaternions[i].z + ", " + quaternions[i].w + "}";
                float weight = 1.0f / (i + 1);
                mean = Quaternion.Slerp(mean, quaternions[i], weight);
            }
            Debug.Log(text);
            Debug.Log("Mean Quaternion: " + mean.x + ", " + mean.y + ", " + mean.z + ", " + mean.w);
            return mean;
        }

        private static List<Marker> CalculateInlierMarkerSet(IReadOnlyList<Marker> allMarkers, Marker averageMarker, double markerInlierStandardDeviationThreshold)
        {
            double positionStandardDeviation, rotationStandardDeviation;
            CalculateStandardDeviations(allMarkers, averageMarker, out positionStandardDeviation, out rotationStandardDeviation);

            List<Marker> inliers = new List<Marker>(allMarkers.Count);
            for (int i = 0; i < allMarkers.Count; i++)
            {
                if (IsMarkerInlier(allMarkers[i], averageMarker, positionStandardDeviation, rotationStandardDeviation, markerInlierStandardDeviationThreshold))
                {
                    inliers.Add(allMarkers[i]);
                }
                else
                {
                    Debug.Log($"Found an outlier: {allMarkers[i].Position} was {(allMarkers[i].Position - averageMarker.Position).magnitude} away and {Quaternion.Angle(allMarkers[i].Rotation, averageMarker.Rotation)} degrees from average. Standard deviation was pos:{positionStandardDeviation} and rot:{rotationStandardDeviation}");
                }
            }

            return inliers;
        }

        private static void CalculateStandardDeviations(IReadOnlyList<Marker> allMarkers, Marker averageMarker, out double positionStandardDeviation, out double rotationStandardDeviation)
        {
            positionStandardDeviation = StandardDeviation(allMarkers, averageMarker, marker => (marker.Position - averageMarker.Position).magnitude);
            rotationStandardDeviation = StandardDeviation(allMarkers, averageMarker, marker => Quaternion.Angle(marker.Rotation, averageMarker.Rotation));
        }

        private static bool IsMarkerInlier(Marker candidate, Marker averageMarker, double positionStandardDeviation, double rotationStandardDeviation, double markerInlierStandardDeviationThreshold)
        {
            return (candidate.Position - averageMarker.Position).magnitude < markerInlierStandardDeviationThreshold * positionStandardDeviation &&
                Quaternion.Angle(candidate.Rotation, averageMarker.Rotation) < markerInlierStandardDeviationThreshold * rotationStandardDeviation;
        }

        private static double StandardDeviation<T>(IReadOnlyList<T> values, T meanValue, Func<T, double> evaluator)
        {
            double sum = 0;
            double meanValueDouble = evaluator(meanValue);
            for (int i = 0; i < values.Count; i++)
            {
                double delta = evaluator(values[i]) - meanValueDouble;
                sum += (delta * delta);
            }

            return Math.Sqrt(sum / values.Count);
        }

        /// <summary>
        /// Class which contains the logic for whether or not a set of detected marker poses is a satisfactory estimate of the
        /// true position of the marker.
        /// </summary>
        private abstract class MarkerDetectionCompletionStrategy
        {
            /// <summary>
            /// Determines if the list of gathered markers is a representative sample, and if so computes the completed marker position.
            /// </summary>
            /// <param name="markers">A set of sampled positions and rotations of a physical marker.</param>
            /// <param name="completedMarker">A pose for the physical marker computed from the sampled positions.</param>
            /// <returns></returns>
            public abstract bool TryCompleteDetection(IReadOnlyList<Marker> markers, out Marker completedMarker);

            /// <summary>
            /// Gets the maximum number of marker samples that should be stored.
            /// </summary>
            public abstract int MaximumMarkerSampleCount { get; }
        }

        /// <summary>
        /// Strategy for detecting a marker that is known to be stationary. Heuristics are used to filter
        /// out noisy data to try to guarantee that the detected marker is actually aligned with the physical
        /// marker.
        /// </summary>
        private sealed class StationaryMarkerDetectionCompletionStrategy : MarkerDetectionCompletionStrategy
        {
            private readonly SpectatorViewPluginArUcoMarkerDetector detector;

            public StationaryMarkerDetectionCompletionStrategy(SpectatorViewPluginArUcoMarkerDetector detector)
            {
                this.detector = detector;
            }

            public override int MaximumMarkerSampleCount => detector.MaximumMarkerSampleCount;

            public override bool TryCompleteDetection(IReadOnlyList<Marker> markers, out Marker completedMarker)
            {
                if (markers.Count >= detector.RequiredObservations)
                {
                    var averageMarker = CalculateAverageMarker(markers);

                    // Find a set of markers that are inliers (within a threshold of the average marker).
                    // This is used to reject spurious marker detections outside the norm to prevent them from polluting
                    // the final marker result.
                    var inliers = CalculateInlierMarkerSet(markers, averageMarker, detector.MarkerInlierStandardDeviationThreshold);
                    if (inliers.Count >= detector.RequiredInlierCount)
                    {
                        // Recompute the average marker using only the set of inliers.
                        var averageInlierMarker = CalculateAverageMarker(inliers);

                        // Determine the standard deviation of the distance from the average marker and the angular
                        // delta from the average marker, and then see if that falls within the required threshold.
                        // If it does, we can stop. Otherwise, continue to gather samples until we get a set that
                        // does fall within the threshold.
                        double positionStandardDeviation, rotationStandardDeviation;
                        CalculateStandardDeviations(inliers, averageInlierMarker, out positionStandardDeviation, out rotationStandardDeviation);
                        if (positionStandardDeviation <= detector.MaximumPositionDistanceStandardDeviation && rotationStandardDeviation <= detector.MaximumRotationAngleStandardDeviation)
                        {
                            completedMarker = averageInlierMarker;
                            LogMessagesAboutMarker("final", markers, inliers, averageMarker, averageInlierMarker);
                            return true;
                        }
                        else
                        {
                            LogMessagesAboutMarker("rejected", markers, inliers, averageMarker, averageInlierMarker);
                        }
                    }
                }

                completedMarker = null;
                return false;
            }
        }

        /// <summary>
        /// Strategy for detecting a marker that is known to be non-stationary (e.g. held in someone's hand).
        /// Multiple markers are averaged but heuristics are not used to try to lock the marker to within
        /// a distance and rotation threshold.
        /// </summary>
        private sealed class MovingMarkerDetectionCompletionStrategy : MarkerDetectionCompletionStrategy
        {
            private readonly SpectatorViewPluginArUcoMarkerDetector detector;

            public MovingMarkerDetectionCompletionStrategy(SpectatorViewPluginArUcoMarkerDetector detector)
            {
                this.detector = detector;
            }

            public override int MaximumMarkerSampleCount => detector.RequiredObservations;

            public override bool TryCompleteDetection(IReadOnlyList<Marker> markers, out Marker completedMarker)
            {
                if (markers.Count >= detector.RequiredObservations)
                {
                    completedMarker = CalculateAverageMarker(markers);
                    return true;
                }
                else
                {
                    completedMarker = null;
                    return false;
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SpectatorViewPluginArUcoMarkerDetector))]
    public class SpectatorViewPluginArUcoMarkerDetectorEditor : UnityEditor.Editor
    {
        private const string requiredObservationsTooltip = "The minimum number of individual marker detections required to compute an average marker pose";
        private const string requiredInlierCountTooltip = "After outlier marker poses are removed, the required number of remaining inlier marker poses required to compute an average marker pose";
        private const string maximumMarkerSampleCountTooltip = "The maximum number of marker poses to keep in a rolling buffer when computing the average marker pose";
        private const string maximumPositionDistanceStandardDeviationTooltip = "The maximum standard deviation of distance between each detected marker and the average marker allowed to complete marker detection";
        private const string maximumRotationAngleStandardDeviationTooltip = "The maximum standard deviation of angular offset between each detected marker and the average marker allowed to complete marker detection";
        private const string markerInlierStandardDeviationThresholdTooltip = "The number of standard deviations away from the mean at which a marker pose will be rejected from the inlier marker set";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SpectatorViewPluginArUcoMarkerDetector detector = target as SpectatorViewPluginArUcoMarkerDetector;
            if (detector.MarkerPositionBehavior == MarkerPositionBehavior.Moving)
            {
                detector.RequiredObservations = EditorGUILayout.IntField(new GUIContent("Required Observations", requiredObservationsTooltip), detector.RequiredObservations);
            }
            else
            {
                detector.RequiredObservations = EditorGUILayout.IntField(new GUIContent("Required Observations", requiredObservationsTooltip), detector.RequiredObservations);
                detector.RequiredInlierCount = EditorGUILayout.IntField(new GUIContent("Required Inlier Count", requiredInlierCountTooltip), detector.RequiredInlierCount);
                detector.MaximumMarkerSampleCount = EditorGUILayout.IntField(new GUIContent("Maximum Marker Sample Count", maximumMarkerSampleCountTooltip), detector.MaximumMarkerSampleCount);
                detector.MarkerInlierStandardDeviationThreshold = EditorGUILayout.FloatField(new GUIContent("Marker Inlier Standard Deviation Threshold", markerInlierStandardDeviationThresholdTooltip), detector.MarkerInlierStandardDeviationThreshold);
                detector.MaximumPositionDistanceStandardDeviation = EditorGUILayout.FloatField(new GUIContent("Maximum Position Distance Standard Deviation", maximumPositionDistanceStandardDeviationTooltip), detector.MaximumPositionDistanceStandardDeviation);
                detector.MaximumRotationAngleStandardDeviation = EditorGUILayout.FloatField(new GUIContent("Maximum Rotation Angle Standard Deviation", maximumRotationAngleStandardDeviationTooltip), detector.MaximumRotationAngleStandardDeviation);

            }
        }
    }
#endif
}