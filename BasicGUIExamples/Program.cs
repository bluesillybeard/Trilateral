public sealed class Program
{
    private static readonly string[] exampleNames = {"basic"};
    private static readonly Action[] exampleFunctions = {Basic};
    public static void Main()
    {
        System.Console.WriteLine("Please choose an example to run. Options:");
        for(int i=0; i<exampleNames.Length; i++)
        {
            System.Console.WriteLine("\t" + i + ": " + exampleNames[i]);
        }
        bool error = false;
        int index = -1;
        do
        {
            string? text = System.Console.ReadLine();
            if(text is null){System.Console.WriteLine("null line!");error=true;continue;}
            error = !int.TryParse(text, out index);
            if(error){System.Console.WriteLine("Not a number apparently");error=true;continue;}
            if(index < 0){System.Console.WriteLine("You gonna gimme a positive index");error=true;continue;}
            if(index >exampleNames.Length-1){System.Console.WriteLine("Out of bounds");error=true;continue;}
        }while(error);
        exampleFunctions[index]();
    }

    private static void Basic()
    {
        System.Console.WriteLine("Runninc basic example");
    }
}