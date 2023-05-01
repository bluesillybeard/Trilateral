#define USE_PROFILE

namespace Trilateral.Utility;

using System.IO;
using System;
using System.Text;
using System.Threading;

public static class Profiler
{
    private static DateTime start = DateTime.Now;
    #if USE_PROFILE 
    private static ProfileReport report = new ProfileReport();
    #endif
    public static void Push(string name)
    {
        #if USE_PROFILE
        report.Push(name, Environment.CurrentManagedThreadId, DateTime.Now - start);
        #endif
    }

    public static void Pop(string name)
    {
        #if USE_PROFILE
        report.Pop(name, Environment.CurrentManagedThreadId, DateTime.Now - start);
        #endif
    }

    public static void Dispose()
    {
        #if USE_PROFILE
        System.Console.WriteLine(report.ToString());
        #endif
    }
}
