using System;

using VRender.Interface;
using VRender;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Voxelesque.Game
{
    public static class Program
    {

        private static void Main()
        {
            //Some things for logging purposes
            System.Threading.Thread.CurrentThread.Name = "Main";
            System.Console.SetOut(new CustomOutTextWriter(System.Console.Out, ConsoleColor.White));
            System.Console.SetError(new CustomOutTextWriter(System.Console.Error, ConsoleColor.Red));
            var random = new Random((int)DateTime.Now.Ticks);
            var settings = new RenderSettings(){
                TargetFrameTime = 1f/60f,
                BackgroundColor = 0x000000ff,
                WindowTitle = "Voxelesque",
                size = new OpenTK.Mathematics.Vector2i(800, 600),
            };
            VRenderLib.InitRender(settings);
            VRenderLib.Render.OnStart += Start;
            VRenderLib.Render.Run();
        }

        private static void Start()
        {
            Voxelesque game = new Voxelesque();
        }
    }

    public class CustomOutTextWriter : TextWriter
    {
        public CustomOutTextWriter(TextWriter back, ConsoleColor color)
        {
            this.back = back;
            this.color = color;
        }
        private TextWriter back;
        private ConsoleColor color;
        public override Encoding Encoding {
            get => Encoding.UTF8;
        }

        private void WritePrefix()
        {
            System.Console.ForegroundColor = color;
            string? name = Thread.CurrentThread.Name;
            var id = Thread.CurrentThread.ManagedThreadId;
            back.Write($"[thread {id} {name}]");
        }
        public override void Write(ReadOnlySpan<char> buffer)
        {
            WritePrefix();
            back.Write(buffer);
        }

        public override void Write(StringBuilder? value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(bool value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(char value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            WritePrefix();
            back.Write(buffer, index, count);
        }

        public override void Write(char[]? buffer)
        {
            WritePrefix();
            back.Write(buffer);
        }

        public override void Write(decimal value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(double value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(float value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(int value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(long value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(object? value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0)
        {
            WritePrefix();
            back.Write(format, arg0);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1)
        {
            WritePrefix();
            back.Write(format, arg0, arg1);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2)
        {
            WritePrefix();
            back.Write(format, arg0, arg1, arg2);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, params object?[] arg)
        {
            WritePrefix();
            back.Write(format, arg);
        }

        public override void Write(string? value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(uint value)
        {
            WritePrefix();
            back.Write(value);
        }

        public override void Write(ulong value)
        {
            WritePrefix();
            back.Write(value);
        }
        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            WritePrefix();
            return back.WriteAsync(buffer, cancellationToken);
        }
        public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default)
        {
            WritePrefix();
            return back.WriteAsync(value, cancellationToken);
        }
        public override Task WriteAsync(char value)
        {
            WritePrefix();
            return back.WriteAsync(value);
        }
        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            WritePrefix();
            return back.WriteAsync(buffer, index, count);
        }
        public override Task WriteAsync(string? value)
        {
            WritePrefix();
            return back.WriteAsync(value);
        }
        public override void WriteLine()
        {
            WritePrefix();
            back.WriteLine();
        }
        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            WritePrefix();
            back.WriteLine(buffer);
        }
        public override void WriteLine(StringBuilder? value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(bool value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(char value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(char[] buffer, int index, int count)
        {
            WritePrefix();
            back.WriteLine(buffer, index, count);
        }
        public override void WriteLine(char[]? buffer)
        {
            WritePrefix();
            back.WriteLine(buffer);
        }
        public override void WriteLine(decimal value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(double value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(float value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(int value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(long value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(object? value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0)
        {
            WritePrefix();
            back.WriteLine(format, arg0);
        }
        public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1)
        {
            WritePrefix();
            back.WriteLine(format, arg0, arg1);
        }
        public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2)
        {
            WritePrefix();
            back.WriteLine(format, arg0, arg1, arg2);
        }
        public override void WriteLine([StringSyntax("CompositeFormat")] string format, params object?[] arg)
        {
            WritePrefix();
            back.WriteLine(format, arg);
        }
        public override void WriteLine(string? value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(uint value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override void WriteLine(ulong value)
        {
            WritePrefix();
            back.WriteLine(value);
        }
        public override Task WriteLineAsync()
        {
            WritePrefix();
            return back.WriteLineAsync();
        }
        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            WritePrefix();
            return back.WriteLineAsync(buffer, cancellationToken);
        }
        public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default)
        {
            WritePrefix();
            return back.WriteLineAsync(value, cancellationToken);
        }
        public override Task WriteLineAsync(char value)
        {
            WritePrefix();
            return back.WriteLineAsync(value);
        }
        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            WritePrefix();
            return back.WriteLineAsync(buffer, index, count);
        }
        public override Task WriteLineAsync(string? value)
        {
            WritePrefix();
            return back.WriteLineAsync(value);
        }
    }
}
