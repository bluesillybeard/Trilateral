namespace nbtsharp;

/**
<summary>
This class represents an NBTElement, except the HashCode and Equals methods are based on the NAME of the contained element, not the name and contained object.
This means that you can change the name of an element, and since this class stores a reference to the original element rather than a separate string,
the name to search also changes.
</summary>
*/
public class NBTElementFinder{
    public INBTElement element;

    public NBTElementFinder(INBTElement element){
        this.element = element;
    }

    public NBTElementFinder(string name){
        element = new NullNBTElement(name);
    }
    public override int GetHashCode()
    {
        return element.Name.GetHashCode();
    }

    public override string ToString()
    {
        return element.ToString();
    }

    public override bool Equals(object obj)
    {
        return element.Equals(obj);
    }

    public class NullNBTElement: INBTElement{
        public NullNBTElement(string name){

        }
        public ENBTType Type{get => (ENBTType)(-1);}

        public string Name{get => _name;set => _name=value;}
        public object Contained{get => null;}

        public byte[] Serialize(){
            return null;
        }
        private string _name;
    }
}