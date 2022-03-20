using System;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace Render
{
    public class Window : GameWindow
    {
        private readonly float[] _vertices =
        {
            // Position         Texture coordinates
             0.5f,  0.5f, 0.0f, 1.0f, 1.0f, // top right
             0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // bottom right
            -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, // bottom left
            -0.5f,  0.5f, 0.0f, 0.0f, 1.0f  // top left
        };

        private readonly uint[] _indices =
        {
            0, 1, 3,
            1, 2, 3
        };


        private LinkedList<RenderableEntity> _entities;

        private Camera _camera;
        private bool _cursorLocked = false;

        private Vector2 _lastCursorPos;
        private Vector2 _lastScrollOffset;

        private double _time;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            _entities = new LinkedList<RenderableEntity>();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.3f, 0.7f, 1.0f, 1.0f);

            GL.Enable(EnableCap.DepthTest);

            Mesh mesh = new Mesh(_vertices, _indices);


            Shader shader = new Shader("Resources/Shaders/vertex.glsl", "Resources/Shaders/fragment.glsl");
            shader.Use();

            Texture texture = Texture.LoadFromFile("Resources/container.png");
            texture.Use(TextureUnit.Texture0);

            shader.SetInt("texture0", 0);
            
            _entities.AddLast(new RenderableEntity(mesh, texture, shader));
            _entities.AddLast(new RenderableEntity(mesh, texture, shader, new EntityPosition(new Vector3(10, 0, 0), Vector3.Zero, Vector3.Zero)));



            // We initialize the camera so that it is 3 units back from where the rectangle is.
            // We also give it the proper aspect ratio.
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            _time += 4.0 * e.Time;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach(RenderableEntity entity in _entities){
                entity.Mesh.Bind();

                entity.Texture.Use(TextureUnit.Texture0);
                entity.Shader.Use();
                
                entity.pos.scale = Vector3.One;
                entity.pos.rot.X = (float)_time;
                entity.pos.pos.Y += (float)Math.Sin(_time/2.0);
                Matrix4 model = entity.GetView();
                entity.Shader.SetMatrix4("model", model);
                entity.Shader.SetMatrix4("camera", _camera.GetViewMatrix() * _camera.GetProjectionMatrix());

                GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            KeyboardState input = KeyboardState;

            if (input.IsKeyReleased(Keys.C))
            {
                //this is the ONLY case where the window absraction layer makes things worse.
                _cursorLocked = !_cursorLocked;
                CursorVisible = !_cursorLocked;
                CursorGrabbed = _cursorLocked;
            }

            const float cameraSpeed = 1.5f;
            const float sensitivity = 0.2f;

            if (input.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
            }

            if (input.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
            }
            if (input.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
            }
            if (input.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
            }
            if (input.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
            }

            // Get the mouse state
            MouseState mouse = MouseState;

            // Calculate the offset of the mouse position
            float deltaX = mouse.X - _lastCursorPos.X;
            float deltaY = mouse.Y - _lastCursorPos.Y;
            _lastCursorPos = new Vector2(mouse.X, mouse.Y);

            // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
            if(_cursorLocked || mouse.IsButtonDown(MouseButton.Middle)){
                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity; // Reversed since y-coordinates range from bottom to top
            }
        }

        // In the mouse wheel function, we manage all the zooming of the camera.
        // This is simply done by changing the FOV of the camera.
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            //_camera.Fov -= e.OffsetY;
            _camera.Fov -= e.Offset.Y - _lastScrollOffset.Y;
            _lastScrollOffset = e.Offset;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            // We need to update the aspect ratio once the window has been resized.
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }
    }
}
