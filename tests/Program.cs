// See https://aka.ms/new-console-template for more information
namespace Tests;
using StbImageSharp;
using System.IO;
using VQoiSharp;
using VQoiSharp.Codec;
static class Program{

    static byte op(byte a, byte b){
        return (byte)(a ^ b);
    }
    static byte[] differentiate(byte[] src){
        byte[] diff = new byte[src.Length];

        diff[0] = op(0, src[0]);
        for(uint i=1; i<src.Length; ++i){
            diff[i] = op(src[i-1], src[i]);
        }
        return diff;
    }

    static byte[] differentiateRGB(byte[] src){
        byte[] diff = new byte[src.Length];
        diff[0] = op(0, src[0]);
        diff[1] = op(0, src[1]);
        diff[2] = op(0, src[2]);
        for(uint i=1; i<src.Length/3; ++i){
            diff[3*i  ] = op(src[3*(i-1)  ], src[3*i  ]);
            diff[3*i+1] = op(src[3*(i-1)+1], src[3*i+1]);
            diff[3*i+2] = op(src[3*(i-1)+2], src[3*i+2]);
        }
        return diff;
    }
    static byte[] undifferentiate(byte[] src){
        byte[] diff = new byte[src.Length];

        diff[0] = op(0, src[0]);
        for(uint i=1; i<src.Length; ++i){
            diff[i] = op(diff[i-1], src[i]);
        }
        return diff;
    }

    static byte[] undifferentiateRGB(byte[] src){
        byte[] diff = new byte[src.Length];
        diff[0] = op(0, src[0]);
        diff[1] = op(0, src[1]);
        diff[2] = op(0, src[2]);
        for(uint i=1; i<src.Length/3; ++i){
            diff[3*i  ] = op(diff[3*(i-1)  ], src[3*i  ]);
            diff[3*i+1] = op(diff[3*(i-1)+1], src[3*i+1]);
            diff[3*i+2] = op(diff[3*(i-1)+2], src[3*i+2]);
        }
        return diff;
    }
    static void Main(){
        string filePath = "/home/bluesillybeard/VSCodeProjects/Voxelesque/src/Resources/vmf/texture/GrassBlock.png";
        ImageResult img = ImageResult.FromMemory(File.ReadAllBytes(filePath));
        
        byte[] diff = differentiateRGB(img.Data);
        //byte[] undiff = undifferentiateRGB(diff);

        //for(int i=0; i<diff.Length; ++i){
        //    if(img.Data[i] != undiff[i]){
        //        System.Console.WriteLine("yeetis");
        //    }
        //}

        File.WriteAllBytes(filePath + ".data", img.Data);
        File.WriteAllBytes(filePath + ".vqoi", VQoiEncoder.Encode(new VQoiImage(img.Data, img.Width, img.Height, (VQoiChannels)3), true));
        File.WriteAllBytes(filePath + ".qoi", VQoiEncoder.Encode(new VQoiImage(img.Data, img.Width, img.Height, (VQoiChannels)3), false));
        
        //filePath = "/home/bluesillybeard/VSCodeProjects/Voxelesque/src/Resources/vmf/texture/GrassBlock.png";
        //img = ImageResult.FromMemory(File.ReadAllBytes(filePath));
        File.WriteAllBytes(filePath + ".diff.data", diff);
        File.WriteAllBytes(filePath + ".diff.vqoi", VQoiEncoder.Encode(new VQoiImage(diff, img.Width, img.Height, (VQoiChannels)3), true));
        File.WriteAllBytes(filePath + ".diff.qoi", VQoiEncoder.Encode(new VQoiImage(diff, img.Width, img.Height, (VQoiChannels)3), false));
    }
}