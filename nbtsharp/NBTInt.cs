namespace nbtsharp;

using System;
using System.Text;
public class NBTInt: INBTElement{
    public NBTInt(string name, int value){
        _name = name;
        _value = value;
    }
    public NBTInt(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.Int))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.Int + ".");
        int size = BitConverter.ToInt32(serializedData, 0);

        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..(size-5)]);
        _value = BitConverter.ToInt32(serializedData, size-4);
    }
    public ENBTType Type => ENBTType.Int;

    public string Name{get => _name;}
    public object Contained{get => _value;}

    public byte[] Serialize(){
        return INBTElement.AddHeader(this, _value);
    }
    public override string ToString(){
        return INBTElement.GetNBTString(this);
    }
    private int _value;
    private string _name;
}