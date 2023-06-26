namespace nbtsharp;

using System;
using System.Text;
public class NBTString: INBTElement{
    public NBTString(string name, string value){
        _name = name;
        _value = value;
    }
    public NBTString(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.String))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.String + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        int index = Array.IndexOf<byte>(serializedData[5..size], 0)+5;
        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..index]);
        _value = ASCIIEncoding.ASCII.GetString(serializedData[index..size]);
    }
    public ENBTType Type => ENBTType.String;

    public string Name{get => _name;}
    public object Contained{get => _value;}
    public string ContainedString{get => _value;}

    public byte[] Serialize(){
        byte[] valueBytes = ASCIIEncoding.ASCII.GetBytes(_value);
        return INBTElement.AddHeader(this, valueBytes);
    }

    public override string ToString(){
        return INBTElement.GetNBTString(this);
    } 
    private string _value;
    private string _name;
}