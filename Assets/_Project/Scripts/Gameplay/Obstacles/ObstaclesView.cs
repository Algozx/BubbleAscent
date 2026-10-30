using System.Collections.Generic;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Draws the obstacles as pooled rounded-rectangle sprites (a gate is two walls, a crosser one block),
    /// colored by whether they lie in the sea or the sky. Drawing always matches the collision rectangles.
    /// </summary>
    public class ObstaclesView : MonoBehaviour
    {
        [Tooltip("9-sliced sprite renderer cloned for every obstacle piece.")]
        [SerializeField] SpriteRenderer template;
        [Tooltip("Color of obstacles below the sea surface.")]
        [SerializeField] Color seaColor = new Color(0.95f, 0.51f, 0.42f, 1f);
        [Tooltip("Color of obstacles above the sea surface.")]
        [SerializeField] Color skyColor = new Color(0.56f, 0.42f, 0.84f, 1f);
        [Tooltip("How far (+-x) the gate walls extend from the center, enough to cover the widest screen.")]
        [SerializeField] float gateOuterX = 6f;

        readonly List<SpriteRenderer> _pieces = new List<SpriteRenderer>();
        float _surfaceHeight;
        int _lastUsed;

        void Awake()
        {
            if (template == null)
            {
                Debug.LogError("ObstaclesView: template not assigned.", this);
                enabled = false;
                return;
            }
            template.enabled = false;
            _pieces.Add(template);
        }

        /// <param name="surfaceHeight">Sea surface height; obstacles above it use the sky color (0 = all sea).</param>
        public void Init(float surfaceHeight) => _surfaceHeight = surfaceHeight;

        /// <summary>Lays the pooled sprites over the field's obstacles at the given run time.</summary>
        public void Present(ObstacleField field, float runTime)
        {
            if (!enabled) return;

            int used = 0;
            IReadOnlyList<Obstacle> list = field.Obstacles;
            for (int i = 0; i < list.Count; i++)
            {
                Obstacle o = list[i];
                Color color = _surfaceHeight > 0f && o.Y >= _surfaceHeight ? skyColor : seaColor;
                float thickness = o.HalfThickness * 2f;
                if (o.Kind == ObstacleKind.Gate)
                {
                    float width = gateOuterX - o.GapHalf;
                    float center = o.GapHalf + width * 0.5f;
                    Place(used++, -center, o.Y, width, thickness, color);
                    Place(used++, center, o.Y, width, thickness, color);
                }
                else
                {
                    Place(used++, ObstacleField.CrosserX(o, runTime), o.Y, o.HalfWidth * 2f, thickness, color);
                }
            }

            for (int i = used; i < _lastUsed; i++) _pieces[i].enabled = false;
            _lastUsed = used;
        }

        void Place(int index, float x, float y, float width, float height, Color color)
        {
            SpriteRenderer piece = GetPiece(index);
            if (!piece.enabled) piece.enabled = true;
            Transform t = piece.transform;
            t.localPosition = new Vector3(x, y, 0f);
            piece.size = new Vector2(width, height);
            if (piece.color != color) piece.color = color;
        }

        // The pool only grows (a few allocations while the screen first fills up).
        SpriteRenderer GetPiece(int index)
        {
            while (index >= _pieces.Count)
                _pieces.Add(Instantiate(template, transform));
            return _pieces[index];
        }
    }
}
