using OpenTK.Mathematics;

namespace Render{
    class RenderableEntity{
        public RenderableEntity(Mesh mesh, Texture texture, Shader shader){
            _mesh = mesh;
            _texture = texture;
            _shader = shader;
        }
        public RenderableEntity(Mesh mesh, Texture texture, Shader shader, EntityPosition position){
            _mesh = mesh;
            _texture = texture;
            _shader = shader;
            this.pos = position;
        }
        public EntityPosition pos;
        public Mesh Mesh{get{return _mesh;} internal set{_mesh = value;}}
        public Texture Texture{get{return _texture;} internal set{_texture = value;}}
        public Shader Shader{get{return _shader;} internal set{_shader = value;}}

        public Matrix4 GetView(){
            return Matrix4.Identity *
            Matrix4.CreateScale(pos.scale) *
            Matrix4.CreateRotationX(pos.rot.X) *
            Matrix4.CreateRotationY(pos.rot.Y) *
            Matrix4.CreateRotationZ(pos.rot.Z) *
            Matrix4.CreateTranslation(pos.pos);
        }

        private Shader _shader;
        private Mesh _mesh;
        private Texture _texture;
    }
}