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
    private static FileStream stream = new FileStream(("proflog" + DateTime.Now + ".vprofile").Replace(' ', '_').Replace('/', '.'), FileMode.CreateNew);
    private static DateTime start = DateTime.Now;
    public static void Push(string name)
    {
        //lock(stream)stream.Write(ASCIIEncoding.ASCII.GetBytes($"s\t{name}\t{Environment.CurrentManagedThreadId}\t{(DateTime.Now-start).Ticks}\n"));
    }

    public static void Pop(string name)
    {
        //lock(stream)stream.Write(ASCIIEncoding.ASCII.GetBytes($"e\t{name}\t{Environment.CurrentManagedThreadId}\t{(DateTime.Now-start).Ticks}\n"));
    }

    public static void Dispose()
    {
        // lock(stream)
        // {
        //     stream.Flush();
        //     stream.Dispose();
        // }
    }
}
