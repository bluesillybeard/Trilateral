namespace nbtsharp;
using System;
using System.Text;
using System.IO;
public class NBTIntArr: INBTElement{
    public NBTIntArr(string name, int[] value){
        _name = name;
        _value = value;
    }

    public NBTIntArr(byte[] serializedData){
        if(serializedData[4] != ((byte)ENBTType.IntArr))
            throw new NotSupportedException("Cannot use data for type " + INBTElement.NBTNameType((ENBTType)serializedData[4]) + " to create type " + INBTElement.NBTNameType(ENBTType.IntArr) + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        //check indices
        int index = Array.IndexOf<byte>(serializedData[5..size], 0)+5;
        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..index]);
        _value = new int[(size-index)/4];
        for(int i=0; i<_value.Length; i++){
            _value[i] = BitConverter.ToInt32(serializedData, index+i*4+1);
        }
    }

    public string Name{get => _name;set => _name=value;}
    public object Contained{get => _value;}

    public int[] ContainedArray => _value;
    public ENBTType Type => ENBTType.IntArr;

    public byte[] Serialize(){
        byte[] valueBytes = new byte[_value.Length*sizeof(int)];
        for(int i=0; i<_value.Length; i++){
            valueBytes[4*i+0] = (byte)(_value[i]);
            valueBytes[4*i+1] = (byte)(_value[i]>>8);
            valueBytes[4*i+2] = (byte)(_value[i]>>16);
            valueBytes[4*i+3] = (byte)(_value[i]>>24);
        }
        return INBTElement.AddHeader(this, valueBytes);
    }
    

    public override string ToString(){
        StringBuilder b = new StringBuilder();
        b.Append(_name.ToString()).Append(": {");
        foreach (int element in _value){
            b.Append(element).Append(", ");
        }
        b.Append("}");
        return b.ToString();    } 
    private int[] _value;
    private string _name;
}