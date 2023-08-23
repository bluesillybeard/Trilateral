#define USE_PROFILE

namespace Trilateral.Utility;

using System.IO;
using System;
using System.Text;
using System.Threading;
public struct ProfileSection : IDisposable
{
    public ProfileSection(string name)
    {
        Profiler.PushRaw(name);
        this.name = name;
    }
    string? name;

    public void Dispose()
    {
        if(name is null)return;
        Profiler.PopRaw(name);
        name = null;
    }
}
public static class Profiler
{
    private static readonly DateTime start = DateTime.Now;
    #if USE_PROFILE 
    private static readonly ProfileReport report = new();
    #endif
    public static void PushRaw(string name)
    {
        #if USE_PROFILE
        report.Push(name, Environment.CurrentManagedThreadId, DateTime.Now - start);
        #endif
    }

    public static void PopRaw(string name)
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

    public static ProfileSection Push(string name)
    {
        return new ProfileSection(name);
    }
}
