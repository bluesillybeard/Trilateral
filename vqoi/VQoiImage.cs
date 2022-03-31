using VQoiSharp.Codec;

namespace VQoiSharp;

/// <summary>
/// QOI image.
/// </summary>
public class VQoiImage
{
    /// <summary>
    /// Raw pixel data.
    /// </summary>
    public byte[] Data { get; }
    
    /// <summary>
    /// Image width.
    /// </summary>
    public int Width { get; }
    
    /// <summary>
    /// Image height
    /// </summary>
    public int Height { get; }
    
    /// <summary>
    /// Channels.
    /// </summary>
    public VQoiChannels Channels { get; }
    
    /// <summary>
    /// Color space.
    /// </summary>
    public VQoiColorSpace ColorSpace { get; }
    
    /// <summary>
    /// Default constructor.
    /// </summary>
    public VQoiImage(byte[] data, int width, int height, VQoiChannels channels, VQoiColorSpace colorSpace = VQoiColorSpace.SRgb)
    {
        Data = data;
        Width = width;
        Height = height;
        Channels = channels;
        ColorSpace = colorSpace;
    }
}