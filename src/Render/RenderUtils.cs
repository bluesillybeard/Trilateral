using System;
using System.Threading;
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

        private static object _printMutex = new object();

        public const ConsoleColor DefaultBack = ConsoleColor.Black;
        public const ConsoleColor DefaultFront = ConsoleColor.White;

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
    }
}