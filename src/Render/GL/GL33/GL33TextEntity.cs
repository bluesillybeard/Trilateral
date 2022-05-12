namespace Voxelesque.Render.GL33;

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
    public GL33TextEntity(EntityPosition pos, string text, bool centerX, bool centerY, GL33Texture texture, GL33Shader shader, int id) : base(pos, null, texture, shader, id){
        _text = text;
        _mesh = new GL33Mesh(MeshGenerators.BasicText(text, centerX, centerY));
    }
}