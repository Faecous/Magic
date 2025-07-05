using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Magic.Gestures
{
    /// <summary>
    /// Records the player's cursor movement in 3D space on a plane relative to the camera.
    /// It is controlled by an external class (like SpellCaster) to start and stop recording.
    /// </summary>
    public class GestureRecorder : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The distance from the camera to place the invisible drawing plane.")]
        [SerializeField] private float planeDistance = 1.0f;
        
        [Tooltip("The minimum distance the cursor must travel to record a new point.")]
        [SerializeField] private float minPointDistance = 0.01f;

        // Public property to access the recorded path
        public List<Vector3> RecordedPath { get; private set; } = new List<Vector3>();
        public bool IsRecording { get; private set; }

        private Camera mainCamera;
        private Plane castingPlane;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Begins the gesture recording process.
        /// </summary>
        public void StartRecording()
        {
            if (IsRecording) return;
            IsRecording = true;

            // Clear any previous path
            RecordedPath.Clear();

            // Create a plane in front of the camera to draw on
            castingPlane = new Plane(mainCamera.transform.forward, mainCamera.transform.position + mainCamera.transform.forward * planeDistance);

            // TODO: Unlock and show the cursor
        }

        /// <summary>
        /// Stops the gesture recording process.
        /// </summary>
        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;

            // TODO: Lock and hide the cursor
        }

        private void Update()
        {
            if (!IsRecording) return;

            // Create a ray from the camera through the current mouse position using the new Input System
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            // Check where the ray intersects the casting plane
            if (castingPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                
                // Add the point to the path if it's far enough from the last one
                if (RecordedPath.Count == 0 || Vector3.Distance(RecordedPath[RecordedPath.Count - 1], hitPoint) > minPointDistance)
                {
                    RecordedPath.Add(hitPoint);
                    // Optional: Debug draw the path
                    // if (RecordedPath.Count > 1)
                    // {
                    //    Debug.DrawLine(RecordedPath[RecordedPath.Count - 2], RecordedPath[RecordedPath.Count - 1], Color.red, 5f);
                    // }
                }
            }
        }
    }
} 