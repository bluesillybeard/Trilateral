namespace Trilateral.Utility;

public static class StringVerifiers
{
    public static readonly string[] invalidPathStrings = new[]{
        ".", //This is to avoid people from putting their world outside the saves folder
        "/", //slashes are generally not good
        "\\", //back slashes too
        "\n", //newlines are probably fine but I don't want to risk it.
        "\0", //If someone somehow manages to get a null character in a path, they deserve applause. And failure.
        "$", //I hear dollar signs can cause issues
        "%", //percents are also not a good idea
    };

    //TODO: find a better way than relying on the index
    // Not that important, they all just become underscores for now.
    public static readonly string[] invalidPathStringConversions = new[]{
        "_",
        "_",
        "_",
        "_",
        "_",
        "_",
        "_",
    };
    public static string ConvertPathToSecure(string path)
    {
        for(int i=0; i<invalidPathStrings.Length; i++)
        {
            path = path.Replace(invalidPathStrings[i], invalidPathStringConversions[i]);
        }
        return path;
    }
}