namespace nbtsharp;
using System;
using System.Text;
using System.IO;
public class NBTUIntArr: INBTElement{
    public NBTUIntArr(string name, uint[] value){
        _name = name;
        _value = value;
    }

    public NBTUIntArr(byte[] serializedData){
        if(serializedData[4] != ((byte)ENBTType.UIntArr))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.UIntArr + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        //check indices
        int index = Array.IndexOf<byte>(serializedData[5..size], 0)+5;
        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..index]);
        _value = new uint[(size-index)/4];
        for(int i=0; i<_value.Length; i++){
            _value[i] = BitConverter.ToUInt32(serializedData, index+i*4+1);
        }
    }

    public string Name{get => _name;}
    public object Contained{get => _value;}

    public uint[] ContainedArray => _value;
    public void SetContainedArray(uint[] val){
        this._value = val;
    }
    public ENBTType Type => ENBTType.UIntArr;

    public byte[] Serialize(){
        byte[] valueBytes = new byte[_value.Length*sizeof(int)];
        for(int i=0; i<_value.Length; i++){
            valueBytes[4*i+0] = (byte)(_value[i]);
            valueBytes[4*i+1] = (byte)(_value[i]>>8);
            valueBytes[4*i+2] = (byte)(_value[i]>>16);
            valueBytes[4*i+3] = (byte)(_value[i]>>24);
        }
        return INBTElement.AddHeader(this, valueBytes);
    }
    

    public override string ToString(){
        StringBuilder b = new StringBuilder();
        b.Append(_name.ToString()).Append(": {");
        foreach (uint element in _value){
            b.Append(element).Append(", ");
        }
        b.Append("}");
        return b.ToString();    } 
    private uint[] _value;
    private string _name;
}