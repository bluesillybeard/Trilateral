namespace nbtsharp;

using System;
using System.Text;
public class NBTFloat : INBTElement{
    public NBTFloat(string name, float value){
        Name = name;
        ContainedFloat = value;
    }
    public NBTFloat(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.Float))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.Float + ".");
        int size = BitConverter.ToInt32(serializedData, 0);//TODO: check endianess.

        Name = ASCIIEncoding.ASCII.GetString(serializedData[5..(size-5)]);//new string(serializedData, 5, size-10);
        ContainedFloat = BitConverter.ToSingle(serializedData, size-4); //check endianess and offset.
    }
    public ENBTType Type => ENBTType.Float;

    public string Name { get; }
    public object Contained{get => ContainedFloat; }
    public float ContainedFloat { get; }

    public byte[] Serialize(){
        int valueBytes = BitConverter.SingleToInt32Bits(ContainedFloat);
        return INBTElement.AddHeader(this, valueBytes);
    }
    public override string ToString(){
        return INBTElement.GetNBTString(this);
    }
}