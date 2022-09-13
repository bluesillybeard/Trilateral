using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using StbImageSharp;

using vmodel;

namespace Render.GL33{
    class GL33Render : IRender{
        public RenderSettings Settings {get => _settings;}

        public bool DebugRendering{
            get => _debugRendering;
            set => _debugRendering = value;
        }
        public bool Init(RenderSettings settings){
            try{
                _delayedEntities = new Stack<GL33Entity>();
                _delayedEntityRemovals = new Stack<GL33Entity>();
                _settings = settings;
                _deletedMeshes = new List<GL33MeshHandle>();
                _deletedTextures = new List<GL33TextureHandle>();
                _entities = new List<GL33Entity?>();
                _freeEntitySlots = new List<int>();


                _window = new NativeWindow(
                    new NativeWindowSettings(){
                        API = ContextAPI.OpenGL,
                        APIVersion = new System.Version(3, 3), //OpenGL 3.3
                        AutoLoadBindings = true,
                        NumberOfSamples = 0,
                        Profile = ContextProfile.Core,
                        Size = settings.Size,
                        StartFocused = false,
                        StartVisible = true,
                        Title = settings.WindowTitle,
                    }
                );

                _window.MakeCurrent();

                //the NativeWindow class has no way to do this, so we directly ask GLFW for it
                if(!settings.VSync){
                    OpenTK.Windowing.GraphicsLibraryFramework.GLFW.SwapInterval(0);
                } else{
                    OpenTK.Windowing.GraphicsLibraryFramework.GLFW.SwapInterval(1);
                }
                

                _window.Resize += OnResize;

                _window.KeyDown += OnKeyDownFunc;
                _window.KeyUp += OnKeyUpFunc;
                _window.MouseDown += OnMouseDownFunc;
                _window.MouseUp += OnMouseUpFunc;
                GL.Enable(EnableCap.DepthTest);
                //GL.Enable(EnableCap.CullFace); //Sadly, OpenGLs face culling system was designed to be extremely simple and not easily worked with.
                //My culling system is based on surface normals, so this simply won't do.

                IRender.CurrentRender = this;
                IRender.CurrentRenderType = ERenderType.GL33;
                return true;
            } catch (Exception e){
                Console.WriteLine("Error creating OpenGL 3.3 (GL33) window.\n\n" + e.StackTrace);
                return false;
            }
        }
        public Action<double>? OnUpdate {get; set;}
        public Action<double>? OnRender {get; set;}

        public Action<KeyboardKeyEventArgs>? OnKeyDown {get; set;}
        public Action<KeyboardKeyEventArgs>? OnKeyUp {get; set;}
        public Action<MouseButtonEventArgs>? OnMouseDown {get; set;}
        public Action<MouseButtonEventArgs>? OnMouseUp {get; set;}

        private void OnKeyDownFunc(KeyboardKeyEventArgs args){
            if(OnKeyDown != null)OnKeyDown.Invoke(args);
        }
        private void OnKeyUpFunc(KeyboardKeyEventArgs args){
            if(OnKeyUp != null)OnKeyUp.Invoke(args);
        }
        private void OnMouseDownFunc(MouseButtonEventArgs args){
            if(OnMouseDown != null)OnMouseDown.Invoke(args);
        }
        private void OnMouseUpFunc(MouseButtonEventArgs args){
            if(OnMouseUp != null)OnMouseUp.Invoke(args);
        }
        
        public void Run(){
            //I implimented my own game loop, because OpenTKs GameWindow doesn't update the keyboard state properly for the external OnUpdate event.
            long lastRenderTime = DateTime.Now.Ticks;
            _lastUpdateTime = lastRenderTime;
            while(!_window.IsExiting){
                bool didSomething = false;
                long time = DateTime.Now.Ticks; //10.000 ticks is 1ms. 10.000.000 ticks is 1s.
                if(time - lastRenderTime > 10_000_000*_settings.TargetFrameTime){
                    Render(lastRenderTime, time);
                    lastRenderTime = time;
                    didSomething = true;
                }
                if(time - _lastUpdateTime > 10_000_000*RenderUtils.UpdateTime){
                    Update(time, time - _lastUpdateTime);
                    _lastUpdateTime = time;
                    didSomething = true;
                }
                if(!didSomething){
                    System.Threading.Thread.Sleep(1); //sleep for 1 ms. This stops us from stealing all of the CPU time.
                }
            }
        }

        public Vector2 WindowSize(){
            return _window.Size;
        }

        public uint EntityCount(){
            return (uint)(_entities.Count - _freeEntitySlots.Count);
        }

        public uint EntityCapacity(){
            return (uint)_entities.Count;
        }

        //meshes
        public IRenderMesh? LoadMesh(string path, out Exception? err){
            VMesh? mesh = VModelUtils.LoadMesh(path, out err);
            if(mesh == null)return null;
            return new GL33Mesh(mesh.Value);
        }

        public IRenderMesh? LoadMesh(string path, bool dynamic, out Exception? err){
            VMesh? mesh = VModelUtils.LoadMesh(path, out err);
            if(mesh == null)return null;
            return new GL33Mesh(mesh.Value, dynamic);
        }

        public IRenderMesh LoadMesh(VMesh mesh){
            return new GL33Mesh(mesh);
        }
        public IRenderMesh LoadMesh(VMesh mesh, bool dynamic){
            return new GL33Mesh(mesh, dynamic);
        }
        public IRenderMesh LoadMesh(float[] vertices, uint[] indices, EAttribute[] attributes){
            return new GL33Mesh(attributes, vertices, indices);
        }

        public IRenderMesh LoadMesh(float[] vertices, uint[] indices, EAttribute[] attributes, bool dynamic){
            return new GL33Mesh(attributes, vertices, indices, dynamic);
        }

        public void DeleteMesh(IRenderMesh mesh){
            ((GL33Mesh)mesh).Dispose(); //any time this code is run, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
        }
        //textures

        public IRenderTexture LoadTexture(string path){
            return new GL33Texture(path);
        }

        public IRenderTexture LoadTexture(ImageResult image){
            return new GL33Texture(image);
        }
        public IRenderTexture LoadTexture(float r, float g, float b, float a){
            return new GL33Texture(r, g, b, a);
        }

        public IRenderTexture LoadTexture(IntPtr pixels, int width, int height, int channels){
            ImageResult image = new ImageResult();
            //first we marshall the data into an actual array
            image.Data = new byte[width*height*channels];
            Marshal.Copy(pixels, image.Data, 0, width*height*channels);

            //Not gonna lie, Java's switch statements are a billion times better than what C# has. C#'s switches are barely any better than just using an if-else chain,
            // Compared to Java which let you do much more advanced stuff like setting a value throug a switch like var x = switch(n){case 1 -> 3\n case 2->1}
            // Java even has nice enumeration support, where if you're doing an enum you only have to type the name, not enum.name for every case.

            /*This Java code don't work in C#
            image.Comp = switch(channels){
                case 1 -> ColorComponents.Grey;
                case 2 -> ColorComponents.GreyAlpha;
                case 3 -> ColorComponents.RedGreenBlue;
                case 3 -> ColorComponents.RedGreenBlueAlpha;
            }
            */
            //instead I have to do this monstrosity
            switch(channels){
                case 1:image.Comp = ColorComponents.Grey;break;
                case 2:image.Comp = ColorComponents.GreyAlpha;break;
                case 3:image.Comp = ColorComponents.RedGreenBlue;break;
                case 4:image.Comp = ColorComponents.RedGreenBlueAlpha;break;
            }
            //It's not actually that bad, but still worse than Java.

            image.Height = height;
            image.Width = width;
            image.SourceComp = image.Comp;
            return new GL33Texture(image);
        }

        public void DeleteTexture(IRenderTexture texture){
            ((GL33Texture)texture).Dispose(); //any time this code is run, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
        }
        //shaders

        public IRenderShader? LoadShader(string shader, out Exception? err){
            try{
                err = null;
                return new GL33Shader(shader + "vertex.glsl", shader + "fragment.glsl");
            }catch(Exception e){
                err = e;
                return null;
            }
        }

        public void DeleteShader(IRenderShader shader){
            ((GL33Shader)shader).Dispose();
        }

        //models
        public RenderEntityModel? LoadModel(string file, out List<VError>? err){
            //load the model data
            VModel? model = VModelUtils.LoadModel(file, out err);//new VModel(folder, file, out var ignored, out ICollection<string>? err);
            if(model == null)return null;
            //send it to the GPU
            GL33Mesh mesh = new GL33Mesh(model.Value.mesh);
            GL33Texture texture = new GL33Texture(model.Value.texture);

            if(err != null){
                RenderUtils.PrintErrLn(string.Join("/n", err));
            }
            return new RenderEntityModel(mesh, texture);
        }

        public RenderEntityModel LoadModel(VModel model){
            //send it to the GPU
            GL33Mesh mesh = new GL33Mesh(model.mesh);
            GL33Texture texture = new GL33Texture(model.texture);
            return new RenderEntityModel(mesh, texture);         
        }

        public void DeleteModel(RenderEntityModel model){
            ((GL33Mesh)model.mesh).Dispose();
            ((GL33Texture)model.texture).Dispose();
        }
        //entities
        public IRenderEntity SpawnEntity(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
            GL33Entity entity = new GL33Entity(pos, (GL33Mesh)mesh, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
            _AddEntity(entity);
            return entity;
        }

        public IRenderEntity SpawnEntityDelayed(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
            GL33Entity entity = new GL33Entity(pos, (GL33Mesh)mesh, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
            _delayedEntities.Push(entity);
            return entity;
        }

        public IRenderTextEntity SpawnTextEntity(EntityPosition pos, string text, bool centerX, bool centerY, IRenderShader shader, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
            GL33TextEntity entity = new GL33TextEntity(pos, text, centerX, centerY, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
            _AddEntity(entity);
            return entity;
        }
        public IRenderTextEntity SpawnTextEntityDelayed(EntityPosition pos, string text, bool centerX, bool centerY, IRenderShader shader, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
            GL33TextEntity entity = new GL33TextEntity(pos, text, centerX, centerY, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
            _delayedEntities.Push(entity);
            return entity;
        }

        public void DeleteEntity(IRenderEntity entity){
            GL33Entity glEntity = (GL33Entity)entity;
            if(glEntity.Id() < 0){
            RenderUtils.PrintErrLn("ERROR: entity index is negative! This should be impossible.");
                return;
            }
            _entities[glEntity.Id()] = null;//remove the entity
            _freeEntitySlots.Add(glEntity.Id()); //add its empty spot to the list
        }

        public void DeleteEntityDelayed(IRenderEntity entity){
            GL33Entity glEntity = (GL33Entity)entity;
            if(glEntity.Id() < 0){
            RenderUtils.PrintErrLn("ERROR: entity index is negative! This should be impossible.");
                return;
            }
            _delayedEntityRemovals.Push(glEntity);
        }

        private void _AddEntity(GL33Entity entity){
            if(_freeEntitySlots.Count > 0){
                int id = _freeEntitySlots[_freeEntitySlots.Count-1];
                _freeEntitySlots.RemoveAt(_freeEntitySlots.Count-1);
                _entities[id] = entity;
                entity.Id(id);
            } else {
                entity.Id(_entities.Count);
                _entities.Add(entity);
            }
        }

        public IEnumerable<IRenderEntity?> GetEntities(){
            return _entities;
        }
        //camera
        public RenderCamera SpawnCamera(Vector3 position, Vector3 rotation, float fovy){
            return new RenderCamera(position, rotation, fovy, _window.ClientSize);
        }
        public void SetCamera(RenderCamera camera){
            _camera = camera;
        }
        public RenderCamera GetCamera(){
            return _camera;
        }
        public void DeleteCamera(RenderCamera camera){
            if(camera == _camera){
                RenderUtils.PrintWarnLn("Cannot delete the active camera!");
            }
            //Cameras are handled by the C# runtime. Technically, this method is completely pointless.
        }
        //Input
        public KeyboardState Keyboard(){
            return _window.KeyboardState;
        }

        public MouseState Mouse(){
            return _window.MouseState;
        }

        public bool CursorLocked{
            get => _cursorLocked;
            set{
                _cursorLocked = value;
                _window.CursorVisible = !_cursorLocked;
                _window.CursorGrabbed = _cursorLocked;
            }
        }
        private void Update(long timeTicks, long deltaTicks){
            _window.ProcessEvents();
            //clear out deletion buffers

            //meshes
            lock(_deletedMeshes){
                if(_deletedMeshes.Count > 0){
                    RenderUtils.PrintWarnLn($"Clearing {_deletedMeshes.Count} leaked meshes - somebody (probably me) forgot to delete their meshes!");
                    foreach(GL33MeshHandle mesh in _deletedMeshes){
                        GL33Mesh.Dispose(mesh);
                    }
                _deletedMeshes.Clear();
                }
            }

            //textures
            lock(_deletedTextures){
                if(_deletedTextures.Count > 0){
                    RenderUtils.PrintWarnLn($"Clearing {_deletedTextures.Count} leaked textures - somebody (probably me) forgot to delete their textures!");
                    foreach(GL33TextureHandle mesh in _deletedTextures){
                        GL33Texture.Dispose(mesh);
                    }
                _deletedTextures.Clear();
                }
            }
            //I don't bother with shaders (yet) since they are usually small, and very few of them ever exist.
            //If it becomes an issue, then i'll add the deletion buffer for that.


            foreach(GL33Entity? entity in _entities){
                //update previous matrix values
                if(entity == null) continue;
                entity.lastTransform = entity.GetTransform();
            }

            if(_camera != null) _camera.lastTransform = _camera.GetTransform();

            //update events
            if(OnUpdate != null)OnUpdate.Invoke(deltaTicks/10_000_000.0);
            //update entity behaviors
            KeyboardState keyboard = Keyboard();
            MouseState mouse = Mouse();
            foreach(GL33Entity? entity in _entities){
                if(entity is null)continue;
                if(entity.Behavior is null)continue;
                entity.Behavior.Update(timeTicks/10_000_000.0, deltaTicks/10_000_000.0, entity, keyboard, mouse);
            }

            //process delayed entities
            while(_delayedEntities.Count > 0){
                _AddEntity(_delayedEntities.Pop());
            }
            while(_delayedEntityRemovals.Count > 0){
                DeleteEntity(_delayedEntityRemovals.Pop());
            }
            //RenderUtils.printLn("update");

        }
        private void Render(long lastRender, long now){
            //we call the OnRender event first, in case meshes are modified.
            // It doesn't matter that much, it just makes framerate-bound mesh updates feel more responsive.
            // (such as the ImGui integration, which uses such mesh updates to be able to display GUI through the actual IRender interface rather than try and do its own thing)
            float delta = (now - _lastUpdateTime)/10_000_000.0f;
            if(OnRender != null)OnRender.Invoke(delta);

            _window.MakeCurrent(); //make sure the window context is current. Technically not required, but it makes it slightly easier for if/when I add multiwindowing
            float weight = (float) (delta/RenderUtils.UpdateTime); //0=only last, 1=fully current'
            float rweight = 1-weight;
            Matrix4 interpolatedCamera;
            if(_camera != null){
                Matrix4 currentCamera = _camera.GetTransform();
                interpolatedCamera = new Matrix4(
                    currentCamera.Row0*weight + _camera.lastTransform.Row0*rweight,
                    currentCamera.Row1*weight + _camera.lastTransform.Row1*rweight,
                    currentCamera.Row2*weight + _camera.lastTransform.Row2*rweight,
                    currentCamera.Row3*weight + _camera.lastTransform.Row3*rweight
                );
            } else {
                interpolatedCamera = Matrix4.Identity;
            }



            foreach(GL33Entity? entity in _entities){
                if(entity is null)continue;
                entity._mesh.Bind();
                entity._texture.Use(TextureUnit.Texture0);
                entity._shader.Use();

                entity._shader.SetInt("tex", 0, true);

                Matrix4 currentView = entity.GetTransform();
                Matrix4 interpolatedEntityView = new Matrix4(
                    currentView.Row0*weight + entity.lastTransform.Row0*rweight,
                    currentView.Row1*weight + entity.lastTransform.Row1*rweight,
                    currentView.Row2*weight + entity.lastTransform.Row2*rweight,
                    currentView.Row3*weight + entity.lastTransform.Row3*rweight
                );

                entity._shader.SetMatrix4("model", interpolatedEntityView, false);
                if(_camera != null)entity._shader.SetMatrix4("camera", interpolatedCamera, false);
                else entity._shader.SetMatrix4("camera", Matrix4.Identity, false);

                if(entity._depthTest)GL.Enable(EnableCap.DepthTest);
                else GL.Disable(EnableCap.DepthTest);
                GL.DrawElements(BeginMode.Triangles, entity._mesh.ElementCount()*3, DrawElementsType.UnsignedInt, 0);
            }

            _window.Context.SwapBuffers();
            GL.ClearColor(0.5f, 0.5f, 0.5f, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }
        private void OnResize(ResizeEventArgs args){
            GL.Viewport(0, 0, args.Width, args.Height);
            if(_camera != null)_camera.Aspect = (float)args.Width/(float)args.Height;
            //this._window.Size
        }

        #pragma warning disable //the null checks are pointless here, since the initialization doesn't happen in the constructor.
        public List<GL33TextureHandle> _deletedTextures;

        public List<GL33MeshHandle> _deletedMeshes;

        private long _lastUpdateTime;

        private RenderSettings _settings;
        private NativeWindow _window;

        private List<GL33Entity?> _entities;

        private List<int> _freeEntitySlots;

        private RenderCamera _camera;

        private bool _cursorLocked;
        private bool _debugRendering;

        private Stack<GL33Entity> _delayedEntities;
        private Stack<GL33Entity> _delayedEntityRemovals;
        #pragma warning enable
    }
}