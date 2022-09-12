namespace vmodel;

using System;
using System.Collections.Generic;

public sealed class MeshBuilder{
    private readonly Dictionary<int /*hash*/, Vertex> _vertexLookup;
    private readonly List<Vertex> _vertices;
    private readonly List<uint> _indices;

    public MeshBuilder(){
        _vertexLookup = new Dictionary<int, Vertex>();
        _vertices = new List<Vertex>();
        _indices = new List<uint>();
    }

    public MeshBuilder(int vertexCapacity, int indexCapacity){
        _vertexLookup = new Dictionary<int, Vertex>(vertexCapacity);
        _vertices = new List<Vertex>(vertexCapacity);
        _indices = new List<uint>(indexCapacity);
    }

    public void AddVertex(params float[] vert){
        int hash = Vertex.GenerateHash(vert);

        if(_vertexLookup.TryGetValue(hash, out var oldVert)){
            _indices.Add(oldVert.ind);
            return;
        }
        Vertex vertex = new Vertex(vert, (uint)_vertices.Count);
        _vertexLookup.Add(hash, vertex);
        _vertices.Add(vertex);
        _indices.Add(vertex.ind);

    }

    public void AddVertex(int[] mapping, params float[] vert){
        vert = VModelUtils.ConvertVertex(vert, mapping);
        AddVertex(vert);
    }

    public VMesh ToMesh(EAttribute[] attributes){
        List<float> vertices = new List<float>(_vertices.Count);
        for(int i=0; i<_vertices.Count; i++){
            vertices.AddRange(_vertices[i].vert);
        }
        return new VMesh(vertices.ToArray(), _indices.ToArray(), attributes, null);
    }

    struct Vertex{
        readonly public float[] vert; //the actual vertex data
        readonly public uint ind; //the index where this vertex can be found

        public Vertex(float[] vertex, uint index){
            vert = vertex;
            ind = index;
        }

        //public Vertex(float[] vertex, uint index, int hash_){
        //    vert = vertex;
        //    ind = index;
        //    hash = hash_;
        //}
        public static int GenerateHash(float[] vertex){
            HashCode hasher = new HashCode();
            foreach(float f in vertex){
                hasher.Add(BitConverter.SingleToInt32Bits(f));
            }
            return hasher.ToHashCode();
        }
    }
}

/*

#define VertexLookup //weather or not to use a vertex lookup hash table. Used for testing the efficacy of the hask function.

using System.Collections.Generic;
using System;

namespace vmodel;

class MeshVertex{
    readonly public float[] values; //the actual data of this vertex

    public uint index; //The index that points to this vertex, WITHIN THE VERTICES, NOT THE INDICES YOU DOOFIS
    readonly public int hash; //the hash code of the vertice's values, NOT THE INDEX YA DOOFIS

    public MeshVertex(float[] vals, uint ind){
        values = vals;
        hash = HashValues(vals);
        index = ind;
    }

    public override int GetHashCode(){
        return hash;
    }

    public override bool Equals(object? obj)
    {
        if(obj is null)return false;
        var mesh = obj as MeshVertex;
        if(mesh is null)return false;
        return mesh.values.Equals(this.values);
    }

    public static bool operator== (MeshVertex? a, object? b){
        if(a is null && b is null)return true;
        if(a !=null)
            if(a.Equals(b))return true;
        return false;
    }

    public static bool operator!= (MeshVertex? a, object? b){
        if(a is null && b is null)return false;
        if(a !=null)
            if(a.Equals(b))return false;
        return true;
    }

    public static int HashValues(float[] vals){
        HashCode hasher = new HashCode();
        foreach(float f in vals){
            hasher.Add(BitConverter.SingleToInt32Bits(f));
        }
        return hasher.ToHashCode();
    }
}

public class MeshBuilder{
    #if VertexLookup
        private Dictionary<int, MeshVertex> _vertexLookup; //for fast checking of weather a vertex exists or not
    #endif
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
        #if VertexLookup
            _vertexLookup = new Dictionary<int, MeshVertex>(vertexCapacity);
        #endif
        _vertices = new List<MeshVertex>(vertexCapacity);
        _indices = new List<uint>(indexCapacity);
    }

    public MeshBuilder(EAttribute[] attributes, int totalAttrib, int vertexCapacity = 16, int indexCapacity = 18){
        _attributes = attributes;
        _totalAttribs = totalAttrib;
        #if VertexLookup
            _vertexLookup = new Dictionary<int, MeshVertex>(vertexCapacity);
        #endif
        _vertices = new List<MeshVertex>(vertexCapacity);
        _indices = new List<uint>(indexCapacity);
    }

    public void AddVertex(params float[] vertex){ //adds a single vertex to the builder
        if(vertex.Length < _totalAttribs){
            throw new System.Exception("Not enough values in vertex."); //Shouldn't happen if the caller has any intelligence.
        }
        MeshVertex hashedVertex = new MeshVertex(vertex, 0);
        #if VertexLookup
        if(_vertexLookup.TryGetValue(hashedVertex.hash, out MeshVertex? oldVertex)){
            //If the vertex exists, we add only the index
            _indices.Add(oldVertex.index);
        } else 
        #endif
        {
            //If it doesn't exist, we set the new index
            hashedVertex.index = (uint)_vertices.Count;
            //Add it to the vertex lookup
            #if VertexLookup
                _vertexLookup.Add(hashedVertex.hash, hashedVertex);
            #endif
            //and add it to the final mesh data
            _vertices.Add(hashedVertex);
            _indices.Add((uint)_indices.Count);
        }
    }
    public void AddVertex(int[] mapping, params float[] vertex){ //adds a single vertex to the builder
        vertex = VModelUtils.ConvertVertex(vertex, mapping);
        AddVertex(vertex);
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
*/