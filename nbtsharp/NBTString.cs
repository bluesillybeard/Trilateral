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
            throw new NotSupportedException("Cannot use data for type " + INBTElement.NBTNameType((ENBTType)serializedData[4]) + " to create type " + INBTElement.NBTNameType(ENBTType.String) + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        //check indices
        int index = Array.IndexOf<byte>(serializedData[5..size], 0);
        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..(index+5)]);
        _value = ASCIIEncoding.ASCII.GetString(serializedData[(index+5)..size]);
    }
    public ENBTType Type => ENBTType.String;

    public string Name{get => _name;set => _name=value;}
    public object Contained{get => _value;}

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