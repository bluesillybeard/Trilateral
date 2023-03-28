using System.IO;
using System.Text;
using System;

//TODO: Refactor to use a discriminated union (if those even ever become a thing in C#)

namespace nbtsharp;
public interface INBTElement
{
    ENBTType Type{get;}

    string Name{get;set;}
    object Contained{get;}

    byte[] Serialize();

    static string NBTNameType(ENBTType type){
        string o;
        switch (type)
        {
            case ENBTType.Int: {o="Integer";break;}
            case ENBTType.UInt: {o="UnsignedInteger";break;}
            case ENBTType.Float: {o="Float";break;}
            case ENBTType.String: {o="String";break;}
            case ENBTType.IntArr: {o="IntegerArray";break;}
            case ENBTType.UIntArr: {o="UnsignedIntegerArray";break;}
            case ENBTType.FloatArr: {o="FloatArray";break;}
            case ENBTType.StringArr: {o="StringArray";break;}
            case ENBTType.Folder: {o="Folder";break;}
            default: { o="Unknown";break;}
        }
        return o;
    }

    public static byte[] AddHeader(INBTElement element, byte[] valueBytes){
        int size = 4 + 1 + element.Name.Length+1 + valueBytes.Length; //4 (length) + 1(type) + name+1 (name + null) + NBT data
        byte[] outs = new byte[size];
        //check to make sure that this won't also dispose of the backing array.
        MemoryStream stream = new MemoryStream(outs);
        using(BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, false)){
            writer.Write(size);
            writer.Write((byte)element.Type);
            writer.Write(ASCIIEncoding.ASCII.GetBytes(element.Name)); //ngl I prefer the Java version of this being in the String class, not it's own separate class somewhat hidden away
            writer.Write((byte)0); //the null to signal the end of the name.
            writer.Write(valueBytes); //write the actual NBT element data. The reader will know we've hit the end because of the length parameter.
        }
        return outs;
    }
        public static byte[] AddHeader(INBTElement element, int quadBytes){
        int size = 4 + 1 + element.Name.Length+1 + 4; //4 (length) + 1(type) + name+1 (name + null) + 4 (this method only applies to 32-bit types)
        byte[] outs = new byte[size];
        //check to make sure that this won't also dispose of the backing array.
        MemoryStream stream = new MemoryStream(outs);
        using(BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, false)){
            writer.Write(size);
            writer.Write((byte)element.Type);
            writer.Write(ASCIIEncoding.ASCII.GetBytes(element.Name)); //ngl I prefer the Java version of this being in the String class, not it's own separate class somewhat hidden away
            writer.Write((byte)0); //the null to signal the end of the name.
            writer.Write(quadBytes); //write the actual NBT element data. The reader will know we've hit the end because of the length parameter.
        }
        return outs;
    }
    static string GetNBTString(INBTElement e){
        return e.Name + ": " + e.Contained;
    }
}
