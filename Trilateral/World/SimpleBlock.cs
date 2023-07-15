namespace Trilateral.World;

using vmodel;
using VRenderLib.Interface;
//This represents a type of block.
public class SimpleBlock : IBlock
{
    public SimpleBlock(VModel model, IRenderTexture texture, IRenderShader shader, string name, string id)
    {
        this.Model = model;
        this.Shader = shader;
        this.Texture = texture;
        this.Draw = true;
        this.Name = name;
        this.UUID = id;
    }
    public SimpleBlock(VModel model, IRenderTexture texture, IRenderShader shader, bool draw, string name, string id)
    {
        this.Model = model;
        this.Shader = shader;
        this.Texture = texture;
        this.Draw = draw;
        this.Name = name;
        this.UUID = id;
    }
    public VModel Model {get;}
    public IRenderTexture Texture {get;}

    public IRenderShader Shader {get;}
    public bool Draw {get;}
    public string Name {get;}
    public string UUID {get;}
}