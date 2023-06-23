namespace nbtsharp;
using System;
using System.Text;
using System.IO;
public class NBTFloatArr: INBTElement{
    public NBTFloatArr(string name, float[] value){
        _name = name;
        _value = value;
    }

    public NBTFloatArr(byte[] serializedData){
        if(serializedData[4] != ((byte)ENBTType.FloatArr))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.FloatArr + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        //check indices
        int index = Array.IndexOf<byte>(serializedData[5..size], 0)+5;
        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..index]);
        _value = new float[(size-index)/4];
        for(int i=0; i<_value.Length; i++){
            _value[i] = BitConverter.ToSingle(serializedData, index+i*4+1);
        }
    }

    public string Name{get => _name;}
    public object Contained{get => _value;}

    public float[] ContainedArray => _value;
    public void SetContainedArray(float[] val){
        _value = val;
    }
    public ENBTType Type => ENBTType.FloatArr;

    public byte[] Serialize(){
        byte[] valueBytes = new byte[_value.Length*sizeof(int)];
        for(int i=0; i<_value.Length; i++){
            int bits = BitConverter.SingleToInt32Bits(_value[i]);
            valueBytes[4*i+0] = (byte)(bits);
            valueBytes[4*i+1] = (byte)(bits>>8);
            valueBytes[4*i+2] = (byte)(bits>>16);
            valueBytes[4*i+3] = (byte)(bits>>24);
        }
        return INBTElement.AddHeader(this, valueBytes);
    }
    

    public override string ToString(){
        StringBuilder b = new StringBuilder();
        b.Append(_name.ToString()).Append(": {");
        foreach (float element in _value){
            b.Append(element).Append(", ");
        }
        b.Append("}");
        return b.ToString();    } 
    private float[] _value;
    private string _name;
}