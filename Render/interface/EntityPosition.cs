using OpenTK.Mathematics;

namespace Voxelesque.Render{
    struct EntityPosition{

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