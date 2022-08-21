namespace Render.GL33;

using Render.Util;
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

    //Personally, being forced to call the super constructor as the very first thing is a little dumb, because (like in this case) there is code that would normally need to run before the constructor.
    #pragma warning disable //disable the null warning, because the mesh is IMMEDIATELY set to a non-null value after the super constructor is called.
    public GL33TextEntity(EntityPosition pos, string text, bool centerX, bool centerY, GL33Texture texture, GL33Shader shader, int id, bool depthTest, IEntityBehavior? behavior)
    :base(pos, null, texture, shader, id, depthTest, behavior){
        #pragma warning enable
        _CenterX = centerX;
        _CenterY = centerY;
        _text = text;
        _mesh = new GL33Mesh(MeshGenerators.BasicText(text, centerX, centerY));
    }
}