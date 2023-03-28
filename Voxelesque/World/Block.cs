namespace Voxelesque.World;

using vmodel;
using VRender.Interface;
//This represents a type of block.
public struct Block
{
    public Block(VModel model, IRenderShader shader)
    {
        this.model = model;
        this.shader = shader;
    }
    //Block models aren't actually loaded into the GPU - they are simply used to create the chunk meshes, which are what actually gets uploaded.
    public VModel model;
    public IRenderShader shader; //The shader that should be used to render this block.
}