namespace nbtsharp;

using System;
using System.Text;
public class NBTInt: INBTElement{
    public NBTInt(string name, int value){
        Name = name;
        ContainedInt = value;
    }
    public NBTInt(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.Int))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.Int + ".");
        int size = BitConverter.ToInt32(serializedData, 0);

        Name = ASCIIEncoding.ASCII.GetString(serializedData[5..(size-5)]);
        ContainedInt = BitConverter.ToInt32(serializedData, size-4);
    }
    public ENBTType Type => ENBTType.Int;

    public string Name { get; }
    public object Contained{get => ContainedInt; }
    public int ContainedInt { get; }

    public byte[] Serialize(){
        return INBTElement.AddHeader(this, ContainedInt);
    }
    public override string ToString(){
        return INBTElement.GetNBTString(this);
    }
}