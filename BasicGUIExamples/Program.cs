using System.Reflection;
public sealed class Program
{
    public static void Main()
    {
        //This is quite the line of code xD
        
        MethodInfo[] methods = typeof(ExampleFunctions).GetMethods().Where((method) => {return method.IsStatic;}).ToArray();
        System.Console.WriteLine("Please choose an example to run. Options:");
        for(int i=0; i<methods.Length; i++)
        {
            System.Console.WriteLine("\t" + i + ": " + methods[i].Name);
        }
        bool error = false;
        int index = -1;
        do
        {
            string? text = "2";///System.Console.ReadLine();
            if(text is null){System.Console.WriteLine("null line!");error=true;continue;}
            error = !int.TryParse(text, out index);
            if(error){System.Console.WriteLine("Not a number apparently");error=true;continue;}
            if(index < 0){System.Console.WriteLine("You gonna gimme a positive index");error=true;continue;}
            if(index >methods.Length-1){System.Console.WriteLine("Out of bounds");error=true;continue;}
        }while(error);
        methods[index].Invoke(null, null);
    }
}