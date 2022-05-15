using OpenTK.Mathematics;

namespace Voxelesque.Render{
    struct EntityPosition{
        public static EntityPosition Zero = new EntityPosition(Vector3.Zero, Vector3.Zero, Vector3.One);

        public EntityPosition(Vector3 location_, Vector3 rotation, Vector3 scale_){
            location = location_;
            this.rotation = rotation;
            scale = scale_;

        }
        public Vector3 location;
        public Vector3 rotation;
        public Vector3 scale;
    }
}