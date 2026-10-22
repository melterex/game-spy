using SixLabors.ImageSharp.Processing;

namespace ImageService
{
    public record ImageProcessingOptions
    {
        public string? Size { get; init; }
        public string? Crop { get; init; }

        private static readonly Dictionary<string, AnchorPositionMode> CropPositions = 
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "center", AnchorPositionMode.Center },
            { "top", AnchorPositionMode.Top },
            { "bottom", AnchorPositionMode.Bottom },
            { "left", AnchorPositionMode.Left },
            { "right", AnchorPositionMode.Right }
        };

        public AnchorPositionMode GetAnchorPosition()
        {
            if (string.IsNullOrWhiteSpace(Crop) || !CropPositions.TryGetValue(Crop, out var position))
                return AnchorPositionMode.Center; 

            return position;
        }

        public ResizeMode GetResizeMode()
        {
            return !string.IsNullOrWhiteSpace(Crop) && CropPositions.ContainsKey(Crop)
                ? ResizeMode.Crop
                : ResizeMode.Max;
        }
    }
}