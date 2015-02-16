Definite Assignment For Partial And Conditional Methods
=======================================================

The C# language specification for definite assignment of an invocation expression [5.3.2.2] assumes incorrectly that the invocation expression and its arguments will be evaluated at runtime if it is reached. That may not be correct for *partial methods* [10.2.7] and methods marked with a *conditional attribute* [17.4.2].

The C# compiler correctly takes into account that the method invocation (and argument evaluation) may be omitted.

```cs
using System.Diagnostics;

 static partial class A
{
    static void Main()
    {
        int x;
        x.Foo(); // OK
        Bar(x); // OK

        Bar(x = 1);
        x.ToString(); // error CS0165: Use of unassigned local variable 'x'
    }

    static partial void Foo(this int y);

    [Conditional("xxxxxx")]
    static void Bar(int y) { }

}
```