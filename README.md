# Bubble Ascent

**[Play in the browser](https://algozx.github.io/BubbleAscent/)**

A one-button mobile game made in Unity. You are a soap bubble trying to rise from the sea floor to the sky.

Holding inflates the bubble and makes it rise faster, but a bigger bubble is also easier to hit. Releasing makes it
shrink and sink. The camera keeps going up on its own, so you can't just stay small and safe.

This started as the core of a bigger game I'm working on. I took the mechanic out into a clean project so the code can
be read on its own, without art, audio or anything else around it.

![Gameplay](Docs/screenshot.png)

## Controls

Hold the screen, the mouse button, Space or the gamepad south button. Release to shrink. One hit pops the bubble, and
holding again restarts right away.

## Technical choices

I wanted to explain a few decisions here, because some of them aren't the usual Unity way.

**The game rules are plain C#.** `BubbleMotor`, `ObstacleField` and `RunSession` are regular classes, and the
MonoBehaviours only read input and draw the result. At this size, writing everything as components would work too. I
went this way because the tests can run thousands of frames in a few milliseconds, without opening a scene or waiting
on real time.

**Every obstacle is fair.** The obstacles are random, but every gap fits the smallest bubble and there is always room
for the biggest one between obstacles. I didn't want a player to lose to a gap that was impossible to pass, so this is
enforced in code and tested over 2000 m of generated course.

**No Physics2D.** The bubble only moves up and down and the obstacles are rectangles, so a simple circle-vs-rectangle
check is enough. The bigger reason is that the collision and the fairness rule use the same numbers, so they can't
drift apart. If the game ever needs bouncing or rotation, I'd switch to Physics2D.

**Seeded courses.** The same seed always builds the same course, which makes bugs much easier to reproduce. Slow frames
are split into smaller steps, so a hitch never lets the bubble skip through an obstacle.

**One place for the numbers.** All the tuning lives in `GameTuning.asset`, a ScriptableObject.

**A shader for the bubble.** It's drawn on a single quad, with the rim, the color shift, a small wobble and the glow.

**A HUD that fits any screen.** It's a TextMeshPro prefab that adapts to the screen size and stays inside the phone's
safe area.

**A small, careful save.** The best height goes to a versioned JSON file, written atomically. For a single number,
PlayerPrefs would honestly be enough. I chose a file because it holds up better if the save grows later, and if the
file can't be read the game never overwrites it, so a record isn't lost by accident.

**Nothing allocated per frame.** Obstacles are pooled and the HUD only updates its text when a value changes.

## Tests

```
"C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml
```

## Editor tools

- `Bubble Ascent > Build Game Scene` rebuilds the scene.
- `Bubble Ascent > Rebuild HUD Prefab` rebuilds the HUD prefab.
- `Bubble Ascent > Capture Screenshot` updates `Docs/screenshot.png`.

## Built with

Unity 6000.6.0f1, URP 2D, Input System and TextMeshPro.
