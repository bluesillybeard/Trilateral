namespace Voxelesque.Utility;

using System.IO;
using System;
using System.Text;
using System.Threading;
//This profiler is rediculously simple.
// For the time being, it just records a list of every event, and which thread submitted the event.

//TODO (VERY IMPORTANT) Create a CLI that creates a breakdown of one of these reports.
// It needs to make a different breakdown for every thread,
// and it needs to represent each breakdown hirarchically. (Perhaps just output a JSON file?)
public static class Profiler
{
    private static DateTime start = DateTime.Now;
    static ProfileReport report = new ProfileReport();
    public static void Push(string name)
    {
        report.Push(name, Environment.CurrentManagedThreadId, DateTime.Now-start);
    }

    public static void Pop(string name)
    {
        report.Pop(name, Environment.CurrentManagedThreadId, DateTime.Now-start);
    }

    public static void Dispose()
    {
        System.Console.WriteLine(report.ToString());
    }
}
