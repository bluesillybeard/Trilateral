namespace Voxelesque.World;

using vmodel;
using VRenderLib.Interface;
//This represents a type of block.
public class Block
{
    public Block(VModel model, IRenderTexture texture, IRenderShader shader, string name)
    {
        this.model = model;
        this.shader = shader;
        this.texture = texture;
        this.draw = true;
        this.name = name;
    }
    public Block(VModel model, IRenderTexture texture, IRenderShader shader, bool draw, string name)
    {
        this.model = model;
        this.shader = shader;
        this.texture = texture;
        this.draw = draw;
        this.name = name;
    }
    //Block models aren't actually loaded into the GPU - they are simply used to create the chunk meshes, which are what actually gets uploaded.
    public VModel model;
    public IRenderTexture texture;

    public IRenderShader shader; //The shader that should be used to render this block.
    public bool draw;
    public string name;
}