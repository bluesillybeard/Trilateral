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
        vert = ConvertVertex(vert, mapping);
        AddVertex(vert);
    }

    public VMesh ToMesh(EAttribute[] attributes){
        List<float> vertices = new List<float>(_vertices.Count);
        for(int i=0; i<_vertices.Count; i++){
            vertices.AddRange(_vertices[i].vert);
        }
        return new VMesh(vertices.ToArray(), _indices.ToArray(), attributes, null);
    }

    public static float[] ConvertVertex(float[] vertex, params int[] mapping){
        float[] ret = new float[mapping.Length];
        for(int i=0; i<mapping.Length; i++){
            int map = mapping[i];
            if(map != -1){
                ret[i] = vertex[map];
            }
        }
        return ret;
    }

    struct Vertex{
        readonly public float[] vert; //the actual vertex data
        readonly public uint ind; //the index where this vertex can be found

        public Vertex(float[] vertex, uint index){
            vert = vertex;
            ind = index;
        }
        public static int GenerateHash(float[] vertex){
            HashCode hasher = new HashCode();
            foreach(float f in vertex){
                hasher.Add(BitConverter.SingleToInt32Bits(f));
            }
            return hasher.ToHashCode();
        }
    }
}