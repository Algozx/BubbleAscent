using BubbleAscent.Gameplay;
using TMPro;
using UnityEngine;

namespace BubbleAscent.UI
{
    /// <summary>
    /// Minimal HUD: current height, best height and the start / pop / restart prompts.
    /// Texts are rewritten only when what they show changes; numbers go through TMP's SetText, which does not allocate.
    /// The layout lives in the Hud prefab (Bubble Ascent > Rebuild HUD Prefab).
    /// </summary>
    public class HudView : MonoBehaviour
    {
        enum Prompt
        {
            None,
            Ready,
            Paused,
            Popped,
            PoppedCanRestart,
            NewBestCanRestart,
        }

        const string RestartHint = "hold to restart";
        const string MetersFormat = "{0}<size=55%> m</size>";

        [SerializeField] TMP_Text heightText;
        [SerializeField] TMP_Text bestText;
        [Tooltip("Big prompt line in the middle of the screen.")]
        [SerializeField] TMP_Text promptText;
        [Tooltip("Smaller line under the prompt.")]
        [SerializeField] TMP_Text hintText;
        [SerializeField] Color promptColor = Color.white;
        [Tooltip("Prompt color when the run just set a new best.")]
        [SerializeField] Color newBestColor = new Color(1f, 0.82f, 0.3f);

        int _shownHeight = -1;
        int _shownBest = -1;
        Prompt _shownPrompt = (Prompt)(-1);

        void Awake()
        {
            if (heightText == null || bestText == null || promptText == null || hintText == null)
            {
                Debug.LogError("HudView: texts not assigned.", this);
                enabled = false;
            }
        }

        /// <summary>Shows the session's current numbers and the prompt for its state.</summary>
        public void Present(RunSession session)
        {
            if (!enabled) return;

            int height = Mathf.Max(0, (int)session.PeakHeight);
            if (height != _shownHeight)
            {
                _shownHeight = height;
                heightText.SetText(MetersFormat, height);
            }

            int best = Mathf.Max(0, (int)session.Best);
            if (best != _shownBest)
            {
                _shownBest = best;
                bestText.SetText(MetersFormat, best);
            }

            Prompt prompt = PromptFor(session);
            if (prompt != _shownPrompt)
            {
                _shownPrompt = prompt;
                promptText.text = TitleFor(prompt);
                promptText.color = prompt == Prompt.NewBestCanRestart ? newBestColor : promptColor;
                hintText.text = HintFor(prompt);
            }
        }

        static Prompt PromptFor(RunSession session)
        {
            switch (session.State)
            {
                case RunState.Ready: return Prompt.Ready;
                case RunState.Paused: return Prompt.Paused;
                case RunState.Popped:
                    if (!session.CanRestart) return Prompt.Popped;
                    return session.LastPopWasNewBest ? Prompt.NewBestCanRestart : Prompt.PoppedCanRestart;
                default: return Prompt.None;
            }
        }

        static string TitleFor(Prompt prompt)
        {
            switch (prompt)
            {
                case Prompt.Ready: return "HOLD TO START";
                case Prompt.Paused: return "PAUSED";
                case Prompt.Popped:
                case Prompt.PoppedCanRestart: return "POPPED!";
                case Prompt.NewBestCanRestart: return "NEW BEST!";
                default: return string.Empty;
            }
        }

        static string HintFor(Prompt prompt)
        {
            switch (prompt)
            {
                case Prompt.Ready: return "hold: grow and rise\nrelease: shrink and sink";
                case Prompt.Paused: return "hold to resume";
                case Prompt.PoppedCanRestart:
                case Prompt.NewBestCanRestart: return RestartHint;
                default: return string.Empty;
            }
        }
    }
}
