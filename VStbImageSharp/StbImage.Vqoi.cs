using Hebron.Runtime;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
    unsafe partial class StbImage
    {
        public static int stbi__vqoi_test(stbi__context s)
		{
            int r = 1;
            int i0 = stbi__get8(s);
            if(i0 == 'v'){
                //vqoi (modified)
                if(stbi__get8(s) != 'q')r = 0;
                else if(stbi__get8(s) != 'o')r = 0;
                else if(stbi__get8(s) != 'i')r = 0;
            } else if (i0 == 'q'){
                //qoif (unmodified)
                if(stbi__get8(s) != 'o')r = 0;
                else if(stbi__get8(s) != 'i')r = 0;
                else if(stbi__get8(s) != 'f')r = 0;
            } else {
                r = 0;
            }
			stbi__rewind(s);
			return r;
		}
        public static void* stbi__vqoi_load(stbi__context s, int* x, int* y, int* comp, int req_comp,
		stbi__result_info* ri)
        {
            System.IO.Stream data = s.Stream;
            //if(!data.CanRead){
            //    return (byte*)(ulong)(stbi__err("bad buffer"));
            //}
            //if(!data.CanSeek){
            //    return (byte*)(ulong)(stbi__err("bad buffer"));
            //}
            bool useModified = false;
            if (data.Length < 14 + 8)
            {
                return (byte*)(ulong)(stbi__err("bad vqoi"));
            }
            
            byte[] magic = new byte[4];
            data.Read(magic, 0, 4);
            //this relies on the file being KNOWN to be a vqoi of qoi file; it does not check for validity itself.
            if(magic[0] == 'v'){
                useModified = true;
            } else {
                useModified = false;
            }

            //We are already at index 4, so we can just continue reading forwards
            s.img_x = stbi__get32be(s);
            s.img_y = stbi__get32be(s);
            s.img_n = stbi__get8(s);
            //ri -> num_channels = s.img_n;
            *comp = s.img_n;
            *x = (int)s.img_x;
            *y = (int)s.img_y;
            byte colorSpace = stbi__get8(s);            

            if (s.img_x <= 0)
            {
                return (byte*)(ulong)(stbi__err("bad width"));
            }
            //check the maximum pixel count
            if (s.img_y <= 0)
            {
                return (byte*)(ulong)(stbi__err("bad height"));
            }
            if(s.img_y >= 400_000_000 / s.img_x)
            {
                return (byte*)(ulong)(stbi__err("bad size"));
            }
            //check number of channels
            if (s.img_n != 3 && s.img_n != 4)
            {
                return (byte*)(ulong)(stbi__err("bad channels"));
            }
            
            byte[] index = new byte[256]; //64 hashes * 4 elements

            long pixelsLength = (s.img_n * s.img_x * s.img_y);
            byte* pixels = (byte*)stbi__malloc((ulong)pixelsLength);
            if(pixels == null){
                return (byte*)(ulong)(stbi__err("outofmem"));
            }
            
            byte r = 0;
            byte g = 0;
            byte b = 0;
            byte a = 255;
            
            int run = 0;
            int p = 14; //skip the header
            int indexPos = 0; //used in the modified version

            for (int pxPos = 0; pxPos < pixelsLength && p < data.Length; pxPos += s.img_n)
            {
                if (run > 0)
                {
                    run--;
                }
                else
                {

                    byte b1 = stbi__get8(s);

                    if (b1 == 0xFE) //RGB
                    {
                        r = stbi__get8(s);
                        g = stbi__get8(s);
                        b = stbi__get8(s);
                        if(useModified){
                            index[indexPos] = r;
                            index[indexPos+1] = g;
                            index[indexPos+2] = b;
                            index[indexPos+3] = a;
                            indexPos = (indexPos+4)%(64*4);
                        }
                    }
                    else if (b1 == 0xFF) //RGBA
                    {
                        if(s.img_n == 3){
                            return (byte*)(ulong)(stbi__err("bad block"));
                        }
                        r = stbi__get8(s);
                        g = stbi__get8(s);
                        b = stbi__get8(s);
                        a = stbi__get8(s);
                        if(useModified){
                            index[indexPos] = r;
                            index[indexPos+1] = g;
                            index[indexPos+2] = b;
                            index[indexPos+3] = a;
                            indexPos = (indexPos+4)%(64*4);
                        }
                    }
                    else if ((b1 & 0xC0) == 0x00) //Index
                    {
                        int indexPos0 = (b1 & ~0xC0) * 4;
                        r = index[indexPos0];
                        g = index[indexPos0 + 1];
                        b = index[indexPos0 + 2];
                        a = index[indexPos0 + 3];
                    }
                    else if ((b1 & 0xC0) == 0x40) //Diff
                    {
                        r += (byte)(((b1 >> 4) & 0x03) - 2);
                        g += (byte)(((b1 >> 2) & 0x03) - 2);
                        b += (byte)((b1 & 0x03) - 2);
                        if(useModified){
                            index[indexPos] = r;
                            index[indexPos+1] = g;
                            index[indexPos+2] = b;
                            index[indexPos+3] = a;
                            indexPos = (indexPos+4)%(64*4);
                        }
                    }
                    else if ((b1 & 0xC0) == 0x80) //Luma 
                    {
                        int b2 = stbi__get8(s);
                        int vg = (b1 & 0x3F) - 32;
                        r += (byte)(vg - 8 + ((b2 >> 4) & 0x0F));
                        g += (byte)vg;
                        b += (byte)(vg - 8 + (b2 & 0x0F));
                        if(useModified){
                            index[indexPos] = r;
                            index[indexPos+1] = g;
                            index[indexPos+2] = b;
                            index[indexPos+3] = a;
                            indexPos = (indexPos+4)%(64*4);
                        }
                    }
                    else if ((b1 & 0xC0) == 0xC0) //Run 
                    {
                        run = b1 & 0x3F;
                    }
                    if(!useModified){
                        int indexPos2 = ((r & 0xFF) * 3 + (g & 0xFF) * 5 + (b & 0xFF) * 7 + (a & 0xFF) * 11) % 64 * 4; //hash index (0-63)
                        index[indexPos2] = r;
                        index[indexPos2 + 1] = g;
                        index[indexPos2 + 2] = b;
                        index[indexPos2 + 3] = a;
                    }
                }

                pixels[pxPos] = r;
                pixels[pxPos + 1] = g;
                pixels[pxPos + 2] = b;
                if (s.img_n == 4)
                {
                    pixels[pxPos + 3] = a;
                }
            }

            return pixels;
        }
    }
}