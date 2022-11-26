namespace BasicGUI.Core;

//Represents an indeterminant position.
public struct NodeBounds
{
    public int? X, Y, W, H;
    public NodeBounds(int? x, int? y, int? w, int? h){
        this.X = x;
        this.Y = y;
        this.W = w;
        this.H = h;
    }

    public bool ContainsPoint(int xp, int yp)
    {
        int x0 = X ?? 0;
        int y0 = Y ?? 0;
        int xf = X+W ?? 0;
        int yf = Y+H ?? 0;
        return xp > x0 && xp < xf && yp > y0 && yp < yf;
    }
}

public static class KeyConverter
{

    public static char? KeyDown(KeyCode keyIn, bool capsLock, bool shift, bool numLock)
    {
        //one or the other makes caps, 
        // both or non makes lowercase.
        bool capitalize = capsLock ^ shift;
        //C# switch statements are truly awful.
        // Get your switch game on Microsoft!
        // I thought C# was supposed to be better than Java!
        switch(keyIn)
        {
            case KeyCode.backspace:
                return '\b';
            case KeyCode.tab:
                return '\t';
            case KeyCode.enter:
                return '\n';
            case KeyCode.space:
                return ' ';
            case KeyCode.zero:
                return shift ? ')' : '0';
            case KeyCode.one:
                return shift ? '!' : '1';
            case KeyCode.two:
                return shift ? '@' : '2';
            case KeyCode.three:
                return shift ? '#' : '3';
            case KeyCode.four:
                return shift ? '$' : '4';
            case KeyCode.five:
                return shift ? '%' : '5';
            case KeyCode.six:
                return shift ? '^' : '6';
            case KeyCode.seven:
                return shift ? '&' : '7';
            case KeyCode.eight:
                return shift ? '*' : '8';
            case KeyCode.nine:
                return shift ? '(' : '9';
            case KeyCode.a:
                return capitalize ? 'A' : 'a';
            case KeyCode.b:
                return capitalize ? 'B' : 'b';
            case KeyCode.c:
                return capitalize ? 'C' : 'c';
            case KeyCode.d:
                return capitalize ? 'D' : 'd';
            case KeyCode.e:
                return capitalize ? 'E' : 'e';
            case KeyCode.f:
                return capitalize ? 'F' : 'f';
            case KeyCode.g:
                return capitalize ? 'G' : 'g';
            case KeyCode.h:
                return capitalize ? 'H' : 'h';
            case KeyCode.i:
                return capitalize ? 'I' : 'i';
            case KeyCode.j:
                return capitalize ? 'J' : 'j';
            case KeyCode.k:
                return capitalize ? 'K' : 'k';
            case KeyCode.l:
                return capitalize ? 'L' : 'l';
            case KeyCode.m:
                return capitalize ? 'M' : 'm';
            case KeyCode.n:
                return capitalize ? 'N' : 'n';
            case KeyCode.o:
                return capitalize ? 'O' : 'o';
            case KeyCode.p:
                return capitalize ? 'P' : 'p';
            case KeyCode.q:
                return capitalize ? 'Q' : 'q';
            case KeyCode.r:
                return capitalize ? 'R' : 'r';
            case KeyCode.s:
                return capitalize ? 'S' : 's';
            case KeyCode.t:
                return capitalize ? 'T' : 't';
            case KeyCode.u:
                return capitalize ? 'U' : 'u';
            case KeyCode.v:
                return capitalize ? 'V' : 'v';
            case KeyCode.w:
                return capitalize ? 'W' : 'w';
            case KeyCode.x:
                return capitalize ? 'X' : 'x';
            case KeyCode.y:
                return capitalize ? 'Y' : 'y';
            case KeyCode.z:
                return capitalize ? 'Z' : 'z';
            case KeyCode.num0:
                return numLock ? '0': null;
            case KeyCode.num1:
                return numLock ? '1': null;
            case KeyCode.num2:
                return numLock ? '2': null;
            case KeyCode.num3:
                return numLock ? '3': null;
            case KeyCode.num4:
                return numLock ? '4': null;
            case KeyCode.num5:
                return numLock ? '5': null;
            case KeyCode.num6:
                return numLock ? '6': null;
            case KeyCode.num7:
                return numLock ? '7': null;
            case KeyCode.num8:
                return numLock ? '8': null;
            case KeyCode.num9:
                return numLock ? '9': null;
            case KeyCode.multiply:
                return numLock ? '*': null;
            case KeyCode.add:
                return numLock ? '+': null;
            case KeyCode.subtract:
                return numLock ? '-': null;
            case KeyCode.decimalPoint:
                return numLock ? '.': null;
            case KeyCode.divide:
                return numLock ? '/': null;
            case KeyCode.numLock:
                numLock = !numLock; //numlock is a toggle
                return null;
            case KeyCode.semicolon:
                return shift ? ':' : ';';
            case KeyCode.equals:
                return shift ? '+' : '=';
            case KeyCode.comma:
                return shift ? '<' : ',';
            case KeyCode.dash:
                return shift ? '_' : '-';
            case KeyCode.period:
                return shift ? '>' : '.';
            case KeyCode.slash:
                return shift ? '?' : '/';
            case KeyCode.grave:
                return shift ? '~' : '`';
            case KeyCode.bracketLeft:
                return shift ? '{' : '[';
            case KeyCode.backSlash:
                return shift ? '|' : '\\';
            case KeyCode.bracketRight:
                return shift ? '}' : ']';
            case KeyCode.quote:
                return shift ? '\"' : '\'';
            //This means its intentionally left out and should do nothing
            default:
                return null;
        }
    }
}