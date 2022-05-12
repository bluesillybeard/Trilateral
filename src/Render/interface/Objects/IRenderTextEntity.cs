namespace Voxelesque.Render;

interface IRenderTextEntity : IRenderEntity{
    string Text{get;set;}
    bool CenterX{get;set;}
    bool CenterY{get;set;}
}