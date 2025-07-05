using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Magic.Gestures
{
    /// <summary>
    /// A custom editor for the GesturePatternData ScriptableObject.
    /// It draws a preview of the gesture in the Inspector.
    /// </summary>
    [CustomEditor(typeof(GesturePatternData))]
    public class GesturePatternDataEditor : Editor
    {
        private const float PREVIEW_SIZE = 200;
        private const float POINT_RADIUS = 5f;

        public override void OnInspectorGUI()
        {
            // Draw the default inspector fields (the waypoints list)
            DrawDefaultInspector();

            GesturePatternData pattern = (GesturePatternData)target;

            // Add some space before our preview
            GUILayout.Space(20);
            
            // Draw a header for our preview box
            GUILayout.Label("Gesture Preview", EditorStyles.boldLabel);
            
            // Reserve a square area for the gesture preview and draw a box around it
            Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
            GUI.Box(previewRect, GUIContent.none, GUI.skin.box);
            
            if (pattern.waypoints == null || pattern.waypoints.Count < 2)
            {
                EditorGUI.LabelField(previewRect, "Not enough points to draw.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Convert normalized waypoints to GUI coordinates within our preview box
            List<Vector2> guiPoints = new List<Vector2>();
            foreach (var waypoint in pattern.waypoints)
            {
                // The waypoints are in a (-0.5, 0.5) space. We convert them to (0, 1) space.
                float x = (waypoint.x + 0.5f) * previewRect.width + previewRect.x;
                // GUI Y-coordinates are inverted, so we subtract from 1.
                float y = (1 - (waypoint.y + 0.5f)) * previewRect.height + previewRect.y;
                guiPoints.Add(new Vector2(x, y));
            }

            // Draw the lines connecting the points
            Handles.color = Color.yellow;
            for (int i = 0; i < guiPoints.Count - 1; i++)
            {
                Handles.DrawLine(guiPoints[i], guiPoints[i + 1]);
            }
            
            // Draw circles for each point
            Handles.color = Color.red;
            foreach (var point in guiPoints)
            {
                Handles.DrawSolidDisc(point, Vector3.forward, POINT_RADIUS);
            }
            
            // Ensure the GUI updates when values are changed
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
} 