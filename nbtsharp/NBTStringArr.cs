namespace nbtsharp;
using System;
using System.Text;
using System.IO;
public class NBTStringArr: INBTElement{
    public NBTStringArr(string name, string[] value){
        Name = name;
        ContainedArray = value;
    }

    public NBTStringArr(byte[] serializedData){
        if(serializedData[4] != ((byte)ENBTType.StringArr))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.StringArr + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        int index = Array.IndexOf<byte>(serializedData[5..size], 0)+5;
        int length = BitConverter.ToInt32(serializedData, index+1);
        Name = ASCIIEncoding.ASCII.GetString(serializedData, 5, index-5);
        ContainedArray = new string[length];
        index += 5;
        for(int i=0; i<length; i++){
            int end = Array.IndexOf<byte>(serializedData, 0, index);
            ContainedArray[i] = ASCIIEncoding.ASCII.GetString(serializedData, index, end-index);
            index = end+1;
        }
    }

    public string Name { get; }
    public object Contained{get => ContainedArray; }

    public string[] ContainedArray { get; }
    public ENBTType Type => ENBTType.StringArr;

    public byte[] Serialize(){
        int length = 0;
        foreach(string val in ContainedArray)
        {
            length += val.Length+1;
        }
        byte[] valueBytes = new byte[4 + length];
        valueBytes[0] = (byte)(ContainedArray.Length);
        valueBytes[1] = (byte)(ContainedArray.Length>>8);
        valueBytes[2] = (byte)(ContainedArray.Length>>16);
        valueBytes[3] = (byte)(ContainedArray.Length>>24);
        int index = 4;
        foreach(string val in ContainedArray)
        {
            foreach(char c in val){
                valueBytes[index++] = ((byte)c);
            }
            valueBytes[index++] = 0;
        }
        return INBTElement.AddHeader(this, valueBytes);
    }

    public override string ToString(){
        StringBuilder b = new();
        b.Append(Name).Append(": {");
        foreach (string element in ContainedArray)
        {
            b.Append('\"').Append(element).Append("\", ");
        }
        b.Append('}');
        return b.ToString();
    }
}