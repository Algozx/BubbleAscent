using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Shows the soap bubble: position, size, visibility and the shader's outline and glow colors.
    /// The collision circle is the bubble's radius; the quad is larger to leave room for the glow.
    /// </summary>
    public class BubbleView : MonoBehaviour
    {
        static readonly int MarginId = Shader.PropertyToID("_Margin");
        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

        [Tooltip("Quad child that draws the bubble (material Bubble).")]
        [SerializeField] SpriteRenderer body;
        [Tooltip("How much larger the quad is than the bubble, to fit the glow (1.5 = 50% extra).")]
        [SerializeField] float glowMargin = 1.5f;
        [Tooltip("Outline color around the bubble.")]
        [SerializeField] Color outlineColor = new Color(1f, 1f, 1f, 0.35f);
        [Tooltip("Outer glow color.")]
        [SerializeField] Color glowColor = new Color(0.56f, 0.91f, 1f, 0.45f);

        Material _material;

        void Awake()
        {
            if (body == null)
            {
                Debug.LogError("BubbleView: body not assigned.", this);
                enabled = false;
                return;
            }
            glowMargin = Mathf.Max(1f, glowMargin);
            // The child is laid out in units of the bubble diameter (this transform is scaled to it).
            body.transform.localScale = new Vector3(glowMargin, glowMargin, 1f);

            _material = body.material; // instance, written once
            _material.SetFloat(MarginId, glowMargin);
            _material.SetColor(OutlineColorId, outlineColor);
            _material.SetColor(GlowColorId, glowColor);
        }

        void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        /// <summary>Places the bubble at (x, y) with the given radius; hidden when not visible.</summary>
        public void Present(float x, float y, float radius, bool visible)
        {
            if (!enabled) return;
            Vector3 p = transform.position;
            transform.position = new Vector3(x, y, p.z);
            float d = radius * 2f;
            transform.localScale = new Vector3(d, d, 1f);
            if (body.enabled != visible) body.enabled = visible;
        }
    }
}
