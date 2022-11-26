namespace nbtsharp;

using System;
using System.Text;
public class NBTUInt: INBTElement{
    
    public NBTUInt(string name, uint value){
        _name = name;
        _value = value;
    }
    public NBTUInt(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.UInt))
            throw new NotSupportedException("Cannot use data for type " + INBTElement.NBTNameType((ENBTType)serializedData[4]) + " to create type " + INBTElement.NBTNameType(ENBTType.UInt) + ".");
        int size = BitConverter.ToInt32(serializedData, 0);

        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..(size-5)]);
        _value = BitConverter.ToUInt32(serializedData, size-4);
    }
    public ENBTType Type => ENBTType.UInt;

    public string Name{get => _name;set => _name=value;}
    public object Contained{get => _value;}

    public byte[] Serialize(){
        return INBTElement.AddHeader(this, (int)_value);
    }
    public override string ToString(){
        return INBTElement.GetNBTString(this);
    }
    private uint _value;
    private string _name;
}