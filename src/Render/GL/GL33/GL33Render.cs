using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System;
using System.Collections.Generic;

using StbImageSharp;

using libvmodel;

namespace Voxelesque.Render.GL33{
    class GL33Render : IRender{
        public RenderSettings Settings {get => _settings;}

        public List<GL33TextureHandle> _deletedTextures;

        public List<GL33MeshHandle> _deletedMeshes;

        private long _lastUpdateTime;

        private RenderSettings _settings;
        private NativeWindow _window;

        private List<GL33Entity> _entities;

        private List<int> _freeEntitySlots;

        private RenderCamera _camera;

        private bool _cursorLocked;
        private bool _debugRendering;
        public bool DebugRendering{
            get => _debugRendering;
            set => _debugRendering = value;
        }
        public bool Init(RenderSettings settings){
            try{
                _settings = settings;
                _deletedMeshes = new List<GL33MeshHandle>();
                _deletedTextures = new List<GL33TextureHandle>();
                _entities = new List<GL33Entity>();
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
                //(Setting the swap interval to 0.)
                OpenTK.Windowing.GraphicsLibraryFramework.GLFW.SwapInterval(0);

                _window.Resize += new Action<ResizeEventArgs>(OnResize);
                GL.Enable(EnableCap.DepthTest);
                //GL.Enable(EnableCap.CullFace); //Sadly, this face culling system was designed to be extremely simple and not easily worked with.
                //My culling system is based on surface normals, so this simply won't do.
                RenderUtils.CurrentRender = this;
                RenderUtils.CurrentRenderType = ERenderType.GL33;

                return true;
            } catch (Exception e){
                Console.WriteLine("Error creating OpenGL 3.3 (GL33) window.\n\n" + e.StackTrace);
                return false;
            }
        }
        public Action<double> OnVoxelesqueUpdate {get; set;}
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
                    Update(time - _lastUpdateTime);
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

        //meshes

        public IRenderMesh LoadMesh(string path){
            string lowerPath = path.ToLower();
            if(lowerPath.EndsWith(".vmesh") || lowerPath.EndsWith(".vbmesh")){
                return new GL33Mesh(path);
            } else {
                RenderUtils.printErr($"{path} is not a vmesh or vbmesh");
                return null;
            }
        }

        public IRenderMesh LoadMesh(VMesh mesh){
            return new GL33Mesh(mesh);
        }
        public IRenderMesh LoadMesh(float[] vertices, uint[] indices){
            return new GL33Mesh(vertices, indices);
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

        public void DeleteTexture(IRenderTexture texture){
            ((GL33Texture)texture).Dispose(); //any time this code is run, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
        }
        //shaders

        public IRenderShader LoadShader(string shader){
            return new GL33Shader(shader + "vertex.glsl", shader + "fragment.glsl");
        }

        public void DeleteShader(IRenderShader shader){
            ((GL33Shader)shader).Dispose();
        }

        //models
        public RenderEntityModel LoadModel(string folder, string file){
            //load the model data
            VModel model = new VModel(folder, file, out var ignored, out ICollection<string> err);
            //send it to the GPU
            GL33Mesh mesh = new GL33Mesh(model.mesh);
            GL33Texture texture = new GL33Texture(model.texture);

            if(err != null){
                RenderUtils.printErrLn(string.Join("/n", err));
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
        public IRenderEntity SpawnEntity(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture){
            if(shader is null || mesh is null || texture is null){
                RenderUtils.printErrLn("The Shader, Mesh, and/or Texture of an entity is null!");
                return null;
            }

            GL33Entity entity = new GL33Entity(pos, (GL33Mesh)mesh, (GL33Texture)texture, (GL33Shader)shader, 0);
            _AddEntity(entity);
            return entity;
        }

        public IRenderTextEntity SpawnTextEntity(EntityPosition pos, string text, bool centerX, bool centerY, IRenderShader shader, IRenderTexture texture){
            if(shader is null || text is null || texture is null){
                RenderUtils.printErrLn("The Shader, Text, and/or Texture of an entity is null!");
                return null;
            }
            GL33TextEntity entity = new GL33TextEntity(pos, text, centerX, centerY, (GL33Texture)texture, (GL33Shader)shader, 0);
            _AddEntity(entity);
            return entity;
        }

        public void DeleteEntity(IRenderEntity entity){
            GL33Entity glEntity = (GL33Entity)entity;
            if(glEntity.Id() < 0){
            RenderUtils.printErrLn("ERROR: entity index is negative! This should be impossible.");
                return;
            }
            _entities[glEntity.Id()] = null;//remove the entity
            _freeEntitySlots.Add(glEntity.Id()); //add its empty spot to the list
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

        public IEnumerable<IRenderEntity> GetEntities(){
            return _entities;
        }
        //camera
        public RenderCamera SpawnCamera(Vector3 position, Vector3 rotation, float fovy){
            return new RenderCamera(position, rotation, fovy, _window.ClientSize);
        }
        public void SetCamera(RenderCamera camera){
            _camera = camera;
        }
        public void DeleteCamera(RenderCamera camera){
            if(camera == _camera){
                RenderUtils.printWarnLn("Cannot delete the active camera!");
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
        private void Update(long ticks){
            _window.ProcessEvents();
            //clear out deletion buffers

            //meshes
            lock(_deletedMeshes){
                if(_deletedMeshes.Count > 0){
                    RenderUtils.printWarnLn($"Clearing {_deletedMeshes.Count} leaked meshes - somebody (probably me) forgot to delete their meshes!");
                    foreach(GL33MeshHandle mesh in _deletedMeshes){
                        GL33Mesh.Dispose(mesh);
                    }
                _deletedMeshes.Clear();
                }
            }

            //textures
            lock(_deletedTextures){
                if(_deletedTextures.Count > 0){
                    RenderUtils.printWarnLn($"Clearing {_deletedTextures.Count} leaked textures - somebody (probably me) forgot to delete their textures!");
                    foreach(GL33TextureHandle mesh in _deletedTextures){
                        GL33Texture.Dispose(mesh);
                    }
                _deletedTextures.Clear();
                }
            }
            //I don't bother with shaders (yet) since they are usually small, and very few of them ever exist.
            //If it becomes an issue, then i'll add the deletion buffer for that.


            foreach(GL33Entity entity in _entities){
                //update previous matrix values
                if(entity == null) continue;
                entity.lastTransform = entity.GetTransform();
            }

            _camera.lastTransform = _camera.GetTransform();

            //update events
            OnVoxelesqueUpdate.Invoke(ticks/10_000_000.0);
            //RenderUtils.printLn("update");

        }
        private void Render(long lastRender, long now){
            _window.MakeCurrent(); //make sure the window context is current. Technically not required, but it makes it slightly easier for if/when I add multiwindowing
            float delta = (now - _lastUpdateTime)/10_000_000.0f;
            float weight = (float) (delta/RenderUtils.UpdateTime); //0=only last, 1=fully current'
            float rweight = 1-weight;
            Matrix4 currentCamera = _camera.GetTransform();
            Matrix4 interpolatedCamera = new Matrix4(
                currentCamera.Row0*weight + _camera.lastTransform.Row0*rweight,
                currentCamera.Row1*weight + _camera.lastTransform.Row1*rweight,
                currentCamera.Row2*weight + _camera.lastTransform.Row2*rweight,
                currentCamera.Row3*weight + _camera.lastTransform.Row3*rweight
            );



            foreach(GL33Entity entity in _entities){
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
                GL.DrawElements(BeginMode.Triangles, entity._mesh.ElementCount(), DrawElementsType.UnsignedInt, 0);
            }

            _window.Context.SwapBuffers();
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }
        private void OnResize(ResizeEventArgs args){
            GL.Viewport(0, 0, args.Width, args.Height);
            _camera.Aspect = (float)args.Width/(float)args.Height;
            //this._window.Size
        }
    }
}