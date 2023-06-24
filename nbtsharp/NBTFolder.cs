namespace nbtsharp;
using System;
using System.Text;
using System.Collections.Generic;
public class NBTFolder: INBTElement{
    public NBTFolder(string name, Dictionary<string, INBTElement> value){
        _name = name;
        _value = value;
    }
    public NBTFolder(string name, INBTElement[] value){
        _name = name;
        _value = new Dictionary<string, INBTElement>(value.Length);
        foreach(INBTElement element in value){
            _value.Add(element.Name, element);
        }
    }
    public NBTFolder(byte[] serializedData) {
        if(serializedData[4] != ((byte)ENBTType.Folder))
            throw new NotSupportedException("Cannot use data for type " + (ENBTType)serializedData[4] + " to create type " + ENBTType.Folder + ".");
        int size = BitConverter.ToInt32(serializedData, 0);
        //check indices
        int index = Array.IndexOf<byte>(serializedData[5..size], 0);
        _name = ASCIIEncoding.ASCII.GetString(serializedData[5..(index+5)]);
        _value = new Dictionary<string, INBTElement>();
        index = 6+_name.Length; //index represents what byte we are on.
        while(index < size) {
            int elementSize = BitConverter.ToInt32(serializedData, index);
            ENBTType elementType = (ENBTType)serializedData[index+4];
            byte[] elementData = serializedData[index..(index+elementSize)];
            INBTElement el;
            switch(elementType){
                case ENBTType.Int: {el=new NBTInt(elementData); break;}
                case ENBTType.IntArr: {el=new NBTIntArr(elementData); break;}
                case ENBTType.UInt: {el=new NBTUInt(elementData); break;}
                case ENBTType.UIntArr: {el=new NBTUIntArr(elementData); break;}
                case ENBTType.Float: {el=new NBTFloat(elementData); break;}
                case ENBTType.FloatArr: {el=new NBTFloatArr(elementData); break;}
                case ENBTType.String: {el=new NBTString(elementData); break;}
                case ENBTType.StringArr: {el=new NBTStringArr(elementData); break;}
                case ENBTType.Folder: {el=new NBTFolder(elementData); break;}
                default: throw new NotSupportedException("found invalid NBT type " + elementType + " at offset " + index);
            }
            _value.Add(el.Name, el);
            index+=elementSize;
        }
    }
    public ENBTType Type => ENBTType.Folder;

    public string Name{get => _name;}
    public object Contained{get => _value;}

    public NBTFolder Add(INBTElement element){
        _value.Add(element.Name, element);
        return this;
    }

    public INBTElement Get(string name)
    {
        return _value[name];
    }

    public T Get<T>(string name)
    where T : INBTElement
    {
        return (T)_value[name];
    }

    public bool TryGet(string name, out INBTElement element)
    {
        return _value.TryGetValue(name, out element);
    }

    public bool TryGet<T>(string name, out T element)
    where T : INBTElement
    {
        bool got = TryGet(name, out var uncastElement);
        if(got && uncastElement is T castElement)
        {
            element = castElement;
            return true;
        }
        element = default(T);
        return false;
    }
    public INBTElement GetOrDefault(string name, INBTElement def)
    {
        if(_value.ContainsKey(name))
        {
            return _value[name];
        }
        return def;
    }

    public T GetOrDefault<T>(string name, T def)
    where T : INBTElement
    {
        if(_value.ContainsKey(name))
        {
            var val = _value[name];
            if(val is T element)
            {
                return element;
            }
        }
        return def;
    }

    
    public bool Remove(string name, out INBTElement value)
    {
        return _value.Remove(name, out value);
    }

    public bool Remove(string name)
    {
        return _value.Remove(name);
    }
    public byte[] Serialize()
    {
        List<byte> data = new List<byte>(_value.Count * (9+5));
        foreach(KeyValuePair<string, INBTElement> element in _value){
            data.AddRange(element.Value.Serialize());
        }
        return INBTElement.AddHeader(this, data.ToArray());
    }
    public override string ToString()
    {
        StringBuilder b = new StringBuilder();
        b.Append(_name.ToString()).Append(": {");
        foreach (KeyValuePair<string, INBTElement> element in _value){
            b.Append(element.Value.ToString()).Append(", ");
        }
        b.Append("}");
        return b.ToString();
    }
    private Dictionary<string, INBTElement> _value;
    private string _name;
}