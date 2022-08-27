namespace vmodel;

//stores the deserialized data of a vmesh or vbmesh.
public struct VMesh{
    //members:
    public float[] vertices;
    public uint[] indices;
    public EAttribute[] attributes;
    public byte[]? removableTriangles;

    public VMesh(float[] vertices, uint[] indices, EAttribute[] attributes, byte[]? removableTriangles){
        this.vertices = vertices;
        this.indices = indices;
        this.attributes = attributes;
        this.removableTriangles = removableTriangles;
    }
}