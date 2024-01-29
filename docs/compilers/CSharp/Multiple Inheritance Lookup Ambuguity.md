Ambiguity in Interface Multiple-Inheritance Lookup
==================================================

Both the (old) native and Roslyn compilers do not obey the C# language specification in handling ambiguity in name lookup when faced with multiple inheritance in an interface. Both compilers accept the following code (with a warning), even though the C# language specification section 7.4 (last bullet) requires an error.

```cs
delegate void D();
interface I1
{
    void M();
}

interface I2
{
    event D M;
}

interface I3 : I1, I2 { }
public class P : I3
{
    event D I2.M
    {
        add { }
        remove { }
    }

    void I1.M()
    {
    }
}

class Q : P
{
    static int Main(string[] args)
    {
        Q p = new Q();
        I3 m = p;
        m.M(); // C# spec 7.4 (last bullet before 7.4.1) requires binding-time error. Compiler gives only warning.
        return 0;
    }
}
```

The rule implemented in the compilers is: when there are methods and non-methods found along different inheritance paths, the compilers emit a warning and drop the non-methods.
