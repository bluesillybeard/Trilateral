namespace nbtsharp;

using System;
using System.Text;
public class NBTFloat : INBTElement{
    public NBTFloat(string name, float value){
        _name = name;
        _value = value;
    }
    public NBTFloat(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.Float))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.Float + ".");
        int size = BitConverter.ToInt32(serializedData, 0);//TODO: check endianess.

        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..(size-5)]);//new string(serializedData, 5, size-10);
        _value = BitConverter.ToSingle(serializedData, size-4); //check endianess and offset.
    }
    public ENBTType Type => ENBTType.Float;

    public string Name{get => _name;}
    public object Contained{get => _value;}
    public float ContainedFloat{get => _value;}

    public byte[] Serialize(){
        int valueBytes = BitConverter.SingleToInt32Bits(_value);
        return INBTElement.AddHeader(this, valueBytes);
    }
    public override string ToString(){
        return INBTElement.GetNBTString(this);
    }
    private float _value;
    private string _name;
}