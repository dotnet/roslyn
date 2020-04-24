# Module Initializers
## Overview

A module initializer is a method that runs eagerly and one-time when an assembly is loaded, similar to a type initializer (static constructor).

A module initializer in .NET is defined by adding a `.cctor` method to the `<Module>` type. Since the `<Module>` type is not explicitly defined in user code, the language has to provide a feature in order to make it possible for users to write a module initializer.

__See__: the [corresponding proposal](https://github.com/dotnet/csharplang/blob/master/proposals/module-initializers.md) in CSharpLang.

## Defining a module initializer method

A method can be designated as module initializer by decorating it with a `[ModuleInitializer]` attribute.

```cs
using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModuleInitializerAttribute : Attribute { }
}
```

The attribute can be used like this:

```cs
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    internal static void M1()
    {
        // ...
    }
}
```

Some requirements are imposed on the method targeted with this attribute:
1. The method must be `static`.
1. The method must be parameterless.
1. The method must return `void`.
1. The method must be accessible from the `<Module>` class.
    - This means the method's effective accessibility must be `internal` or `public`.
    - This also means the method cannot be a local function.

## Translation strategy

When the `[ModuleInitializer]` attribute is used in the compilation, the compiler will synthesize a static constructor on the `<Module>` type. The constructor will call all the module initializer methods in user code in source order, first by file order and then by line number, similar to how static field initializers on partial classes are ordered.

For example, if `file1.cs` and `file2.cs` are provided to the compiler in the following order:

```cs
// file1.cs
public class C1
{
    [ModuleInitializer]
    public static void M1()
    {
        // ...
    }

    [ModuleInitializer]
    public static void M2()
    {
        // ...
    }
}

// file2.cs
internal class C2
{
    [ModuleInitializer]
    internal static void M3()
    {
        // ...
    }

    internal class C3
    {
        [ModuleInitializer]
        internal static void M4()
        {
            // ...
        }
    }
}
```

IL equivalent to the following would be emitted:

```il
.class private auto ansi '<Module>'
{
    .method private hidebysig specialname rtspecialname static 
        void .cctor () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 21 (0x15)
        .maxstack 8

        IL_0000: call void C1::M1()
        IL_0005: call void C1::M2()
        IL_000a: call void C2::M3()
        IL_000a: call void C2/C3::M4()
        IL_000f: ret
    } // end of method C::.cctor
} // end of class <Module>
```
