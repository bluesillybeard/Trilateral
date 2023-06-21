namespace nbtsharp;

/**
<summary>
This class represents an NBTElement, except the HashCode and Equals methods are based on the NAME of the contained element, not the name and contained object.
This allows one to store a set of NBTElementFinders and actually be able to fetch a certain element without already having the element.
The hashCode function will return the hash code as if the NBTElementFinder were a String containing the name of the elementm 
and the Equals method can be directly applied to a String containing a name
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
        return element.Name;
    }

    public override bool Equals(object obj)
    {
        if(obj is NBTElementFinder other)
        {
            return other.element.Name.Equals(element.Name);
        }
        return false;
    }

    public class NullNBTElement: INBTElement{
        //a Crappy Little Class for holding an invalid element. When serialized, it appears as nothing.
        public NullNBTElement(string name){

        }
        public ENBTType Type{get => (ENBTType)(-1);}

        public string Name{get => _name;set => _name=value;}
        public object Contained{get => null;}

        public byte[] Serialize(){
            return new byte[0];
        }
        private string _name;
    }
}