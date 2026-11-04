using System;

namespace Paw.Presentation
{
    /// <summary>Fits the board and HUD to the same safe rectangle.</summary>
    public readonly struct SafeAreaLayout
    {
        public double HudScale { get; }
        public double CenterX { get; }
        public double CenterY { get; }
        public double CameraX { get; }
        public double CameraY { get; }
        public double CameraHalfHeight { get; }
        public bool FitHeight { get; }
        private SafeAreaLayout(double scale, double centerX, double centerY, double cameraX, double cameraY, double halfHeight, bool fitHeight)
        { HudScale = scale; CenterX = centerX; CenterY = centerY; CameraX = cameraX; CameraY = cameraY; CameraHalfHeight = halfHeight; FitHeight = fitHeight; }
        public static SafeAreaLayout Calculate(double screenWidth, double screenHeight, double safeX, double safeY, double safeWidth, double safeHeight, double boardWidth, double boardHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || safeWidth <= 0 || safeHeight <= 0 || boardWidth <= 0 || boardHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            double widthScale = screenWidth / boardWidth, heightScale = screenHeight / boardHeight;
            bool fitHeight = heightScale < widthScale;
            double screenScale = Math.Min(widthScale, heightScale);
            double safeScale = Math.Min(1, Math.Min(safeWidth / (boardWidth * screenScale), safeHeight / (boardHeight * screenScale)));
            double pixelsPerUnit = screenScale * safeScale;
            double cx = safeX + safeWidth * .5, cy = safeY + safeHeight * .5;
            return new SafeAreaLayout(safeScale, cx / screenWidth, cy / screenHeight,
                (screenWidth * .5 - cx) / pixelsPerUnit, (screenHeight * .5 - cy) / pixelsPerUnit,
                screenHeight / (2 * pixelsPerUnit), fitHeight);
        }
    }
}
