using UnityEngine;
using System.Collections.Generic;

namespace Magic.Gestures
{
    /// <summary>
    /// A static class that handles the logic for normalizing and comparing gesture paths.
    /// </summary>
    public static class GestureRecognizer
    {
        /// <summary>
        /// The number of points to resample a gesture to before comparison.
        /// </summary>
        private const int NUM_POINTS = 64;
        
        /// <summary>
        /// The normalized size of the gesture template.
        /// </summary>
        private const float SQUARE_SIZE = 1.0f;

        /// <summary>
        /// Compares a recorded 3D path against a 2D gesture pattern.
        /// </summary>
        /// <param name="recordedPath">The raw 3D path from the GestureRecorder.</param>
        /// <param name="pattern">The GesturePatternData to compare against.</param>
        /// <param name="camera">The camera used for recording, to correctly flatten the path.</param>
        /// <param name="debugMode">If true, will print detailed logs of the recognition process.</param>
        /// <returns>A score from 0.0 (no match) to 1.0 (perfect match).</returns>
        public static float Recognize(List<Vector3> recordedPath, GesturePatternData pattern, Camera camera, bool debugMode = false)
        {
            if (recordedPath.Count < 2 || pattern.waypoints.Count < 2)
            {
                return 0.0f;
            }

            // 1. Flatten the 3D path to 2D screen-space points
            List<Vector2> playerPoints2D = new List<Vector2>();
            foreach (var point3D in recordedPath)
            {
                playerPoints2D.Add(camera.WorldToScreenPoint(point3D));
            }

            // 2. Resample BOTH paths to a fixed number of points
            List<Vector2> resampledPlayerPoints = Resample(playerPoints2D, NUM_POINTS);
            List<Vector2> resampledPatternPoints = Resample(pattern.waypoints, NUM_POINTS);

            // 3. Normalize both point sets to have the same scale and origin
            List<Vector2> normalizedPlayerPoints = Normalize(resampledPlayerPoints, SQUARE_SIZE);
            List<Vector2> normalizedPatternPoints = Normalize(resampledPatternPoints, SQUARE_SIZE);

            if (debugMode)
            {
                UnityEngine.Debug.Log("--- GESTURE RECOGNIZER DEBUG ---");
                LogPoints("Normalized Player Gesture", normalizedPlayerPoints);
                LogPoints("Normalized Pre-defined Pattern", normalizedPatternPoints);
            }

            // 4. Compare with the predefined pattern
            return Compare(normalizedPlayerPoints, normalizedPatternPoints, debugMode);
        }

        private static void LogPoints(string header, List<Vector2> points)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(header);
            for (int i = 0; i < points.Count; i++)
            {
                sb.AppendLine($"  Point {i}: ({points[i].x:F3}, {points[i].y:F3})");
            }
            UnityEngine.Debug.Log(sb.ToString());
        }

        private static float PathLength(List<Vector2> points)
        {
            float length = 0;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1], points[i]);
            }
            return length;
        }

        private static List<Vector2> Resample(List<Vector2> points, int n)
        {
            if (points.Count < 2) return points;

            List<Vector2> newPoints = new List<Vector2> { points[0] };
            
            float totalLength = PathLength(points);
            // If it's just a single point with no length, we can't resample. Return N copies of that point.
            if (totalLength <= 0)
            {
                while (newPoints.Count < n) newPoints.Add(points[0]);
                return newPoints;
            }

            float interval = totalLength / (n - 1);
            float distanceCoveredOnPath = 0f;

            // Start at the second point in the original path
            for (int i = 1; i < points.Count; i++)
            {
                if (newPoints.Count >= n) break;
                
                float segmentLength = Vector2.Distance(points[i-1], points[i]);
                if (segmentLength > 0)
                {
                    // While there are more resampled points to be placed on this segment
                    while (distanceCoveredOnPath + segmentLength >= interval * newPoints.Count && newPoints.Count < n)
                    {
                        // How far into this segment do we need to go to place the next point
                        float distanceToNextSample = (interval * newPoints.Count) - distanceCoveredOnPath;
                        float t = distanceToNextSample / segmentLength; // Lerp ratio
                        
                        Vector2 newPoint = Vector2.Lerp(points[i - 1], points[i], t);
                        newPoints.Add(newPoint);
                    }
                }
                distanceCoveredOnPath += segmentLength;
            }

            // If we are short on points due to floating point precision, fill up with the last point.
            while (newPoints.Count < n)
            {
                newPoints.Add(points[points.Count - 1]);
            }

            return newPoints;
        }
        
        private static List<Vector2> Normalize(List<Vector2> points, float squareSize)
        {
            // Find the bounding box
            Rect boundingBox = new Rect(points[0].x, points[0].y, 0, 0);
            foreach (var point in points)
            {
                boundingBox.xMin = Mathf.Min(boundingBox.xMin, point.x);
                boundingBox.yMin = Mathf.Min(boundingBox.yMin, point.y);
                boundingBox.xMax = Mathf.Max(boundingBox.xMax, point.x);
                boundingBox.yMax = Mathf.Max(boundingBox.yMax, point.y);
            }

            float scale = Mathf.Max(boundingBox.width, boundingBox.height);

            // Add a guard against division by zero if the path has no size (e.g., a single point)
            if (scale <= float.Epsilon)
            {
                // Cannot normalize a zero-size path. Return points at origin.
                List<Vector2> zeroPoints = new List<Vector2>();
                for (int i = 0; i < points.Count; i++) zeroPoints.Add(Vector2.zero);
                return zeroPoints;
            }

            // Scale and translate
            List<Vector2> newPoints = new List<Vector2>();
            foreach (var point in points)
            {
                float qx = (point.x - boundingBox.center.x) / scale;
                float qy = (point.y - boundingBox.center.y) / scale;
                newPoints.Add(new Vector2(qx, qy) * squareSize);
            }
            return newPoints;
        }

        private static float Compare(List<Vector2> points, List<Vector2> pattern, bool debugMode)
        {
            // Assuming both lists are resampled to the same number of points
            float distance = 0;
            for (int i = 0; i < Mathf.Min(points.Count, pattern.Count); i++)
            {
                float d = Vector2.Distance(points[i], pattern[i]);
                if(debugMode) UnityEngine.Debug.Log($"  Compare Point {i}: Player({points[i].x:F2},{points[i].y:F2}) vs Pattern({pattern[i].x:F2},{pattern[i].y:F2}). Distance = {d:F3}");
                distance += d;
            }
            
            float averageDistance = distance / points.Count;

            // The score is 1 for a perfect match (avg distance = 0) and falls off from there.
            // The value 0.5 is a scaling factor that can be tuned.
            float score = Mathf.Max(1.0f - averageDistance / 0.5f, 0.0f);

            if (debugMode)
            {
                UnityEngine.Debug.Log($"Total Distance: {distance:F3}, Average Distance: {averageDistance:F3}");
                UnityEngine.Debug.Log($"Final Score: {score:F3}");
                UnityEngine.Debug.Log("------------------------------------");
            }

            return score;
        }
    }
} 