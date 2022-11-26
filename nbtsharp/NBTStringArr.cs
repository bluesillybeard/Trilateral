namespace nbtsharp;
using System;
using System.Text;
using System.IO;
public class NBTStringArr: INBTElement{
    public NBTStringArr(string name, string[] value){
        _name = name;
        _value = value;
    }

    public NBTStringArr(byte[] serializedData){
        if(serializedData[4] != ((byte)ENBTType.StringArr))
            throw new NotSupportedException("Cannot use data for type " + INBTElement.NBTNameType((ENBTType)serializedData[4]) + " to create type " + INBTElement.NBTNameType(ENBTType.StringArr) + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        int index = Array.IndexOf<byte>(serializedData[5..size], 0)+5;
        int length = BitConverter.ToInt32(serializedData, index+1);
        _name = ASCIIEncoding.ASCII.GetString(serializedData, 5, index-5);
        _value = new string[length];
        index += 5;
        for(int i=0; i<length; i++){
            int end = Array.IndexOf<byte>(serializedData, 0, index);
            _value[i] = ASCIIEncoding.ASCII.GetString(serializedData, index, end-index);
            index = end+1;
        }
    }

    public string Name{get => _name;set => _name=value;}
    public object Contained{get => _value;}

    public string[] ContainedArray => _value;
    public ENBTType Type => ENBTType.StringArr;

    public byte[] Serialize(){
        int length = 0;
        foreach(string val in _value){
            length += val.Length+1;
        }
        byte[] valueBytes = new byte[4 + length];
        valueBytes[0] = (byte)(_value.Length);
        valueBytes[1] = (byte)(_value.Length>>8);
        valueBytes[2] = (byte)(_value.Length>>16);
        valueBytes[3] = (byte)(_value.Length>>24);
        int index = 4;
        foreach(string val in _value){
            foreach(char c in val){
                valueBytes[index++] = ((byte)c);
            }
            valueBytes[index++] = 0;
        }
        return INBTElement.AddHeader(this, valueBytes);
    }
    

    public override string ToString(){
        StringBuilder b = new StringBuilder();
        b.Append(_name.ToString()).Append(": {");
        foreach (string element in _value){
            b.Append("\"").Append(element).Append("\", ");
        }
        b.Append("}");
        return b.ToString();
    } 
    private string[] _value;
    private string _name;
}