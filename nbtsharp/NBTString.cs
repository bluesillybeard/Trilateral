namespace nbtsharp;

using System;
using System.Text;
public class NBTString: INBTElement{
    public NBTString(string name, string value){
        Name = name;
        ContainedString = value;
    }
    public NBTString(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.String))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.String + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        int index = Array.IndexOf<byte>(serializedData[5..size], 0)+5;
        Name = ASCIIEncoding.ASCII.GetString(serializedData[5..index]);
        ContainedString = ASCIIEncoding.ASCII.GetString(serializedData[index..size]);
    }
    public ENBTType Type => ENBTType.String;

    public string Name { get; }
    public object Contained{get => ContainedString; }
    public string ContainedString { get; }

    public byte[] Serialize(){
        byte[] valueBytes = ASCIIEncoding.ASCII.GetBytes(ContainedString);
        return INBTElement.AddHeader(this, valueBytes);
    }

    public override string ToString(){
        return INBTElement.GetNBTString(this);
    }
}