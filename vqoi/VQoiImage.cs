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

    /// <summary>
    /// Create the "error texture" - black and magenta squares.
    /// </summary>
    public VQoiImage()
    {
        this.Data = new byte[]{
            255, 0, 255, 0,   0, 0,
            0,   0, 0,   255, 0, 255
        };
        this.Width = 2;
        this.Height = 2;
        this.Channels = VQoiChannels.Rgb;
        this.ColorSpace = VQoiColorSpace.SRgb;
    }
}