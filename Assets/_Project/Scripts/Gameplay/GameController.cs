using System;
using BubbleAscent.Core;
using BubbleAscent.Data;
using BubbleAscent.UI;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Binds input to the RunSession and the session's state to the views; saves the best height.
    /// Holds no game rules: those live in RunSession and its parts.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Tooltip("Asset with every balance number.")]
        [SerializeField] GameTuning tuning;
        [Tooltip("Where the hold comes from.")]
        [SerializeField] HoldInput input;
        [SerializeField] BubbleView bubbleView;
        [SerializeField] ObstaclesView obstaclesView;
        [SerializeField] BackgroundView background;
        [Tooltip("Camera that rises with the run; its size comes from the tuning.")]
        [SerializeField] Camera gameCamera;
        [SerializeField] HudView hud;

        RunSession _session;
        SaveData _save;
        bool _saveWritable;

        public RunSession Session => _session;

        void Awake()
        {
            if (tuning == null || input == null || bubbleView == null || obstaclesView == null ||
                background == null || gameCamera == null || hud == null)
            {
                Debug.LogError("GameController: missing references.", this);
                enabled = false;
                return;
            }

            _save = SaveStore.Load(SaveStore.DefaultPath, out _saveWritable);
            _session = new RunSession(tuning.bubble, tuning.ascent, tuning.obstacles, tuning.run,
                Environment.TickCount, _save.bestHeight, tuning.journey);
            gameCamera.orthographicSize = tuning.ascent.viewHalfHeight;
            obstaclesView.Init(tuning.journey.surfaceHeight);
            background.Init(tuning.journey.surfaceHeight);
            _session.Popped += OnPopped;
        }

        void OnDestroy()
        {
            if (_session != null) _session.Popped -= OnPopped;
        }

        void Start() => Present();

        void Update()
        {
            _session.Tick(input.IsHeld, input.WasPressedThisFrame, Time.deltaTime);
            Present();
        }

        // Leaving the app mid-run must never cost the run: freeze until the next press.
        void OnApplicationPause(bool paused)
        {
            if (paused && _session != null) _session.Pause();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && _session != null) _session.Pause();
        }

        void OnPopped()
        {
            // A save that exists but could not be read is never overwritten (it may hold a higher record).
            if (!_saveWritable || _session.Best <= _save.bestHeight) return;
            _save.bestHeight = _session.Best;
            SaveStore.Save(SaveStore.DefaultPath, _save);
        }

        /// <summary>Pushes the session's current state to every view.</summary>
        public void Present()
        {
            BubbleMotor bubble = _session.Bubble;
            bubbleView.Present(0f, bubble.Y, bubble.Radius, _session.State != RunState.Popped);

            Transform cam = gameCamera.transform;
            Vector3 c = cam.position;
            cam.position = new Vector3(c.x, _session.ViewCenterY, c.z);

            obstaclesView.Present(_session.Obstacles, _session.RunTime);
            background.Present(_session.ViewCenterY);
            hud.Present(_session);
        }
    }
}
