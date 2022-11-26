namespace nbtsharp;
public enum ENBTType{
    Invalid = -1, //an invalid NBT element. Shouldn't occur in nature.
    Int=0, IntArr=4,
    UInt=1, UIntArr=5,
    Float=2, FloatArr=6,
    String=3, StringArr=7,
    Folder=8,
}