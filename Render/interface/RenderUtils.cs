namespace Voxelesque.Render{
    static class RenderUtils{
        public const double Pid = 3.141592653589793238462643383279502884197169399375105820974944592307816406286;
        public const float Pif = (float)Pid;
        public const double DegreesToRadiansd = (2*Pid)/180;
        public const double RadiansToDegreesd = 180/(2*Pid);
        public const float DegreesToRadiansf = (2*Pif)/180;
        public const float RadiansToDegreesf = 180/(2*Pif);

        public static IRender CurrentRender;
        public static ERenderType CurrentRenderType;
    }
}