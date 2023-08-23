namespace nbtsharp;

using System;
using System.Text;
public class NBTUInt: INBTElement{
    public NBTUInt(string name, uint value){
        Name = name;
        ContainedUint = value;
    }
    public NBTUInt(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.UInt))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.UInt + ".");
        int size = BitConverter.ToInt32(serializedData, 0);

        Name = ASCIIEncoding.ASCII.GetString(serializedData[5..(size-5)]);
        ContainedUint = BitConverter.ToUInt32(serializedData, size-4);
    }
    public ENBTType Type => ENBTType.UInt;

    public string Name { get; }
    public object Contained{get => ContainedUint; }
    public uint ContainedUint { get; }

    public byte[] Serialize(){
        return INBTElement.AddHeader(this, (int)ContainedUint);
    }
    public override string ToString(){
        return INBTElement.GetNBTString(this);
    }
}