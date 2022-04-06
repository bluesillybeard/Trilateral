namespace VQoiSharp.Codec;

public enum VQoiColorSpace : byte
{
    /// <summary>
    /// Gamma scaled RGB channels and a linear alpha channel.
    /// </summary>
    SRgb = 0,
    
    /// <summary>
    /// All channels are linear.
    /// </summary>
    Linear = 1
}