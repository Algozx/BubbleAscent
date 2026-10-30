using System;
using System.IO;
using BubbleAscent.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BubbleAscent.EditorTools
{
    /// <summary>
    /// Renders one gameplay frame to Docs/screenshot.png: enters Play Mode, plays a scripted hold pattern for a
    /// few seconds on the real scene, then renders the camera (HUD included) to a RenderTexture.
    /// The same frame is also written at a tall phone and a tablet aspect to Logs/, to check the HUD layout.
    /// Batch: Unity.exe -batchmode -projectPath . -executeMethod BubbleAscent.EditorTools.ScreenshotCapture.Capture
    /// (no -nographics: it needs a graphics device).
    /// </summary>
    [InitializeOnLoad]
    public static class ScreenshotCapture
    {
        const string StageKey = "BubbleAscent.ScreenshotCapture.Stage";
        const string OutputPath = "Docs/screenshot.png";
        static readonly (string path, int width, int height)[] Shots =
        {
            (OutputPath, 720, 1280),          // 9:16
            ("Logs/hud-tall.png", 720, 1560),  // 9:19.5, modern phones
            ("Logs/hud-tablet.png", 960, 1280), // 3:4
        };
        const float StepSeconds = 1f / 60f;
        const float CaptureAtRunTime = 9f;
        const float MaxSeconds = 40f;
        const int WarmupFrames = 5;

        static int _frames;

        static ScreenshotCapture()
        {
            if (SessionState.GetInt(StageKey, 0) == 1) EditorApplication.update += OnUpdate;
        }

        [MenuItem("Bubble Ascent/Capture Screenshot")]
        public static void Capture()
        {
            if (!File.Exists(GameSceneBuilder.ScenePath)) GameSceneBuilder.Build();
            EditorSceneManager.OpenScene(GameSceneBuilder.ScenePath, OpenSceneMode.Single);
            SessionState.SetInt(StageKey, 1);
            _frames = 0;
            EditorApplication.update -= OnUpdate; // also covers projects with domain reload on entering Play Mode disabled
            EditorApplication.update += OnUpdate;
            Debug.Log("ScreenshotCapture: entering Play Mode");
            EditorApplication.EnterPlaymode();
        }

        static void OnUpdate()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (++_frames < WarmupFrames) return; // let Awake/Start run

            EditorApplication.update -= OnUpdate;
            SessionState.SetInt(StageKey, 0);
            Debug.Log("ScreenshotCapture: in Play Mode, driving the session");
            int exitCode = 0;
            try
            {
                exitCode = Run() ? 0 : 2;
            }
            catch (Exception e)
            {
                Debug.LogError($"ScreenshotCapture: {e}");
                exitCode = 1;
            }
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            else EditorApplication.ExitPlaymode();
        }

        static bool Run()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<GameController>();
            Camera cam = Camera.main;
            if (controller == null || controller.Session == null || cam == null)
                throw new InvalidOperationException("Scene is not set up (controller, session or camera missing).");

            controller.enabled = false; // this tool drives the session instead of the player
            RunSession session = controller.Session;
            bool first = true;
            for (float t = 0f; t < MaxSeconds; t += StepSeconds)
            {
                session.Tick(Hold(session), first, StepSeconds);
                first = false;
                if (session.State == RunState.Popped)
                {
                    Debug.LogWarning("ScreenshotCapture: the scripted bubble popped before the capture time; run again.");
                    return false;
                }
                if (session.RunTime >= CaptureAtRunTime)
                {
                    Render(controller, cam);
                    return true;
                }
            }
            return false;
        }

        // Scripted player: follow slightly above the camera center, shrink when a gate is close ahead.
        static bool Hold(RunSession s)
        {
            BubbleMotor bubble = s.Bubble;
            var list = s.Obstacles.Obstacles;
            for (int i = 0; i < list.Count; i++)
            {
                Obstacle o = list[i];
                float ahead = o.Y - o.HalfThickness - bubble.Y;
                if (o.Kind == ObstacleKind.Gate && ahead > -bubble.Radius && ahead < 1.5f && bubble.Radius > o.GapHalf * 0.85f)
                    return false;
            }
            return bubble.Y < s.ViewCenterY + 0.5f;
        }

        static void Render(GameController controller, Camera cam)
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) // overlay canvases are not part of a camera render: draw it through the camera
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            controller.Present();
            foreach (var (path, width, height) in Shots)
                RenderTo(cam, canvas, path, width, height);

            controller.Session.Pause(); // the prompt block, at the two extreme aspects
            controller.Present();
            RenderTo(cam, canvas, "Logs/hud-paused-tall.png", 720, 1560);
            RenderTo(cam, canvas, "Logs/hud-paused-tablet.png", 960, 1280);
        }

        static void RenderTo(Camera cam, Canvas canvas, string path, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            if (canvas != null) // let the scaler and the layout pick up the new size
            {
                var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null) { scaler.enabled = false; scaler.enabled = true; }
            }
            Canvas.ForceUpdateCanvases();

            var previous = RenderTexture.active;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;

            string full = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            Debug.Log($"ScreenshotCapture: wrote {full}");
            UnityEngine.Object.Destroy(tex);
            rt.Release();
        }
    }
}
