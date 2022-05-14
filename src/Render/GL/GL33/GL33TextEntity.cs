namespace Voxelesque.Render.GL33;

using Voxelesque.Render.Util;
class GL33TextEntity : GL33Entity, IRenderTextEntity{
    public string Text{
        get => _text;
        set {
            _text = value;
            _mesh.ReData(MeshGenerators.BasicText(_text, _CenterX, _CenterY));
        }
    }

    public bool CenterX{get => _CenterX; set=>_CenterX=value;}
    public bool CenterY{get => _CenterY; set=>_CenterY=value;}
    
    private bool _CenterX;
    private bool _CenterY;
    private string _text;
    //No mesh is provided since we generate that ourselves.

    //Personally I prever the Java way of calling the base constructor ourself, instead of having it automatically called. To each their own.
    public GL33TextEntity(EntityPosition pos, string text, bool centerX, bool centerY, GL33Texture texture, GL33Shader shader, int id) : base(pos, null, texture, shader, id){
        _CenterX = centerX;
        _CenterY = centerY;
        _text = text;
        _mesh = new GL33Mesh(MeshGenerators.BasicText(text, centerX, centerY));
    }
}