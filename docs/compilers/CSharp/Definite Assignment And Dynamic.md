Definite Assignment and Short-Circuit Operators in Dynamic Expressions
======================================================================

The definite assignment rules implemented by previous compilers for dynamic expressions allowed some cases of code that could result in variables being read that are not definitely assigned. See https://github.com/dotnet/roslyn/issues/4509 for one report of this.

The following (twisted) program demonstrates that "fixing" this issue would introduce a situation in which the compiler would allow reading an uninitialized variable.

```cs
using System;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string val = "unassigned";
            if (Api1() && Api2(out val) && UseVal(val))
            {
                Console.WriteLine(val);
            }
        }

        static dynamic Api1()
        {
            return new Waffle();
        }

        static bool Api2(out string val)
        {
            Console.WriteLine("Assigning to val");
            val = "assigned";
            return true;
        }

        static dynamic UseVal(string val)
        {
            Console.WriteLine($"Using val == {val}");
            return true;
        }
    }

    class Waffle
    {
        int count = 0;
        public static implicit operator bool(Waffle w)
        {
            try
            {
                return w.count != 0;
            }
            finally
            {
                w.count++;
            }
        }
    }
}
```

The output of this program is

```none
Using val == unassigned
unassigned
```

Because of this possibility the compiler must not allow this program to be compiled if `val` has no initial value. Previous versions of the compiler (prior to VS2015) allowed this program to compile even if val has no initial value. Roslyn now diagnoses this attempt to read a possibly uninitialized variable.
