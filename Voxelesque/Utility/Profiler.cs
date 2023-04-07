namespace Voxelesque.Utility;

using System.IO;
using System;
using System.Text;
using System.Threading;
public static class Profiler
{
    private static DateTime start = DateTime.Now;
    private static ProfileReport report = new ProfileReport();
    public static void Push(string name)
    {
        report.Push(name, Environment.CurrentManagedThreadId, DateTime.Now - start);
    }

    public static void Pop(string name)
    {
        report.Pop(name, Environment.CurrentManagedThreadId, DateTime.Now - start);
    }

    public static void Dispose()
    {
        System.Console.WriteLine(report.ToString());
    }
}
