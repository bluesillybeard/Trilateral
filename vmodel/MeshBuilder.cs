using System.Collections.Generic;

namespace vmodel;

struct MeshVertex{
    public float[] values; //the actual data of this vertex

    public uint index; //The index that points to this vertex
    public int hash; //the hash code of the vertice's values

    public MeshVertex(float[] vals, uint ind){
        values = vals;
        hash = HashVertex(vals);
        index = ind;
    }
    public static int HashVertex(float[] vertex){ //I wrapped it in case I want to change the hash function to a non-default one.
        return vertex.GetHashCode();
    }
}

public class MeshBuilder{
    private Dictionary<int, MeshVertex> _vertexLookup; //for fast checking of weather a vertex exists or not
    private List<MeshVertex> _vertices;
    private List<uint> _indices;

    private EAttribute[] _attributes;
    private int _totalAttribs;


    public MeshBuilder(EAttribute[] attributes, int vertexCapacity = 16, int indexCapacity = 18){
        _attributes = attributes;
        _totalAttribs = 0;
        for(int i=0; i<attributes.Length; i++){
            EAttribute attribute = attributes[i];
            _totalAttribs += (int)attribute;
        }
        _vertexLookup = new Dictionary<int, MeshVertex>(vertexCapacity);
        _vertices = new List<MeshVertex>(vertexCapacity);
        _indices = new List<uint>(indexCapacity);
    }

    public MeshBuilder(EAttribute[] attributes, int totalAttrib, int vertexCapacity = 16, int indexCapacity = 18){
        _attributes = attributes;
        _totalAttribs = totalAttrib;
        _vertexLookup = new Dictionary<int, MeshVertex>(vertexCapacity);
        _vertices = new List<MeshVertex>(vertexCapacity);
        _indices = new List<uint>(indexCapacity);
    }

    public void AddVertex(params float[] vertex){ //adds a single vertex to the builder
        if(vertex.Length < _totalAttribs){
            throw new System.Exception("Not enough values in vertex."); //Shouldn't happen if the caller has any intelligence.
        }
        MeshVertex hashedVertex = new MeshVertex(vertex, 0);
        if(_vertexLookup.TryGetValue(hashedVertex.hash, out MeshVertex oldVertex)){
            //If the vertex exists, we add only the index
            _indices.Add(oldVertex.index);
        } else {
            //If it doesn't exist, we set the new index
            hashedVertex.index = (uint)_indices.Count-1;
            //Add it to the vertex lookup
            _vertexLookup.Add(hashedVertex.hash, hashedVertex);
            //and add it to the final mesh data
            _vertices.Add(hashedVertex);
            _indices.Add(hashedVertex.index);
        }
    }
    public void AddVertex(int[] mapping, params float[] vertex){ //adds a single vertex to the builder
        vertex = VModelUtils.ConvertVertex(vertex, mapping);
        if(vertex.Length < _totalAttribs){
            throw new System.Exception("Not enough values in vertex."); //Shouldn't happen if the caller has any intelligence.
        }
        MeshVertex hashedVertex = new MeshVertex(vertex, 0);
        if(_vertexLookup.TryGetValue(hashedVertex.hash, out MeshVertex oldVertex)){
            //If the vertex exists, we add only the index
            _indices.Add(oldVertex.index);
        } else {
            //If it doesn't exist, we set the new index
            hashedVertex.index = (uint)_indices.Count-1;
            //Add it to the vertex lookup
            _vertexLookup.Add(hashedVertex.hash, hashedVertex);
            //and add it to the final mesh data
            _vertices.Add(hashedVertex);
            _indices.Add(hashedVertex.index);
        }
    }
    public void AddLine(float[] v1, float[] v2){
        AddVertex(v1);
        AddVertex(v2);
    }
    public void AddLine(int[] mapping, float[] v1, float[] v2){
        AddVertex(mapping, v1);
        AddVertex(mapping, v2);
    }
    public void AddTri(float[] v1, float[] v2, float[] v3){
        AddVertex(v1);
        AddVertex(v2);
        AddVertex(v3);
    }
    public void AddTri(int[] mapping, float[] v1, float[] v2, float[] v3){
        AddVertex(mapping, v1);
        AddVertex(mapping, v2);
        AddVertex(mapping, v3);
    }
    public void AddQuad(float[] v1, float[] v2, float[] v3, float[] v4){
        AddVertex(v1);
        AddVertex(v2);
        AddVertex(v3);
        AddVertex(v4);
    }
    public void AddQuad(int[] mapping, float[] v1, float[] v2, float[] v3, float[] v4){
        AddVertex(mapping, v1);
        AddVertex(mapping, v2);
        AddVertex(mapping, v3);
        AddVertex(mapping, v4);
    }

    public void AddMany(params float[][] verts){
        foreach(float[] vert in verts){
            AddVertex(vert);
        }
    }
    public void AddMany(int[] mapping, params float[][] verts){
        foreach(float[] vert in verts){
            AddVertex(mapping, vert);
        }
    }
    public VMesh ToMesh(){
        //finalize the vertices into a form that's actually usable
        List<float> vertices = new List<float>(_vertices.Count*_totalAttribs);
        for(int v=0; v<_vertices.Count; v++){
            MeshVertex vertex = _vertices[v];
            vertices.AddRange(vertex.values);
        }
        
        //We can serve the indices raw, since those aren't stored in a special way as a builder

        return new VMesh(vertices.ToArray(), _indices.ToArray(), _attributes, null);

    }
}