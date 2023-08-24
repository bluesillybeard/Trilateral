namespace Trilateral.Utility;
using System.Collections.Generic;
using System;
using System.Text;
using System.Threading;

public sealed class ProfileReport
{
    readonly Dictionary<int, ProfileInProgress> profiles;

    //This is because, despite the appearance, push and pop are thread-safe
    // However toString is not thread safe
    // I don't want push and pop to wait for each other
    // But ToString has to wait until there are no push or pop operations in progress.
    uint numberOfPushOrPopOperations;
    readonly object numberOfPushOrPopOperationsMutex;

    public ProfileReport()
    {
        profiles = new Dictionary<int, ProfileInProgress>();
        numberOfPushOrPopOperationsMutex = new object();
    }

    public void Push(string name, int thread, TimeSpan time)
    {
        lock(numberOfPushOrPopOperationsMutex)numberOfPushOrPopOperations++;
        //First, verify that this thread's root exist.
        if(!profiles.TryGetValue(thread, out var pair))
        {
            //If it doesn't, add it
            ProfileNode root = new("thread " + thread)
            {
                calls = 1
            };
            pair = new ProfileInProgress(root, root);
            lock(profiles) profiles.Add(thread, pair);
        }
        var current = pair.current;
        if(!current.TryGetChild(name, out var child) || child is null)
        {
            child = new ProfileNode(name);
            current.Add(child);
        }
        //Finally, we can actually update the profiling point with the newly pushed data
        child.lastPushTime = time;
        pair.current = child;
        lock(numberOfPushOrPopOperationsMutex)numberOfPushOrPopOperations--;
    }

    public void Pop(string name, int thread, TimeSpan time)
    {
        _ = name;
        lock(numberOfPushOrPopOperationsMutex)numberOfPushOrPopOperations++;
        //Check to make sure it exists
        if(!profiles.TryGetValue(thread, out var pair))
        {
            lock(numberOfPushOrPopOperationsMutex)numberOfPushOrPopOperations--;
            throw new Exception("This profile is invalid! A nonexistent value was popped.");
        }
        var current = pair.current;
        //Add the elapsed time to the total
        var delta = time - current.lastPushTime;
        current.totalDelta += delta;
        if(current.largestDelta < delta)
        {
            current.largestDelta = delta;
        }
        if(current.smallestDelta > delta)
        {
            current.smallestDelta = delta;
        }
        current.calls++;

        //And Then move back to the parent, since this node is finished.
        if(current.parent is null){
            lock(numberOfPushOrPopOperationsMutex)numberOfPushOrPopOperations--;
            return;
        }
        pair.current = current.parent;
        lock(numberOfPushOrPopOperationsMutex)numberOfPushOrPopOperations--;
    }
    public override string ToString()
    {
        //Spin wait, which is probably bad but I do't really care
        while(numberOfPushOrPopOperations > 0){}
        lock(numberOfPushOrPopOperationsMutex)
        {
            StringBuilder b = new();
            foreach(KeyValuePair<int, ProfileInProgress> profile in profiles)
            {
                profile.Value.root.Print(b, 0);
            }
            return b.ToString();
        }
    }
}
class ProfileInProgress
{
    public ProfileInProgress(ProfileNode root, ProfileNode current)
    {
        this.current = current;
        this.root = root;
    }
    //The current node; we are waiting for a pop on this one
    public ProfileNode current;
    //The rood node of this profile
    public ProfileNode root;
}
class ProfileNode
{
    public List<ProfileNode>? children;
    public ProfileNode? parent;
    public TimeSpan lastPushTime;// The last time this node was pushed to the profile tree.
    public TimeSpan totalDelta;
    public TimeSpan largestDelta;
    public TimeSpan smallestDelta;
    public uint calls;
    public string name;

    public bool HasChild(string name)
    {
        if(children is null)return false;
        foreach(var child in children)
        {
            if(child.name == name)
            {
                return true;
            }
        }
        return false;
    }

    public bool TryGetChild(string name, out ProfileNode? childOut)
    {
        childOut = null;
        if(children is null)return false;
        foreach(var child in children)
        {
            if(child.name == name)
            {
                childOut = child;
                return true;
            }
        }
        return false;
    }

    public void Add(ProfileNode child)
    {
        children ??= new List<ProfileNode>();
        children.Add(child);
        child.parent = this;
    }

    public ProfileNode(string name)
    {
        totalDelta = TimeSpan.Zero;
        largestDelta = TimeSpan.Zero;
        smallestDelta = TimeSpan.MaxValue;
        calls = 0;
        this.name = name;
    }

    public void Print(StringBuilder text, uint indent)
    {
        //add indent
        for(uint i=0; i<indent; i++)
        {
            text.Append('\t');
        }
        if(calls == 0) calls = 1;
        text.Append(name)
        .Append(" avg:").Append(totalDelta.Ticks / calls / (double)TimeSpan.TicksPerSecond)
        .Append(" min:").Append((smallestDelta.Ticks)/(double)TimeSpan.TicksPerSecond)
        .Append(" max:").Append((largestDelta.Ticks)/(double)TimeSpan.TicksPerSecond)
        .Append(" n:").Append(calls)
        .Append('\n');
        if(children is not null)
        {
            foreach(ProfileNode child in children)
            {
                child.Print(text, indent+1);
            }
            for(uint i=0; i<indent; i++)
            {
                text.Append('\t');
            }
            text.Append("END ").Append(name).Append('\n');
        }
    }
}
