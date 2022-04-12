using VQoiSharp.Codec;
using VQoiSharp.Exceptions;

using System;

namespace VQoiSharp;

/// <summary>
/// QOI decoder.
/// </summary>
public static class VQoiDecoder 
{
    /// <summary>
    /// Decodes QOI data into raw pixel data.
    /// </summary>
    /// <param name="data">QOI data</param>
    /// <returns>Decoding result.</returns>
    /// <exception cref="VQoiDecodingException">Thrown when data is invalid.</exception>
    public static VQoiImage Decode(byte[] data)
    {
        bool useModified = false;;
        if (data.Length < VQoiCodec.HeaderSize + VQoiCodec.Padding.Length)
        {
            throw new VQoiDecodingException("File too short");
        }
        
        if (!VQoiCodec.IsValidMagic(data[..4]))
        {
            if(!VQoiCodec.IsValidMagicModified(data[..4])){
                throw new VQoiDecodingException("Invalid file magic");
            }
            useModified = true; //if it's not a normal one, but instead a modified.
        }

        int width = data[4] << 24 | data[5] << 16 | data[6] << 8 | data[7];
        int height = data[8] << 24 | data[9] << 16 | data[10] << 8 | data[11];
        byte channels = data[12]; 
        var colorSpace = (VQoiColorSpace)data[13];

        if (width == 0)
        {
            throw new VQoiDecodingException($"Invalid width: {width}");
        }
        if (height == 0 || height >= VQoiCodec.MaxPixels / width)
        {
            throw new VQoiDecodingException($"Invalid height: {height}. Maximum for this image is {VQoiCodec.MaxPixels / width - 1}");
        }
        if (channels is not 3 and not 4)
        {
            throw new VQoiDecodingException($"Invalid number of channels: {channels}");
        }
        
        byte[] index = new byte[VQoiCodec.HashTableSize * 4];
        //if (channels == 3) // TODO: delete
        //{
        //    for (int indexPos0 = 3; indexPos0 < index.Length; indexPos0 += 4)
        //    {
        //        index[indexPos0] = 255;
        //    }
        //}

        byte[] pixels = new byte[width * height * channels];
        
        byte r = 0;
        byte g = 0;
        byte b = 0;
        byte a = 255;
        
        int run = 0;
        int p = VQoiCodec.HeaderSize;
        int indexPos = 0; //used in the modified version

        for (int pxPos = 0; pxPos < pixels.Length && p < data.Length; pxPos += channels)
        {
            if (run > 0)
            {
                run--;
            }
            else
            {

                byte b1 = data[p++];

                if (b1 == VQoiCodec.Rgb)
                {
                    r = data[p++];
                    g = data[p++];
                    b = data[p++];
                    if(useModified){
                        index[indexPos] = r;
                        index[indexPos+1] = g;
                        index[indexPos+2] = b;
                        index[indexPos+3] = a;
                        indexPos = (indexPos+4)%(VQoiCodec.HashTableSize*4);
                    }
                }
                else if (b1 == VQoiCodec.Rgba)
                {
                    if(channels == 3){
                        throw new VQoiDecodingException("Cannot have RGBA block in an RGB image");
                    }
                    r = data[p++];
                    g = data[p++];
                    b = data[p++];
                    a = data[p++];
                    if(useModified){
                        index[indexPos] = r;
                        index[indexPos+1] = g;
                        index[indexPos+2] = b;
                        index[indexPos+3] = a;
                        indexPos = (indexPos+4)%(VQoiCodec.HashTableSize*4);
                    }
                }
                else if ((b1 & VQoiCodec.Mask2) == VQoiCodec.Index)
                {
                    int indexPos0 = (b1 & ~VQoiCodec.Mask2) * 4;
                    r = index[indexPos0];
                    g = index[indexPos0 + 1];
                    b = index[indexPos0 + 2];
                    a = index[indexPos0 + 3];
                }
                else if ((b1 & VQoiCodec.Mask2) == VQoiCodec.Diff)
                {
                    r += (byte)(((b1 >> 4) & 0x03) - 2);
                    g += (byte)(((b1 >> 2) & 0x03) - 2);
                    b += (byte)((b1 & 0x03) - 2);
                    if(useModified){
                        index[indexPos] = r;
                        index[indexPos+1] = g;
                        index[indexPos+2] = b;
                        index[indexPos+3] = a;
                        indexPos = (indexPos+4)%(VQoiCodec.HashTableSize*4);
                    }
                }
                else if ((b1 & VQoiCodec.Mask2) == VQoiCodec.Luma) 
                {
                    int b2 = data[p++];
                    int vg = (b1 & 0x3F) - 32;
                    r += (byte)(vg - 8 + ((b2 >> 4) & 0x0F));
                    g += (byte)vg;
                    b += (byte)(vg - 8 + (b2 & 0x0F));
                    if(useModified){
                        index[indexPos] = r;
                        index[indexPos+1] = g;
                        index[indexPos+2] = b;
                        index[indexPos+3] = a;
                        indexPos = (indexPos+4)%(VQoiCodec.HashTableSize*4);
                    }
                }
                else if ((b1 & VQoiCodec.Mask2) == VQoiCodec.Run) 
                {
                    run = b1 & 0x3F;
                }
                if(!useModified){
                    int indexPos2 = VQoiCodec.CalculateHashTableIndex(r, g, b, a);
                    index[indexPos2] = r;
                    index[indexPos2 + 1] = g;
                    index[indexPos2 + 2] = b;
                    index[indexPos2 + 3] = a;
                }
            }

            pixels[pxPos] = r;
            pixels[pxPos + 1] = g;
            pixels[pxPos + 2] = b;
            if (channels == 4)
            {
                pixels[pxPos + 3] = a;
            }
        }

        return new VQoiImage(pixels, width, height, (VQoiChannels)channels, colorSpace);
    }
}