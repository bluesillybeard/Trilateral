using System;
using System.Threading;


using libvmodel;
using OpenTK.Mathematics;

using OpenTK.Graphics.OpenGL;

namespace Voxelesque.Render{
    static class RenderUtils{
        public const double UpdateTime = 1.0/15.0;
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

        public static bool MeshCollides(VMesh mesh, Vector2 pos, Matrix4? transform){
            pos.Y *= -1;

            //in the Java version, I used temporary variables since they are always on the Heap anyway, so cache locality was an unfixable problem.
            // In C# however, Vectors are stack allocated (reference by value), and thus using temp variables on the Heap would actually result in WORSE performance
            // since it would have to access data from the Heap, which is less cache friendly.

            uint[] indices = mesh.indices;
            float[] vertices = mesh.vertices;
            const int elements = 8; //8 elements per vertex]
            //GL.UseProgram(0);
            //GL.PointSize(10);
            //GL.Begin(PrimitiveType.Points);
            //GL.Vertex2(pos);
            //GL.End();
            //GL.Begin(PrimitiveType.Triangles);
            //GL.Color4(1f, 0f, 1f, 1f);
            for(int i=0; i<indices.Length/3; i++){ //each triangle in the mesh
                //get the triangle vertices and transform the triangle to the screen coordinates.
                //We use Vector4s for the matrix transformation to work.

                uint t = elements*indices[3*i];
                Vector3 v1 = Vector3.TransformPerspective(new Vector3(vertices[t], vertices[t+1], vertices[t+2]), transform.Value);

                t = elements*indices[3*i+1];
                Vector3 v2 = Vector3.TransformPerspective(new Vector3(vertices[t], vertices[t+1], vertices[t+2]), transform.Value);
                
                t = elements*indices[3*i+2];
                Vector3 v3 = Vector3.TransformPerspective(new Vector3(vertices[t], vertices[t+1], vertices[t+2]), transform.Value);

                //if the triangle isn't behind the camera, and it touches the point, return true.'
                //if(v1.Z < 1.0f && v2.Z < 1.0f && v3.Z < 1.0f){
                //    GL.Vertex2(v1.X, v1.Y);
                //    GL.Vertex2(v2.X, v2.Y);
                //    GL.Vertex2(v3.X, v3.Y);
                //}
                if(v1.Z < 1.0f && v2.Z < 1.0f && v3.Z < 1.0f && IsInside(v1.Xy, v2.Xy, v3.Xy, pos)) {
                    //GL.End();
                    return true;
                }
            }
            //GL.End();
            return false;
        }


        //thanks to https://www.tutorialspoint.com/Check-whether-a-given-point-lies-inside-a-Triangle for the following code
        //I adapted it to fit my code better, and to fix a bug related to float precision

        public static double TriangleArea(Vector2 A, Vector2 B, Vector2 C) {
            return Math.Abs((A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y)) / 2.0);
        }

        public static bool IsInside(Vector2 A, Vector2 B, Vector2 C, Vector2 p) {
            double area  = TriangleArea(A, B, C) + .0000177;//area of triangle ABC with a tiny bit of extra to avoid issues related to float precision errors
            double area1 = TriangleArea(p, B, C);           //area of PBC
            double area2 = TriangleArea(A, p, C);           //area of APC
            double area3 = TriangleArea(A, B, p);           //area of ABP

            return (area >= area1 + area2 + area3);        //when three triangles are forming the whole triangle
            //I changed it to >= because floats cannot be trusted to hold perfectly accurate data,
        }
    }
}