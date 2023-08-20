# BRANCH PROJECT:
### Replace 'Java style' async code with the (not so new) C# async+await syntax.
## IMPORTANT NOTES:
- Async by itself does not spawn new threads- it simply uses interrupts to create efficient context switching within a single thread.
- use Task.Run instead of thread pools. It works more or less the same as the current thread pool system.
- - A downside of removing thread pools is less control, however that control is unnessesary and is not currently used anyway.
- Most of the changes should stay within the ChunkManager class and the classes/structs related to it.
- Because removing thread pools causes all tasks to share the same one, using async and await to split individual tasks apart will likely be a good idea
## IMPORTANT GOALS
- Remove thread pools. C# doesn't have them built-in for a reason. That will fix the problem of creating >2x as many threads as required
- make it more clear which methods can be called on which threads
- - three possibilities: Game thread, Main thread, pool thread
- - Main thread is reserved for tasks that require interacting with the OpenGL context
- - Note: might expand to four possibilities: Game, Frame, Main, Pool, since another goal is to separate rendering and updating
- - It might be worth making EVERYTHING run within a pool thread, to make things consistently async, but we'll see how doable that is.
- make rendering and updating asynchronous
- - In addition, make the GUI and camera update with the framerate, while gameplay-important items update with the 30 UPS clock
- - interpolation to make future things smoother, although nothing in the current engine can even use interpolation
- Make chunk building process less long-winded, and possibly optimize it a bit, although that's a lower priority

# Trilateral
Minecraft, except triangular

Trilateral is basically a Minecraft clo-- parody, where the boring cubes are replaced with triangular prisms.<br>
instead of the squares for top faces, they are triangles.

I watched a video (forgot exactly what, probably a cursed PhoenixSC video about hexagonal minecraft), <br>
and one of the comments talked about someone maybe actually making of that. <br>
I was already considering making my own game, and this seemed like a good way to start.<br>
After much thought, I decided that hexagons are not the bestagons in this case, so instead I opted for triangles.

Check out the [planning document](https://docs.google.com/document/d/1Fdh-ZeGf8YEUFWpwgS4D_PbiXnAFNS3MLjdPwnYaDAo) <br>
And the [Devlogs](https://youtu.be/M8LMCoB7KTM) <br>
[Short Casual Devlogs](https://www.youtube.com/channel/UCm83QMQFK20LsBOCGx5jYHA/videos)<br>

some short term goals I should achieve before I can call this a "complete game" (knowing the progress of this project it's gonna be another year or two):
- complete all of the features from the [Java prototype](https://github.com/bluesillybeard/VoxelesqueJava)
- - runtime generated texture atlas
- - basic content defined by config files instead of regular code
- gameplay loop
- basic biomes
- flora and fauna
