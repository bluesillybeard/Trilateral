using OpenTK.Mathematics;

namespace Render{
    struct EntityPosition{

        public EntityPosition(Vector3 position, Vector3 rotation, Vector3 scale_){
            pos = position;
            rot = rotation;
            scale = scale_;

        }
        public Vector3 pos;
        public Vector3 rot;
        public Vector3 scale;
    }
}