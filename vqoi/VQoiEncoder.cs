using VQoiSharp.Codec;
using VQoiSharp.Exceptions;

using System;

namespace VQoiSharp;

/// <summary>
/// QOI encoder.
/// </summary>
public static class VQoiEncoder
{
    /// <summary>
    /// Encodes raw pixel data into QOI.
    /// </summary>
    /// <param name="image">QOI image.</param>
    /// <returns>Encoded image.</returns>
    /// <exception cref="QoiEncodingException">Thrown when image information is invalid.</exception>
    public static byte[] Encode(VQoiImage image, bool useModified)
    {
        if (image.Width == 0)
        {
            throw new VQoiEncodingException($"Invalid width: {image.Width}");
        }

        if (image.Height == 0 || image.Height >= VQoiCodec.MaxPixels / image.Width)
        {
            throw new VQoiEncodingException($"Invalid height: {image.Height}. Maximum for this image is {VQoiCodec.MaxPixels / image.Width - 1}");
        }

        int width = image.Width;
        int height = image.Height;
        byte channels = (byte)image.Channels;
        byte colorSpace = (byte)image.ColorSpace;
        byte[] pixels = image.Data;

        byte[] bytes = new byte[VQoiCodec.HeaderSize + VQoiCodec.Padding.Length + (width * height * channels)];

        uint[] indexLookup = null; //used by the modified FIFO version that I translated from https://github.com/phoboslab/qoi/blob/fifo/qoi.h
        if(useModified)indexLookup = new uint[VQoiCodec.LookupHashTableSize];
        
        uint indexPos = 0; //used by FIFO (modified)
        
        if(useModified){
            bytes[0] = (byte)(VQoiCodec.MagicModified >> 24);
            bytes[1] = (byte)(VQoiCodec.MagicModified >> 16);
            bytes[2] = (byte)(VQoiCodec.MagicModified >> 8);
            bytes[3] = (byte)VQoiCodec.MagicModified;
        } else {
            bytes[0] = (byte)(VQoiCodec.Magic >> 24);
            bytes[1] = (byte)(VQoiCodec.Magic >> 16);
            bytes[2] = (byte)(VQoiCodec.Magic >> 8);
            bytes[3] = (byte)VQoiCodec.Magic;
        }

        bytes[4] = (byte)(width >> 24);
        bytes[5] = (byte)(width >> 16);
        bytes[6] = (byte)(width >> 8);
        bytes[7] = (byte)width;

        bytes[8] = (byte)(height >> 24);
        bytes[9] = (byte)(height >> 16);
        bytes[10] = (byte)(height >> 8);
        bytes[11] = (byte)height;

        bytes[12] = channels;
        bytes[13] = colorSpace;

        byte[] index = new byte[VQoiCodec.HashTableSize * 4];

        byte prevR = 0;
        byte prevG = 0;
        byte prevB = 0;
        byte prevA = 255;

        byte r = 0;
        byte g = 0;
        byte b = 0;
        byte a = 255;

        int run = 0;
        int p = VQoiCodec.HeaderSize;
        bool hasAlpha = channels == 4;

        int pixelsLength = width * height * channels;
        int pixelsEnd = pixelsLength - channels;
        int counter = 0;

        for (int pxPos = 0; pxPos < pixelsLength; pxPos += channels)
        {
            //load the RGB value into a buffer
            r = pixels[pxPos];
            g = pixels[pxPos + 1];
            b = pixels[pxPos + 2];
            //account for alpha
            if (hasAlpha)
            {
                a = pixels[pxPos + 3];
            }
            //run
            if (RgbaEquals(prevR, prevG, prevB, prevA, r, g, b, a))
            {
                run++;
                //If we are at the end of a run, write it and reset run before continuing
                if (run == 62 || pxPos == pixelsEnd)
                {
                    bytes[p++] = (byte)(VQoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                //If we are at the end of a run, write it and reset run before continuing
                if (run > 0)
                {
                    bytes[p++] = (byte)(VQoiCodec.Run | (run - 1));
                    run = 0;
                }

                //find if the current color is the same as the hash code color.
                //expect a lot of "if using modified it's this, otherwise that"
                //since the root algorithm is the same, but how the hash table is used changes.
                int hash;
                bool indexEquals;
                if(useModified){
                    hash = VQoiCodec.CalculateLookupHashIndex(r, g, b, a);
                    indexEquals = RgbaEquals(r, g, b, a, index[indexLookup[hash/4]],index[indexLookup[hash/4]+1],index[indexLookup[hash/4]+2],index[indexLookup[hash/4]+3]);
                }
                else{
                    hash = VQoiCodec.CalculateHashTableIndex(r, g, b, a);
                    indexEquals = RgbaEquals(r, g, b, a, index[hash], index[hash + 1], index[hash + 2], index[hash + 3]);
                }
                if (indexEquals)
                {
                    //add the index, if it matches.
                    if(useModified)             
                        bytes[p++] = (byte)(VQoiCodec.Index | indexLookup[hash/4]/4);
                    else bytes[p++] = (byte)(VQoiCodec.Index | (hash / 4));
                }
                else
                {
                    if(useModified){
                        indexLookup[hash/4] = indexPos/4;

                        index[indexPos] = r;
                        index[indexPos + 1] = g;
                        index[indexPos + 2] = b;
                        index[indexPos + 3] = a;
                        
                        indexPos = (indexPos+4)%(VQoiCodec.HashTableSize*4);

                    } else {
                        index[hash] = r;
                        index[hash + 1] = g;
                        index[hash + 2] = b;
                        index[hash + 3] = a;
                    }

                    if (a == prevA)
                    {
                        int vr = r - prevR;
                        int vg = g - prevG;
                        int vb = b - prevB;

                        int vgr = vr - vg;
                        int vgb = vb - vg;

                        if (vr is > -3 and < 2 &&
                            vg is > -3 and < 2 &&
                            vb is > -3 and < 2)
                        {
                            counter++;
                            bytes[p++] = (byte)(VQoiCodec.Diff | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
                        }
                        else if (vgr is > -9 and < 8 &&
                                 vg is > -33 and < 32 &&
                                 vgb is > -9 and < 8
                                )
                        {
                            bytes[p++] = (byte)(VQoiCodec.Luma | (vg + 32));
                            bytes[p++] = (byte)((vgr + 8) << 4 | (vgb + 8));
                        }
                        else
                        {
                            bytes[p++] = VQoiCodec.Rgb;
                            bytes[p++] = r;
                            bytes[p++] = g;
                            bytes[p++] = b;
                        }
                    }
                    else
                    {
                        bytes[p++] = VQoiCodec.Rgba;
                        bytes[p++] = r;
                        bytes[p++] = g;
                        bytes[p++] = b;
                        bytes[p++] = a;
                    }
                }
            }

            prevR = r;
            prevG = g;
            prevB = b;
            prevA = a;
        }

        for (int padIdx = 0; padIdx < VQoiCodec.Padding.Length; padIdx++)
        {
            bytes[p + padIdx] = VQoiCodec.Padding[padIdx];
        }

        p += VQoiCodec.Padding.Length;

        return bytes[..p];
    }

    private static bool RgbaEquals(byte r1, byte g1, byte b1, byte a1, byte r2, byte g2, byte b2, byte a2) =>
        r1 == r2 &&
        g1 == g2 &&
        b1 == b2 &&
        a1 == a2;
}