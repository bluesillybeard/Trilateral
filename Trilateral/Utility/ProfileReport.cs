namespace Trilateral.Utility;
using System.Collections.Generic;
using System;
using System.Text;
public class ProfileReport
{
    Dictionary<int, ProfileInProgress> profiles;

    public ProfileReport()
    {
        profiles = new Dictionary<int, ProfileInProgress>();
    }

    public void Push(string name, int thread, TimeSpan time)
    {
        //First, verify that this thread's root exist.
        if(!profiles.TryGetValue(thread, out var pair))
        {
            //If it doesn't, add it
            ProfileNode root = new ProfileNode("thread" + thread);
            root.calls = 1;
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

    }

    public void Pop(string name, int thread, TimeSpan time)
    {
        //Check to make sure it exists
        if(!profiles.TryGetValue(thread, out var pair))
        {
            throw new Exception("This profile is invalid! A nonexistent value was popped.");
        }
        var current = pair.current;
        //Add the elapsed time to the total
        var delta = (time - current.lastPushTime);
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
        if(current.parent is null)return;
        pair.current = current.parent;
    }
    public override string ToString()
    {
        StringBuilder b = new StringBuilder();
        foreach(KeyValuePair<int, ProfileInProgress> profile in profiles)
        {
            profile.Value.root.Print(b, 0);
        }
        return b.ToString();
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
        if(children is null) children = new List<ProfileNode>();
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
            text.Append("\t");
        }
        if(calls == 0) calls = 1;
        text.Append(name)
        .Append(" avg:").Append((totalDelta.Ticks / calls)/(double)TimeSpan.TicksPerSecond)
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
                text.Append("\t");
            }
            text.Append("END ").Append(name).Append('\n');
        }
    }
}

