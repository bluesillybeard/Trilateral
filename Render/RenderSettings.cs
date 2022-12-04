using OpenTK.Mathematics;


namespace Render{
    public struct RenderSettings{
        //default
        public RenderSettings()
        {

        }

        //Make any non-default value using the curly brace syntax.

        public Vector2i Size = new Vector2i(800, 600);

        ///<summary>contains the starting assets directory</summary>
        public string Dir = "Resources/";

        public float Fov = 90*RenderUtils.DegreesToRadiansf;

        ///<summary> how long each frame should take. Frames may take shorter or longer. Defaults to 1/30 </summary>
        public double TargetFrameTime = 1.0/120.0;

        public bool VSync = false;

        public string WindowTitle = "Voxelesque window";
    
    }
}