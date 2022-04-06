using OpenTK.Mathematics;

namespace Voxelesque.Render.GL33{
    class GL33Entity: GL33Object, IRenderEntity{
        public GL33Entity(EntityPosition pos, GL33Mesh mesh, GL33Texture texture, GL33Shader shader, int id){
            _position = pos;
            _mesh = mesh;
            _texture = texture;
            _shader = shader;
            _id = id;
        }

        public Matrix4 GetView(){
            return Matrix4.Identity *
            Matrix4.CreateScale(_position.scale) *
            Matrix4.CreateRotationX(_position.rotation.X) *
            Matrix4.CreateRotationY(_position.rotation.Y) *
            Matrix4.CreateRotationZ(_position.rotation.Z) *
            Matrix4.CreateTranslation(_position.location);
        }

        public EntityPosition Position{
            get{return _position;}
            set{_position = value;}
        }
        public Vector3 Location{
            get{return _position.location;}
            set{_position.location = value;}
        }
        public Vector3 Rotation{
            get{return _position.rotation;}
            set{_position.rotation = value;}
        }
        public Vector3 Scale{
            get{return _position.scale;}
            set{_position.scale = value;}
        }

        public float LocationX{
            get{return _position.location.X;} 
            set{_position.location.X = value;}
        }
        public float LocationY{
            get{return _position.location.Y;}
            set{_position.location.Y = value;}
        }
        public float LocationZ{
            get{return _position.location.Z;}
            set{_position.location.Z = value;}
        }

        public float RotationX{
            get{return _position.rotation.X;}
            set{_position.rotation.X = value;}
        }
        public float RotationY{
            get{return _position.rotation.Y;}
            set{_position.rotation.Y = value;}
        }
        public float RotationZ{
            get{return _position.rotation.Z;}
            set{_position.rotation.Z = value;}
        }

        public float ScaleX{
            get{return _position.scale.X;}
            set{_position.scale.X = value;}
        }
        public float ScaleY{
            get{return _position.scale.Y;}
            set{_position.scale.Y = value;}
        }
        public float ScaleZ{
            get{return _position.scale.Z;}
            set{_position.scale.Z = value;}
        }

        public IRenderMesh Mesh{
            get{return _mesh;}
            set{_mesh = (GL33Mesh)value;}
        }
        public IRenderShader Shader{
            get{return _shader;}
            set{_shader = (GL33Shader)value;}
        }

        public IRenderTexture Texture{
            get{return _texture;}
            set{_texture = (GL33Texture)value;}
        }

        public GL33Mesh _mesh;
        public GL33Shader _shader;

        public GL33Texture _texture;

        public void Id(int id){
            _id = id;
        }

        private EntityPosition _position;
    }
}