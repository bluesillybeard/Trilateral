namespace vmodel;

using StbImageSharp;
public struct VModel{
    public VMesh mesh;
    public ImageResult texture;

    public byte? blockedFaces;
    public byte? blockableFaces;
    public VModel(VMesh mesh, ImageResult texture, byte? blockedFaces, byte? blockableFaces){
        this.mesh = mesh;
        this.texture = texture;
        this.blockedFaces = blockedFaces;
        this.blockableFaces = blockableFaces;
    }
}
