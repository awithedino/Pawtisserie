using NUnit.Framework;
using Paw.Presentation;

namespace Paw.Tests.EditMode
{
    public sealed class SafeAreaLayoutTests
    {
        [Test]
        public void FullScreenFitsReferenceWithoutCameraOffset()
        {
            var fit = SafeAreaLayout.Calculate(400, 800, 0, 0, 400, 800, 400, 800);
            Assert.AreEqual(1d, fit.HudScale); Assert.AreEqual(.5d, fit.CenterX); Assert.AreEqual(.5d, fit.CenterY);
            Assert.AreEqual(0d, fit.CameraX); Assert.AreEqual(0d, fit.CameraY); Assert.AreEqual(400d, fit.CameraHalfHeight);
        }
        [Test]
        public void UnevenSafeInsetsCenterAndScaleWorldAndHudTogether()
        {
            var fit = SafeAreaLayout.Calculate(400, 800, 0, 20, 400, 740, 400, 800);
            Assert.AreEqual(.925d, fit.HudScale, 1e-9);
            Assert.AreEqual(.4875d, fit.CenterY, 1e-9);
            Assert.AreEqual(10d, fit.CameraY * fit.HudScale, 1e-9);
            Assert.AreEqual(400d, fit.CameraHalfHeight * fit.HudScale, 1e-9);
        }
    }
}
