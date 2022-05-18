namespace nbtsharp;
using System;
using System.Text;
using System.Collections.Generic;
public class NBTFolder: INBTElement{
    public NBTFolder(string name, List<INBTElement> value){
        _name = name;
        _value = value;
    }
    public NBTFolder(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.Folder))
            throw new NotSupportedException("Cannot use data for type " + INBTElement.NBTNameType((ENBTType)serializedData[4]) + " to create type " + INBTElement.NBTNameType(ENBTType.Folder) + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        //check indices
        int index = Array.IndexOf<byte>(serializedData[5..size], 0);
        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..(index+5)]);
        _value = new List<INBTElement>();
        index = 6+_name.Length; //index represents what byte we are on. Skip the 
        while(index < size) {
            //
            int elementSize = BitConverter.ToInt32(serializedData, index);
            ENBTType elementType = (ENBTType)serializedData[index+4];
            byte[] elementData = serializedData[index..(index+elementSize)];
            INBTElement el;
            switch(elementType){
                case ENBTType.Int: {el=new NBTInt(elementData); break;}
                //case ENBTType.IntArr: {el=new NBTIntArr(elementData); break;}
                case ENBTType.UInt: {el=new NBTUInt(elementData); break;}
                //case ENBTType.UIntArr: {el=new NBTUIntArr(elementData); break;}
                case ENBTType.Float: {el=new NBTFloat(elementData); break;}
                //case ENBTType.FloatArr: {el=new NBTFloatArr(elementData); break;}
                case ENBTType.String: {el=new NBTString(elementData); break;}
                //case ENBTType.StringArr: {el=new NBTStringArr(elementData); break;}
                case ENBTType.Folder: {el=new NBTFolder(elementData); break;}
                default: throw new NotSupportedException("found invalid NBT type " + elementType + " at offset " + index);
            }
            _value.Add(el);
            index+=elementSize;
        }
    }
    public ENBTType Type => ENBTType.Folder;

    public string Name{get => _name;set => _name=value;}
    public object Contained{get => _value;}

    public byte[] Serialize(){
        List<byte> data = new List<byte>(_value.Count * (9+5));
        foreach(INBTElement element in _value){
            data.AddRange(element.Serialize());
        }
        return INBTElement.AddHeader(this, data.ToArray());
    }
    public override string ToString(){
        StringBuilder b = new StringBuilder();
        b.Append(_name.ToString()).Append(": {");
        foreach (INBTElement element in _value){
            b.Append(element.ToString()).Append(", ");
        }
        b.Append("}");
        return b.ToString();
    }
    private List<INBTElement> _value;
    private string _name;
}