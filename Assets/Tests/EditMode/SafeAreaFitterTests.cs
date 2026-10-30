using BubbleAscent.UI;
using NUnit.Framework;
using UnityEngine;

namespace BubbleAscent.Tests
{
    public class SafeAreaFitterTests
    {
        [Test]
        public void FullScreen_GivesFullAnchors()
        {
            SafeAreaFitter.AnchorsFor(new Rect(0, 0, 1080, 1920), new Vector2(1080, 1920), out var min, out var max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void Notch_AndHomeIndicator_AreCutFromTopAndBottom()
        {
            // 1170 x 2532 phone with a 141 px notch and a 102 px home indicator.
            SafeAreaFitter.AnchorsFor(new Rect(0, 102, 1170, 2532 - 141 - 102), new Vector2(1170, 2532),
                out var min, out var max);
            Assert.AreEqual(0f, min.x, 1e-5f);
            Assert.AreEqual(102f / 2532f, min.y, 1e-5f);
            Assert.AreEqual(1f, max.x, 1e-5f);
            Assert.AreEqual(1f - 141f / 2532f, max.y, 1e-5f);
        }

        [Test]
        public void UnknownScreenSize_FallsBackToFullScreen()
        {
            SafeAreaFitter.AnchorsFor(new Rect(10, 10, 50, 50), Vector2.zero, out var min, out var max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
