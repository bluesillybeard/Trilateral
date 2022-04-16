using System;
using System.Threading;
using System.IO;

using VQoiSharp;
using VQoiSharp.Codec;

using StbImageSharp;
namespace Voxelesque.Render{
    static class RenderUtils{
        public const double Pid = 3.141592653589793238462643383279502884197169399375105820974944592307816406286;
        public const float Pif = (float)Pid;
        public const double DegreesToRadiansd = (2*Pid)/180;
        public const double RadiansToDegreesd = 180/(2*Pid);
        public const float DegreesToRadiansf = (2*Pif)/180;
        public const float RadiansToDegreesf = 180/(2*Pif);

        public static IRender CurrentRender;
        public static ERenderType CurrentRenderType;

        private static object _printMutex = new object(); //makes sure that the print messages don't get screwed up by concurrency

        public const ConsoleColor DefaultBack = ConsoleColor.Black;
        public const ConsoleColor DefaultFront = ConsoleColor.White;

        public const ConsoleColor WarnBack = ConsoleColor.Black;
        public const ConsoleColor WarnFront = ConsoleColor.Yellow;

        public const ConsoleColor ErrorBack = ConsoleColor.Black;
        public const ConsoleColor ErrorFront = ConsoleColor.Red;

        public static void print(object message){
            lock(_printMutex){
                Console.BackgroundColor = DefaultBack;
                Console.ForegroundColor = DefaultFront;
                Console.Write($"[{Thread.CurrentThread.Name}] {message}");
                Console.ResetColor();
            }
        }

        public static void printLn(object message){
            print(message);
            Console.WriteLine();
        }

        public static void printWarn(object message){
            lock(_printMutex){
                Console.BackgroundColor = WarnBack;
                Console.ForegroundColor = WarnFront;
                Console.Write($"[{Thread.CurrentThread.Name}] {message}");
                Console.ResetColor();
            }
        }

        public static void printWarnLn(object message){
            print(message);
            Console.WriteLine();
        }

        public static void printErr(object message){
            lock(_printMutex){
                Console.BackgroundColor = ErrorBack;
                Console.ForegroundColor = ErrorFront;
                Console.Write($"[{Thread.CurrentThread.Name}] {message}");
                Console.ResetColor();
            }
        }

        public static void printErrLn(object message){
            printErr(message);
            Console.WriteLine();
        }

        public static byte[] GetRawImageData(string ImagePath, out int width, out int height, out VQoiChannels channels){
            string lowerPath = ImagePath.ToLower();
            if(lowerPath.EndsWith(".vqoi") || lowerPath.EndsWith(".qoi")){
                VQoiImage image = VQoiDecoder.Decode(File.ReadAllBytes(ImagePath));
                width = image.Width;
                height = image.Height;
                channels = image.Channels;
                return image.Data;
            } else {
                ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(ImagePath), ColorComponents.RedGreenBlueAlpha);
                return GetRawImageData(image, out width, out height, out channels);
            }
        }
        public static byte[] GetRawImageData(ImageResult image, out int width, out int height, out VQoiChannels channels){
            width = image.Width;
            height = image.Height;
            channels = VQoiChannels.RgbWithAlpha;
            return image.Data;
        }


        public static VQoiImage GetRawImage(string ImagePath){
            string lowerPath = ImagePath.ToLower();
            if(lowerPath.EndsWith(".vqoi") || lowerPath.EndsWith(".qoi")){
                return VQoiDecoder.Decode(File.ReadAllBytes(ImagePath));
            } else {
                ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(ImagePath), ColorComponents.RedGreenBlueAlpha);
                return GetRawImage(image);
            }
        }
        public static VQoiImage GetRawImage(ImageResult image){
            return new VQoiImage(
                image.Data,
                image.Width,
                image.Height,
                VQoiChannels.RgbWithAlpha
            );
        }
    }
}