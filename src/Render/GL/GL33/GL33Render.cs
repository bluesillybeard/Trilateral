using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System;
using System.Collections.Generic;
using System.Drawing;

using Voxelesque.Render.Common;

namespace Voxelesque.Render.GL33{
    class GL33Render : IRender{
        public RenderSettings Settings {get; internal set;}

        public List<GL33TextureHandle> _deletedTextures;

        public List<GL33MeshHandle> _deletedMeshes;

        private RenderSettings _settings;
        private NativeWindow _window;

        private List<GL33Entity> _entities;

        private LinkedList<int> _freeEntitySlots;

        private RenderCamera _camera;

        private bool _cursorLocked;
        public bool Init(RenderSettings settings){
            try{
                _settings = settings;
                _deletedMeshes = new List<GL33MeshHandle>();
                _deletedTextures = new List<GL33TextureHandle>();
                _entities = new List<GL33Entity>();
                _freeEntitySlots = new LinkedList<int>();


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

                //todo: set swap interval to 0

                GL.Enable(EnableCap.DepthTest);
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
            long lastUpdateTime = lastRenderTime;
            while(!_window.IsExiting){
                bool didSomething = false;
                long time = DateTime.Now.Ticks; //10.000 ticks is 1ms. 10.000.000 ticks is 1s.
                if(time - lastRenderTime > 10_000_000*_settings.TargetFrameTime){
                    Render(time - lastRenderTime);
                    lastRenderTime = time;
                    didSomething = true;
                }
                if(time - lastUpdateTime > 10_000_000/15.0){
                    Update(time - lastUpdateTime);
                    lastUpdateTime = time;
                    didSomething = true;
                }
                if(!didSomething){
                    System.Threading.Thread.Sleep(1); //sleep for 1 ms. This stops us from stealing all of the CPU time.
                }
            }
        }

        //meshes
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

        public IRenderTexture LoadTexture(Bitmap image){
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

        //entities
        public IRenderEntity SpawnEntity(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture){
            if(shader is null || mesh is null || texture is null){
                System.Console.WriteLine("The Shader, Mesh, and/or Texture of an entity is null!");
                return null;
            }

            GL33Entity entity = new GL33Entity(pos, (GL33Mesh)mesh, (GL33Texture)texture, (GL33Shader)shader, 0);
            if(_freeEntitySlots.Count > 0){
                int id = _freeEntitySlots.First.Value;
                _freeEntitySlots.RemoveFirst();
                entity.Id(id);
            } else {
                _entities.Add(entity);
            }
            return entity;
        }

        public void DeleteEntity(IRenderEntity entity){
            GL33Entity glEntity = (GL33Entity)entity;
            if(glEntity.Id() < 0){
                System.Console.WriteLine("ERROR: entity index is negative! This should be impossible.");
                return;
            }
            _entities[glEntity.Id()] = null;//remove the entity
            _freeEntitySlots.AddLast(glEntity.Id()); //add its empty spot to the list
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
                System.Console.WriteLine("Cannot delete the active camera!");
            }
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
                    System.Console.WriteLine($"Clearing {_deletedMeshes.Count} leaked meshes - somebody (probably me) forgot to delete their meshes!");
                    foreach(GL33MeshHandle mesh in _deletedMeshes){
                        GL33Mesh.Dispose(mesh);
                    }
                _deletedMeshes.Clear();
                }
            }

            //textures
            lock(_deletedTextures){
                if(_deletedTextures.Count > 0){
                    System.Console.WriteLine($"Clearing {_deletedTextures.Count} leaked textures - somebody (probably me) forgot to delete their textures!");
                    foreach(GL33TextureHandle mesh in _deletedTextures){
                        GL33Texture.Dispose(mesh);
                    }
                _deletedTextures.Clear();
                }
            }
            //I don't bother with shaders (yet) since they are usually small, and very few of them ever exist.
            //If it becomes an issue, then i'll add the deletion buffer for that.


            //update events
            OnVoxelesqueUpdate.Invoke(ticks/10_000_000.0);
        }
        private void Render(long ticks){
            _window.MakeCurrent(); //make sure the window context is current. Technically not required, but it makes it slightly easier for if/when I add multiwindowing
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach(GL33Entity entity in _entities){
                if(entity is null)continue;
                entity._mesh.Bind();
                entity._texture.Use(TextureUnit.Texture0);
                entity._shader.Use();

                entity._shader.SetInt("tex", 0);
                entity._shader.SetMatrix4("model", entity.GetView());
                if(_camera != null)entity._shader.SetMatrix4("camera", _camera.GetTransform());
                else entity._shader.SetMatrix4("camera", Matrix4.Identity);
                GL.DrawElements(BeginMode.Triangles, entity._mesh.ElementCount(), DrawElementsType.UnsignedInt, 0);
            }

            _window.Context.SwapBuffers();
        }
    }
}