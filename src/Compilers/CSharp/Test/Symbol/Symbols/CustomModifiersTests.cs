// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class CustomModifiersTests : CSharpTestBase
    {
        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ModifiedTypeArgument_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test1
       extends[mscorlib] System.Object
        {
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
        {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call instance void[mscorlib]
        System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
    } // end of method Test1::.ctor

  .method public hidebysig static void Test(valuetype[mscorlib] System.Nullable`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> x) cil managed
    {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""Test""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
    } // end of method Test1::Test

} // end of class Test1
";

            var source = @"
class Module1
{
     static void Main()
    {
        Test1.Test(null);
    }
}
";
            var compilation = (Compilation)CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var test = compilation.GetTypeByMetadataName("Test1").GetMember<IMethodSymbol>("Test");
            var type = (INamedTypeSymbol)test.Parameters.First().Type;
            Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsLong)?", type.ToTestDisplayString());
            Assert.Equal("System.Runtime.CompilerServices.IsLong", type.GetTypeArgumentCustomModifiers(0).Single().Modifier.ToTestDisplayString());
            Assert.Throws<System.IndexOutOfRangeException>(() => type.GetTypeArgumentCustomModifiers(1));
            Assert.Throws<System.IndexOutOfRangeException>(() => type.GetTypeArgumentCustomModifiers(-1));

            var nullable = type.OriginalDefinition;
            Assert.Equal("System.Nullable<T>", nullable.ToTestDisplayString());
            Assert.True(nullable.GetTypeArgumentCustomModifiers(0).IsEmpty);
            Assert.Throws<System.IndexOutOfRangeException>(() => nullable.GetTypeArgumentCustomModifiers(1));
            Assert.Throws<System.IndexOutOfRangeException>(() => nullable.GetTypeArgumentCustomModifiers(-1));

            var i = (INamedTypeSymbol)type.TypeArguments.First();
            Assert.Equal("System.Int32", i.ToTestDisplayString());
            Assert.Throws<System.IndexOutOfRangeException>(() => i.GetTypeArgumentCustomModifiers(0));

            nullable = nullable.Construct(i);
            Assert.Equal("System.Int32?", nullable.ToTestDisplayString());
            Assert.True(nullable.GetTypeArgumentCustomModifiers(0).IsEmpty);
            Assert.Throws<System.IndexOutOfRangeException>(() => nullable.GetTypeArgumentCustomModifiers(1));
            Assert.Throws<System.IndexOutOfRangeException>(() => nullable.GetTypeArgumentCustomModifiers(-1));

            CompileAndVerify(compilation, expectedOutput: "Test");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ModifiedTypeArgument_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test1
       extends[mscorlib] System.Object
        {
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
        {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call instance void[mscorlib]
        System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
    } // end of method Test1::.ctor

  .method public hidebysig static void Test(class [mscorlib] System.Collections.Generic.Dictionary`2<int32, int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsConst)> x) cil managed
    {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""Test""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
    } // end of method Test1::Test

} // end of class Test1
";

            var source = @"
class Module1
{
     static void Main()
    {
        Test1.Test(null);
    }
}
";
            var compilation = (Compilation)CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var test = compilation.GetTypeByMetadataName("Test1").GetMember<IMethodSymbol>("Test");
            var type = (INamedTypeSymbol)test.Parameters.First().Type;
            Assert.Equal("System.Collections.Generic.Dictionary<System.Int32, System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)>",
                         type.ToTestDisplayString());
            Assert.True(type.GetTypeArgumentCustomModifiers(0).IsEmpty);
            var modifiers = type.GetTypeArgumentCustomModifiers(1);
            Assert.Equal(2, modifiers.Length);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", modifiers.First().Modifier.ToTestDisplayString());
            Assert.Equal("System.Runtime.CompilerServices.IsLong", modifiers.Last().Modifier.ToTestDisplayString());
            Assert.Throws<System.IndexOutOfRangeException>(() => type.GetTypeArgumentCustomModifiers(2));
            Assert.Throws<System.IndexOutOfRangeException>(() => type.GetTypeArgumentCustomModifiers(-1));

            var dictionary = type.OriginalDefinition;
            Assert.Equal("System.Collections.Generic.Dictionary<TKey, TValue>", dictionary.ToTestDisplayString());
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(0).IsEmpty);
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(1).IsEmpty);
            Assert.Throws<System.IndexOutOfRangeException>(() => dictionary.GetTypeArgumentCustomModifiers(2));
            Assert.Throws<System.IndexOutOfRangeException>(() => dictionary.GetTypeArgumentCustomModifiers(-1));

            var i = type.TypeArguments.First();
            dictionary = dictionary.Construct(i, i);
            Assert.Equal("System.Collections.Generic.Dictionary<System.Int32, System.Int32>", dictionary.ToTestDisplayString());
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(0).IsEmpty);
            Assert.True(dictionary.GetTypeArgumentCustomModifiers(1).IsEmpty);
            Assert.Throws<System.IndexOutOfRangeException>(() => dictionary.GetTypeArgumentCustomModifiers(2));
            Assert.Throws<System.IndexOutOfRangeException>(() => dictionary.GetTypeArgumentCustomModifiers(-1));

            CompileAndVerify(compilation, expectedOutput: "Test");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiers_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig instance void Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      IL_0000:  ldstr      ""Test""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        new CL2().Test(0);
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: "Test");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiers_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:
        ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        x.Test(0);
    }
}

class CL3
    : CL2
{
    public override void Test(int x)
    {
        System.Console.WriteLine(""Overridden"");
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            var withModifiers = cl3.BaseType().BaseType();
            var withoutModifiers = withModifiers.OriginalDefinition.Construct(withModifiers.TypeArguments());
            Assert.True(HasTypeArgumentsCustomModifiers(withModifiers));
            Assert.False(HasTypeArgumentsCustomModifiers(withoutModifiers));
            Assert.True(withoutModifiers.Equals(withModifiers, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.NotEqual(withoutModifiers, withModifiers);

            CompileAndVerify(compilation, expectedOutput: "Overridden");
        }

        private bool HasTypeArgumentsCustomModifiers(NamedTypeSymbol type)
        {
            return type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Any(a => a.CustomModifiers.Any());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRef_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        int y = 0;
        x.Test(ref y);
    }
}

class CL3
    : CL2
{
    public override void Test(ref int x)
    {
        System.Console.WriteLine(""Overridden"");
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRef_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000: ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        int y = 0;
        x.Test(ref y);
    }
}

class CL3
    : CL2
{
    public override void Test(ref int x)
    {
        System.Console.WriteLine(""Overridden"");
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRef_03()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1& t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000: ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        int y = 0;
        x.Test(ref y);
    }
}

class CL3
    : CL2
{
    public override void Test(ref int x)
    {
        System.Console.WriteLine(""Overridden"");
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref System.Int32 modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRef_04()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:
        ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        int y = 0;
        x.Test(ref y);
    }
}

class CL3
    : CL2
{
    public override void Test(ref int x)
    {
        System.Console.WriteLine(""Overridden"");
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRefReturn_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } 

    .property instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& P()
    {
      .get instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)& CL1`1::get_P()
    } 

} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        x.Test() = 2;
        x.P = 3;
    }
}

class CL3
    : CL2
{
    private int f2;

    public override ref int Test()
    {
        System.Console.WriteLine(""Overridden"");
        return ref f2;
    }

    public override ref int P
    {
        get
        {
            System.Console.WriteLine(""Overridden P"");
            return ref f2;
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            Assert.Equal("ref System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL3.Test()", cl3.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL3.P { get; }", cl3.GetMember<PropertySymbol>("P").ToTestDisplayString());

            var cl1 = compilation.GetTypeByMetadataName("CL1`1");
            Assert.Equal("ref T1 modopt(System.Runtime.CompilerServices.IsConst) CL1<T1>.Test()", cl1.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref T1 modopt(System.Runtime.CompilerServices.IsConst) CL1<T1>.P { get; }", cl1.GetMember<PropertySymbol>("P").ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:
@"Overridden
Overridden P");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRefReturn_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } 

    .property instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) P()
    {
      .get instance !T1& modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1`1::get_P()
    } 
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        x.Test() = 2;
        x.P = 3;
    }
}

class CL3
    : CL2
{
    private int f2;

    public override ref int Test()
    {
        System.Console.WriteLine(""Overridden"");
        return ref f2;
    }

    public override ref int P
    {
        get
        {
            System.Console.WriteLine(""Overridden P"");
            return ref f2;
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsLong) CL3.Test()", cl3.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsLong) CL3.P { get; }", cl3.GetMember<PropertySymbol>("P").ToTestDisplayString());

            var cl1 = compilation.GetTypeByMetadataName("CL1`1");
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) T1 CL1<T1>.Test()", cl1.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) T1 CL1<T1>.P { get; }", cl1.GetMember<PropertySymbol>("P").ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:
@"Overridden
Overridden P");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRefReturn_03()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1& Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1& get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } 

    .property instance !T1& P()
    {
      .get instance !T1& CL1`1::get_P()
    } 
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        x.Test() = 2;
        x.P = 3;
    }
}

class CL3
    : CL2
{
    private int f2;

    public override ref int Test()
    {
        System.Console.WriteLine(""Overridden"");
        return ref f2;
    }

    public override ref int P
    {
        get
        {
            System.Console.WriteLine(""Overridden P"");
            return ref f2;
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            Assert.Equal("ref System.Int32 modopt(System.Runtime.CompilerServices.IsLong) CL3.Test()", cl3.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref System.Int32 modopt(System.Runtime.CompilerServices.IsLong) CL3.P { get; }", cl3.GetMember<PropertySymbol>("P").ToTestDisplayString());

            var cl1 = compilation.GetTypeByMetadataName("CL1`1");
            Assert.Equal("ref T1 CL1<T1>.Test()", cl1.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref T1 CL1<T1>.P { get; }", cl1.GetMember<PropertySymbol>("P").ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:
@"Overridden
Overridden P");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRefReturn_04()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .field private !T1 f1

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Test() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) get_P() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     !0 class CL1`1<!T1>::f1
      IL_0006:  ret
    } 

    .property instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) P()
    {
      .get instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1`1::get_P()
    } 
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        x.Test() = 2;
        x.P = 3;
    }
}

class CL3
    : CL2
{
    private int f2;

    public override ref int Test()
    {
        System.Console.WriteLine(""Overridden"");
        return ref f2;
    }

    public override ref int P
    {
        get
        {
            System.Console.WriteLine(""Overridden P"");
            return ref f2;
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong) CL3.Test()", cl3.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong) CL3.P { get; }", cl3.GetMember<PropertySymbol>("P").ToTestDisplayString());

            var cl1 = compilation.GetTypeByMetadataName("CL1`1");
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) T1 modopt(System.Runtime.CompilerServices.IsVolatile) CL1<T1>.Test()", cl1.GetMember<MethodSymbol>("Test").ToTestDisplayString());
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) T1 modopt(System.Runtime.CompilerServices.IsVolatile) CL1<T1>.P { get; }", cl1.GetMember<PropertySymbol>("P").ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:
@"Overridden
Overridden P");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(8948, "https://github.com/dotnet/roslyn/issues/8948")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRefReturn_05()
        {
            var ilSource = @"
.class interface public abstract auto ansi I1
{
  .method public hidebysig newslot abstract virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst)  M() cil managed
  {
  } // end of method I1::M

  .method public hidebysig newslot specialname abstract virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) get_P() cil managed
  {
  } // end of method I1::get_P

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) P()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) & modopt([mscorlib]System.Runtime.CompilerServices.IsConst) I1::get_P()
  } // end of property I1::P
} // end of class I1
";
            var source = @"
class Module1
{
    static void Main()
    {
        I1 x = new CL2();
        x.M() = 2;
        x.P = 3;

        x = new CL3();
        x.M() = 4;
        x.P = 5;
    }
}

class CL2 : I1
{
    private int f2;

    public ref int M()
    {
        System.Console.WriteLine(""CL2.M"");
        return ref f2;
    }

    public ref int P 
    {
        get
        {
            System.Console.WriteLine(""CL2.P"");
            return ref f2;
        }
    }
}

class CL3 : I1
{
    private int f3;

    ref int I1.M()
    {
        System.Console.WriteLine(""CL3.M"");
        return ref f3;
    }

    ref int I1.P 
    {
        get
        {
            System.Console.WriteLine(""CL3.P"");
            return ref f3;
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) CL3.I1.M()",
                             cl3.GetMember<MethodSymbol>("I1.M").ToTestDisplayString());
            Assert.Equal("ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) CL3.I1.P { get; }",
                             cl3.GetMember<PropertySymbol>("I1.P").ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:
@"CL2.M
CL2.P
CL3.M
CL3.P
");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiers_03()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .property instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
            Test()
    {
      .get instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1`1::get_Test()
      .set instance void CL1`1::set_Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
    } // end of property CL1`1::Test

    .method public hidebysig newslot specialname virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 
            get_Test() cil managed
    {
      // Code size       2 (0x2)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
    } // end of method CL1`1::get_Test

    .method public hidebysig newslot specialname virtual
            instance void  set_Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed
    {
      // Code size       3 (0x3)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
      IL_0002:  ret
    } // end of method CL1`1::set_Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        x.Test = 0;
        var y = x.Test;
    }
}

class CL3
    : CL2
{
    public override int Test
    {
        get
        {
            System.Console.WriteLine(""Get Overridden"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""Set Overridden"");
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<PropertySymbol>("Test");
            Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL3.Test { get; set; }", test.ToTestDisplayString());
            Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL3.Test.get", test.GetMethod.ToTestDisplayString());
            Assert.True(test.GetMethod.ReturnTypeWithAnnotations.CustomModifiers.SequenceEqual(test.SetMethod.Parameters.First().TypeWithAnnotations.CustomModifiers));

            CompileAndVerify(compilation, expectedOutput: @"Set Overridden
Get Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiers_04()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000: ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        CL2 x = new CL3();

        x.Test(null);
    }
}

class CL3
    : CL2
{
    public override void Test(int [] x)
    {
        System.Console.WriteLine(""Overridden"");
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) [] x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiers_05()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .field public static !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Test

    .method private hidebysig specialname rtspecialname static
            void  .cctor() cil managed
    {
      // Code size       18 (0x12)
      .maxstack  1
      IL_0000: ldc.i4.s   123
      IL_0002: box [mscorlib]System.Int32
      IL_0007:  unbox.any  !T1
      IL_000c: stsfld     !0 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) class CL1`1<!T1>::Test
     IL_0011:  ret
    } // end of method CL1`1::.cctor

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
} // end of method CL1`1::.ctor
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

";
            var source = @"
class Module1
{
    static void Main()
    {
        System.Console.WriteLine(CL2.Test);
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl2 = compilation.GetTypeByMetadataName("CL2");
            Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL1<System.Int32 modopt(System.Runtime.CompilerServices.IsLong)>.Test", cl2.BaseType().GetMember("Test").ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "123");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        public void ConstructedTypesEquality_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:
        ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

.class public auto ansi beforefieldinit CL3
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

.class public auto ansi beforefieldinit CL4
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
{
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
";
            var source = @"
class Module1
{
    static void Main()
    {
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var base1 = compilation.GetTypeByMetadataName("CL2").BaseType();
            var base2 = compilation.GetTypeByMetadataName("CL3").BaseType();
            var base3 = compilation.GetTypeByMetadataName("CL4").BaseType();

            Assert.True(HasTypeArgumentsCustomModifiers(base1));
            Assert.True(HasTypeArgumentsCustomModifiers(base2));
            Assert.True(base1.Equals(base2, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.NotEqual(base1, base2);

            Assert.True(HasTypeArgumentsCustomModifiers(base3));
            Assert.True(base1.Equals(base3, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.Equal(base1, base3);
            Assert.NotSame(base1, base3);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void RetargetingModifiedTypeArgument_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test1
       extends[mscorlib] System.Object
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
    {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000: ldarg.0
    IL_0001: call instance void[mscorlib] System.Object::.ctor()
    IL_0006:
        nop
IL_0007:  ret
  } // end of method Test1::.ctor

  .method public hidebysig newslot virtual
            instance void  Test(valuetype[mscorlib]System.Nullable`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> x) cil managed
    {
    .maxstack  1
    IL_000a:
        ret
  } // end of method Test1::Test

} // end of class Test1
";
            var source = @"
class Module1
    : Test1
{
    public override void Test(System.Nullable<int> x)
    {
    }
}
";
            var compilation1 = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseDll);

            CompileAndVerify(compilation1);

            var test = compilation1.GetTypeByMetadataName("Module1").GetMember<MethodSymbol>("Test");

            Assert.Equal("void Module1.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsLong)? x)", test.ToTestDisplayString());

            Assert.Same(compilation1.SourceModule.CorLibrary(), test.Parameters.First().Type.OriginalDefinition.ContainingAssembly);
            Assert.Same(compilation1.SourceModule.CorLibrary(), ((CSharpCustomModifier)((NamedTypeSymbol)test.Parameters.First().Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].CustomModifiers.First()).ModifierSymbol.ContainingAssembly);

            var compilation2 = CreateCompilationWithMscorlib45(new SyntaxTree[] { }, references: new[] { new CSharpCompilationReference(compilation1) });

            test = compilation2.GetTypeByMetadataName("Module1").GetMember<MethodSymbol>("Test");
            Assert.Equal("void Module1.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsLong)? x)", test.ToTestDisplayString());

            Assert.IsType<CSharp.Symbols.Retargeting.RetargetingAssemblySymbol>(test.ContainingAssembly);
            Assert.Same(compilation2.SourceModule.CorLibrary(), test.Parameters.First().Type.OriginalDefinition.ContainingAssembly);
            Assert.Same(compilation2.SourceModule.CorLibrary(), ((CSharpCustomModifier)((NamedTypeSymbol)test.Parameters.First().Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].CustomModifiers.First()).ModifierSymbol.ContainingAssembly);

            Assert.NotSame(compilation1.SourceModule.CorLibrary(), compilation2.SourceModule.CorLibrary());
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        public void TypeUnification_01()
        {
            var ilSource = @"
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T>
{
} // end of class ITest2`1
";
            var source = @"
interface ITest3<T, U>
    : ITest1<T>, ITest2<U>
{}

interface ITest4<T, U>
    : ITest2<T>, ITest1<U>
{}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseDll);

            compilation.VerifyDiagnostics(
    // (2,11): error CS0695: 'ITest3<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest3<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest3").WithArguments("ITest3<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(2, 11),
    // (6,11): error CS0695: 'ITest4<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest4<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest4").WithArguments("ITest4<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(6, 11)
                );
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        public void TypeUnification_02()
        {
            var ilSource = @"
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T>
{
} // end of class ITest2`1
";
            var source = @"
interface ITest3<T, U>
    : ITest1<T>, ITest2<U>
{}

interface ITest4<T, U>
    : ITest2<T>, ITest1<U>
{}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseDll);

            compilation.VerifyDiagnostics(
    // (2,11): error CS0695: 'ITest3<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest3<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest3").WithArguments("ITest3<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(2, 11),
    // (6,11): error CS0695: 'ITest4<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest4<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest4").WithArguments("ITest4<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(6, 11)
                );
        }

        [Fact]
        public void TypeUnification_03()
        {
            var ilSource = @"
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
} // end of class ITest2`1
";
            var source = @"
interface ITest3<T, U>
    : ITest1<T>, ITest2<U>
{}

interface ITest4<T, U>
    : ITest2<T>, ITest1<U>
{}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseDll);

            compilation.VerifyDiagnostics();
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        public void TypeUnification_04()
        {
            var ilSource = @"
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest2`1
";
            var source = @"
interface ITest3<T, U>
    : ITest1<T>, ITest2<U>
{}

interface ITest4<T, U>
    : ITest2<T>, ITest1<U>
{}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseDll);

            compilation.VerifyDiagnostics(
    // (2,11): error CS0695: 'ITest3<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest3<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest3").WithArguments("ITest3<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(2, 11),
    // (6,11): error CS0695: 'ITest4<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest4<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest4").WithArguments("ITest4<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(6, 11)
                );
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        public void TypeUnification_05()
        {
            var ilSource = @"
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest2`1
";
            var source = @"
interface ITest3<T, U>
    : ITest1<T>, ITest2<U>
{}

interface ITest4<T, U>
    : ITest2<T>, ITest1<U>
{}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseDll);

            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)>", compilation.GetTypeByMetadataName("ITest1`1").Interfaces().First().ToTestDisplayString());
            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsConst)>", compilation.GetTypeByMetadataName("ITest2`1").Interfaces().First().ToTestDisplayString());

            compilation.VerifyDiagnostics(
    // (2,11): error CS0695: 'ITest3<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest3<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest3").WithArguments("ITest3<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(2, 11),
    // (6,11): error CS0695: 'ITest4<T, U>' cannot implement both 'ITest0<T>' and 'ITest0<U>' because they may unify for some type parameter substitutions
    // interface ITest4<T, U>
    Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "ITest4").WithArguments("ITest4<T, U>", "ITest0<T>", "ITest0<U>").WithLocation(6, 11)
                );
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        public void TypeUnification_06()
        {
            var ilSource = @"
.class interface public abstract auto ansi ITest0`1<T>
{
} // end of class ITest0`1

.class interface public abstract auto ansi ITest1`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
} // end of class ITest1`1

.class interface public abstract auto ansi ITest2`1<T>
       implements class ITest0`1<!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)>
{
} // end of class ITest2`1
";
            var source = @"
interface ITest3<T, U>
    : ITest1<T>, ITest2<U>
{}

interface ITest4<T, U>
    : ITest2<T>, ITest1<U>
{}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseDll);

            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsLong) modopt(System.Runtime.CompilerServices.IsConst)>", compilation.GetTypeByMetadataName("ITest1`1").Interfaces().First().ToTestDisplayString());
            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsConst)>", compilation.GetTypeByMetadataName("ITest2`1").Interfaces().First().ToTestDisplayString());

            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void DynamicEncodingDecoding_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(class [mscorlib]System.Collections.Generic.Dictionary`2<!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst),!T1> a,
                                class [mscorlib]System.Collections.Generic.Dictionary`2<!T1,!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)> b,
                                class [mscorlib]System.Collections.Generic.Dictionary`2<!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst),!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)> c) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<object>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<object>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
";
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;

class CL3 : CL2
{
    public override void Test(Dictionary<dynamic, dynamic> a, Dictionary<dynamic, dynamic> b, Dictionary<dynamic, dynamic> c)
    {
        System.Console.WriteLine(""Overridden"");
        foreach (var param in typeof(CL3).GetMethod(""Test"").GetParameters())
            {
                System.Console.WriteLine(param.GetCustomAttributesData().Single());
            }
        }

        static void Main()
        {
            CL2 x = new CL3();
            x.Test(null, null, null);
        }
    }
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Mscorlib40, references: new[] { SystemCoreRef });

            System.Action<ModuleSymbol> validator = (m) =>
            {
                var cl3 = ((ModuleSymbol)m).GlobalNamespace.GetTypeMember("CL3");
                var test = cl3.GetMember<MethodSymbol>("Test");
                Assert.Equal("void CL3.Test(System.Collections.Generic.Dictionary<dynamic modopt(System.Runtime.CompilerServices.IsConst), dynamic> a, System.Collections.Generic.Dictionary<dynamic, dynamic modopt(System.Runtime.CompilerServices.IsConst)> b, System.Collections.Generic.Dictionary<dynamic modopt(System.Runtime.CompilerServices.IsConst), dynamic modopt(System.Runtime.CompilerServices.IsConst)> c)", test.ToTestDisplayString());
            };

            CompileAndVerify(compilation, expectedOutput: @"Overridden
[System.Runtime.CompilerServices.DynamicAttribute(new Boolean[3] { False, True, True })]
[System.Runtime.CompilerServices.DynamicAttribute(new Boolean[3] { False, True, True })]
[System.Runtime.CompilerServices.DynamicAttribute(new Boolean[3] { False, True, True })]",
                             sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void Delegates_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual 
            instance !T1  Test(class [mscorlib]System.Func`2<!T1,!T1> d,
                               !T1 val) cil managed
    {
      // Code size       10 (0xa)
      .maxstack  2
      .locals init ([0] !T1 V_0)
      IL_0000:  ldarg.1
      IL_0001:  ldarg.2
      IL_0002:  callvirt   instance !1 class [mscorlib]System.Func`2<!T1,!T1>::Invoke(!0)
      IL_0007:  stloc.0
      IL_0008:  ldloc.0
      IL_0009:  ret
    } // end of method CL1`1::Test

} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2

.class public auto ansi sealed beforefieldinit MyDelegate
       extends [mscorlib]System.MulticastDelegate
{
    .method public specialname rtspecialname 
            instance void  .ctor(object A_0,
                                 native int A_1) runtime managed forwardref
    {
    } // end of method MyDelegate::.ctor

    .method public newslot virtual final instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                        class [mscorlib]System.AsyncCallback callback,
                        object obj) runtime managed forwardref
    {
    } // end of method MyDelegate::BeginInvoke

    .method public newslot virtual final instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
            EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed forwardref
    {
    } // end of method MyDelegate::EndInvoke

    .method public newslot virtual final instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
            Invoke(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) runtime managed forwardref
    {
    } // end of method MyDelegate::Invoke
} // end of class MyDelegate
";

            var source = @"
class Module1
{
     static void Main()
    {
        CL2 x = new CL2();
        x.Test(Test, 1);
        x.Test((int v) =>
               {
                   System.Console.WriteLine(""Test {0}"", v);
                   return v;
               }, 2);

        x = new CL3();
        x.Test(Test, 3);
        x.Test((int v) =>
               {
                   System.Console.WriteLine(""Test {0}"", v);
                   return v;
               }, 4);

        Test(Test, 5);
        Test((int v) =>
               {
                   System.Console.WriteLine(""Test {0}"", v);
                   return v;
               }, 6);
    }

    static int Test(int v)
    {
        System.Console.WriteLine(""Test {0}"", v);
        return v;
    }

    static int Test(MyDelegate d, int v)
    {
        System.Console.WriteLine(""MyDelegate"");
        return d(v);
    }
}

class CL3 : CL2
{
    public override int Test(System.Func<int, int> x, int y)
    {
        System.Console.WriteLine(""Overridden"");
        return x(y);
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test 1
Test 2
Overridden
Test 3
Overridden
Test 4
MyDelegate
Test 5
MyDelegate
Test 6");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(4623, "https://github.com/dotnet/roslyn/issues/4623")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void MultiDimensionalArray_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test1
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance void  Test(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[0...,0...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      IL_0000:  ldstr      ""Test""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method Test1::Test
} // end of class Test1
";

            var source = @"
class Test
{
    static void Main()
    {
        Test1 x = new Test1();
        x.Test(null);
        x = new Test11();
        x.Test(null);
    }
}

class Test11 : Test1
{
    public override void Test(int [,] c)
    {
        System.Console.WriteLine(""Overridden"");
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ModifiersWithConstructedType_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<valuetype .ctor ([mscorlib]System.ValueType) T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt(valuetype [mscorlib]System.Nullable`1<!T1>) t1) cil managed 
    {
      // Code size       1 (0x1)
      .maxstack  1
      IL_0000:  ldstr      ""Test""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
";

            var source = @"
class Test
{
    static void Main()
    {
        var x = new CL2();
        x.Test(1);
        x = new CL3();
        x.Test(1);
    }
}

class CL3 : CL2
{
    public override void Test(int c)
    {
        System.Console.WriteLine(""Overridden"");
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overridden");
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ModifiersWithConstructedType_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<valuetype .ctor ([mscorlib]System.ValueType) T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test(!T1 modopt(valuetype [mscorlib]System.Nullable`1) t1) cil managed 
    {
      // Code size       1 (0x1)
      .maxstack  1
      IL_0000:  ldstr      ""Test""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
";

            var source = @"
class Test
{
    static void Main()
    {
        var x = new CL2();
        x.Test(1);
        x = new CL3();
        x.Test(1);
    }
}

class CL3 : CL2
{
    public override void Test(int c)
    {
        System.Console.WriteLine(""Overridden"");
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overridden");
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ModifiersWithConstructedType_03()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<valuetype .ctor ([mscorlib]System.ValueType) T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance int32 modopt(CL2) modopt(valuetype [mscorlib]System.Nullable`1<!T1>) modopt(valuetype [mscorlib]System.Nullable`1<!T1>) modopt(CL2) [] Test(!T1 t1) cil managed 
    {
      // Code size       1 (0x1)
      .maxstack  1
      IL_0000:  ldstr      ""Test""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0006:  ldnull
      IL_000a:  ret
    } // end of method CL1`1::Test
} // end of class CL1`1

.class public auto ansi beforefieldinit CL2
       extends class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call instance void class CL1`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>::.ctor()
      IL_0006:  ret
    } // end of method CL2::.ctor
} // end of class CL2
";

            var source = @"
class Test
{
    static void Main()
    {
        var x = new CL2();
        x.Test(1);
        x = new CL3();
        x.Test(1);
    }
}

class CL3 : CL2
{
    public override int[] Test(int c)
    {
        System.Console.WriteLine(""Overridden"");
        return null;
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overridden");
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(14453, "https://github.com/dotnet/roslyn/issues/14453")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ModifiersWithConstructedType_04()
        {
            var source = @"
class Test
{
    static void Main()
    {
        CL1 x = new CL2();
        x.Test<int>(1);
    }
}

class CL2 : CL1
{
    public override System.ValueType Test<U>(System.ValueType c)
    {
        System.Console.WriteLine(""Overridden"");
        return c;
    }
}";
            var compilation = CreateCompilation(source, references: new[] { TestReferences.SymbolsTests.CustomModifiers.GenericMethodWithModifiers.dll },
                                                            options: TestOptions.ReleaseExe);

            var cl2 = compilation.GetTypeByMetadataName("CL2");
            var test = cl2.GetMember<MethodSymbol>("Test");
            Assert.Equal("System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(U?) CL2.Test<U>(System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(U?) c)", test.ToTestDisplayString());
            Assert.Equal("System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(T?) CL1.Test<T>(System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(T?) x)", test.OverriddenMethod.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: @"Overridden");
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(14453, "https://github.com/dotnet/roslyn/issues/14453")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ModifiersWithConstructedType_05()
        {
            var source = @"
class Test
{
    static void Main()
    {
        I1 x = new CL2();
        x.Test<int>(1);

        x = new CL3();
        x.Test<int>(2);
    }
}

class CL2 : I1
{
    public System.ValueType Test<U>(System.ValueType c) where U : struct
    {
        System.Console.WriteLine(""CL2.Test"");
        return c;
    }
}

class CL3 : I1
{
    System.ValueType I1.Test<U>(System.ValueType c) 
    {
        System.Console.WriteLine(""CL3.Test"");
        return c;
    }
}";
            var compilation = CreateCompilation(source, references: new[] { TestReferences.SymbolsTests.CustomModifiers.GenericMethodWithModifiers.dll },
                                                            options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("I1.Test");
            Assert.Equal("System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(U?) CL3.I1.Test<U>(System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(U?) c)", test.ToTestDisplayString());
            Assert.Equal("System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(T?) I1.Test<T>(System.ValueType modopt(System.Runtime.CompilerServices.IsBoxed) modopt(T?) x)", test.ExplicitInterfaceImplementations[0].ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:
@"CL2.Test
CL3.Test");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(5993, "https://github.com/dotnet/roslyn/issues/5993")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRef_05()
        {
            var ilSource = @"
.class interface public abstract auto ansi beforefieldinit I
{
  .method public newslot abstract virtual 
          instance void  A(uint32& modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) x) cil managed
  {
  } // end of method I::A

  .method public newslot abstract virtual 
          instance void  B(uint32& x) cil managed
  {
  } // end of method I::B

} // end of class I
";

            var source = @"
class CI : I 
{
    public void A(ref uint x)
    {
        System.Console.WriteLine(""Implemented A"");
    }

    public void B(ref uint x)
    {
        System.Console.WriteLine(""Implemented B"");
    }
}

internal class Program
{
    private static void Main()
    {
        I x = new CI();
        uint y = 0;
        x.A(ref y);
        x.B(ref y);
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerifyCommon(compilation, expectedOutput: @"Implemented A
Implemented B",
                assemblyValidator: assembly =>
                {
                    var reader = assembly.GetMetadataReader();
                    // Verify synthesized forwarding method I.A was generated.
                    AssertEx.SetEqual(new[] { "A", "B", ".ctor", "I.A", "Main", ".ctor" }, new[] { reader }.GetStrings(reader.GetMethodDefNames()));
                });
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void ConcatModifiersAndByRef_06()
        {
            var ilSource =
@".class interface public abstract I
{
  .method public abstract virtual instance int32& modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) F()
  {
  }
}";
            var source =
@"class C : I 
{
    private int _f;
    public ref int F()
    {
        return ref _f;
    }
    static void Main()
    {
        I x = new C();
        x.F() = 2;
        System.Console.WriteLine(x.F());
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);
            CompileAndVerifyCommon(compilation, expectedOutput: "2",
                assemblyValidator: assembly =>
                {
                    var reader = assembly.GetMetadataReader();
                    // Verify synthesized forwarding method I.F was generated.
                    AssertEx.SetEqual(new[] { ".ctor", "F", "I.F", "Main" }, new[] { reader }.GetStrings(reader.GetMethodDefNames()));
                });
        }

        [Fact, WorkItem(6372, "https://github.com/dotnet/roslyn/issues/6372")]
        public void ModifiedTypeParameterAsTypeArgument_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1`1<T1>
       extends[mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006:
        ret
    } // end of method CL1`1::.ctor

    .method public hidebysig newslot virtual
            instance void  Test1(class CL1`1<!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)> t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:
        ret
    } // end of method CL1`1::Test

    .method public hidebysig newslot virtual
            instance void  Test2(class CL1`1<!T1> t1) cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:
        ret
    } // end of method CL1`1::Test
} // end of class CL1`1
";
            var source = @"";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var cl1 = compilation.GetTypeByMetadataName("CL1`1");
            var test1 = cl1.GetMember<MethodSymbol>("Test1");
            Assert.Equal("void CL1<T1>.Test1(CL1<T1 modopt(System.Runtime.CompilerServices.IsConst)> t1)", test1.ToTestDisplayString());

            var test2 = cl1.GetMember<MethodSymbol>("Test2");
            Assert.Equal("void CL1<T1>.Test2(CL1<T1> t1)", test2.ToTestDisplayString());

            var t1 = test1.Parameters[0].TypeWithAnnotations;
            var t2 = test2.Parameters[0].TypeWithAnnotations;

            Assert.False(t1.Equals(t2, TypeCompareKind.ConsiderEverything));
            Assert.False(t2.Equals(t1, TypeCompareKind.ConsiderEverything));
            Assert.False(t1.Type.Equals(t2.Type));
            Assert.False(t2.Type.Equals(t1.Type));

            Assert.True(t1.Equals(t2, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.True(t2.Equals(t1, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.True(t1.Type.Equals(t2.Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.True(t2.Type.Equals(t1.Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(7674, "https://github.com/dotnet/roslyn/issues/7674")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void PropertyWithDynamic()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1
       extends [mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .property instance object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
            Test()
    {
      .get instance object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1::get_Test()
      .set instance void CL1::set_Test(object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
    } // end of property CL1::Test

    .method public hidebysig newslot specialname virtual
            instance object modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
            get_Test() cil managed
    {
      // Code size       2 (0x2)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
    } // end of method CL1::get_Test

    .method public hidebysig newslot specialname virtual
            instance void  set_Test(object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed
    {
      // Code size       3 (0x3)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
      IL_0002:  ret
    } // end of method CL1::set_Test
} // end of class CL1
";
            var source = @"
class Module1
{
    static void Main()
    {
        CL1 x = new CL2();

        x.Test = null;
        var y = x.Test;

        x = new CL3();

        x.Test = null;
        var z = x.Test;
    }
}

class CL2
    : CL1
{
    public override dynamic[] Test
    {
        get
        {
            System.Console.WriteLine(""Get Overridden2"");
            return null;
        }
        set
        {
            System.Console.WriteLine(""Set Overridden2"");
        }
    }
}

class CL3
    : CL1
{
    public override object[] Test
    {
        get
        {
            System.Console.WriteLine(""Get Overridden3"");
            return null;
        }
        set
        {
            System.Console.WriteLine(""Set Overridden3"");
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, references: new[] { CSharpRef }, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Standard);

            var cl2 = compilation.GetTypeByMetadataName("CL2");
            var test2 = cl2.GetMember<PropertySymbol>("Test");
            Assert.Equal("dynamic modopt(System.Runtime.CompilerServices.IsConst) [] modopt(System.Runtime.CompilerServices.IsConst) CL2.Test { get; set; }",
                         test2.ToTestDisplayString());

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test3 = cl3.GetMember<PropertySymbol>("Test");
            Assert.Equal("System.Object modopt(System.Runtime.CompilerServices.IsConst) [] modopt(System.Runtime.CompilerServices.IsConst) CL3.Test { get; set; }",
                         test3.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: @"Set Overridden2
Get Overridden2
Set Overridden3
Get Overridden3");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(7674, "https://github.com/dotnet/roslyn/issues/7674")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void EventWithDynamic()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1
       extends [mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .event class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]> Test
    {
      .addon instance void CL1::add_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]>)
      .removeon instance void CL1::remove_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]>)
    } // end of event CL1::Test

    .method public hidebysig newslot specialname virtual 
            instance void  add_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]> 'value') cil managed
    {
      // Code size       2 (0x2)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
    } // end of method CL1::get_Test

    .method public hidebysig newslot specialname virtual 
            instance void  remove_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]> 'value') cil managed
    {
      // Code size       3 (0x3)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
      IL_0002:  ret
    } // end of method CL1::set_Test
} // end of class CL1
";
            var source = @"
using System;

class Module1
{
    static void Main()
    {
        CL1 x = new CL2();

        x.Test+= null;
        x.Test-= null;

        x = new CL3();

        x.Test+= null;
        x.Test-= null;
    }
}

class CL2
    : CL1
{
    public override event Action<dynamic[]> Test
    {
        add
        {
            System.Console.WriteLine(""Add Overridden2"");
        }
        remove
        {
            System.Console.WriteLine(""Remove Overridden2"");
        }
    }
}

class CL3
    : CL1
{
    public override event Action<object[]> Test
    {
        add
        {
            System.Console.WriteLine(""Add Overridden3"");
        }
        remove
        {
            System.Console.WriteLine(""Remove Overridden3"");
        }
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, references: new[] { CSharpRef }, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Standard);

            var cl2 = compilation.GetTypeByMetadataName("CL2");
            var test2 = cl2.GetMember<EventSymbol>("Test");
            Assert.Equal("event System.Action<dynamic modopt(System.Runtime.CompilerServices.IsConst) []> CL2.Test",
                         test2.ToTestDisplayString());

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test3 = cl3.GetMember<EventSymbol>("Test");
            Assert.Equal("event System.Action<System.Object modopt(System.Runtime.CompilerServices.IsConst) []> CL3.Test",
                         test3.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: @"Add Overridden2
Remove Overridden2
Add Overridden3
Remove Overridden3");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void EventFieldWithDynamic()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit CL1
       extends [mscorlib] System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: call instance void[mscorlib] System.Object::.ctor()
      IL_0006: ret
    } // end of method CL1`1::.ctor

    .event class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]> Test
    {
      .addon instance void CL1::add_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]>)
      .removeon instance void CL1::remove_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]>)
    } // end of event CL1::Test

    .method public hidebysig newslot specialname virtual 
            instance void  add_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]> 'value') cil managed
    {
      // Code size       2 (0x2)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
    } // end of method CL1::get_Test

    .method public hidebysig newslot specialname virtual 
            instance void  remove_Test(class [mscorlib]System.Action`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)[]> 'value') cil managed
    {
      // Code size       3 (0x3)
      .maxstack  1
      IL_0000: ldarg.0
      IL_0001: throw
      IL_0002:  ret
    } // end of method CL1::set_Test
} // end of class CL1
";
            var source = @"
using System;

class Module1
{
    static void Main()
    {
        CL2 cl2 = new CL2();
        CL1 cl1 = cl2;
        cl1.Test += (d) => Console.WriteLine(d[0] + "" and "" + d[1]);
        cl2.Raise();

        CL3 cl3 = new CL3();
        cl1 = cl3;
        cl1.Test += (d) => Console.WriteLine(""Charlie"");
        cl3.Raise();
    }
}

class CL2 : CL1
{
    public override event Action<dynamic[]> Test;
    public void Raise() => Test(new string[] { ""Alice"", ""Bob"" });
}

class CL3 : CL1
{
    public override event Action<object[]> Test;
    public void Raise() => Test(null);
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, references: new[] { CSharpRef }, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Standard);

            var cl2 = compilation.GetTypeByMetadataName("CL2");
            var test2 = cl2.GetMember<EventSymbol>("Test");
            Assert.Equal("event System.Action<dynamic modopt(System.Runtime.CompilerServices.IsConst) []> CL2.Test",
                         test2.ToTestDisplayString());

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test3 = cl3.GetMember<EventSymbol>("Test");
            Assert.Equal("event System.Action<System.Object modopt(System.Runtime.CompilerServices.IsConst) []> CL3.Test",
                         test3.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:
@"Alice and Bob
Charlie");
        }

        [Fact]
        [WorkItem(58520, "https://github.com/dotnet/roslyn/issues/58520")]
        public void Issue58520_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C1`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig static 
        string Method () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 6 (0x6)
        .maxstack 8

        IL_0000: ldstr ""Method""
        IL_0005: ret
    } // end of method C1`1::Method

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C1`1::.ctor

} // end of class C1`1

.class public auto ansi beforefieldinit C2`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi beforefieldinit C3`1<T>
    extends class C1`1<int32 modopt(class C2`1<!T>)>
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x205f
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void class C1`1<int32>::.ctor()
        IL_0006: ret
    } // end of method C3`1::.ctor

} // end of class C3`1
";

            var source = @"
class Test
{
    static void Main()
    {
        M<int>();
    }

    static void M<T>()
    {
        System.Func<string> x = C3<T>.Method;
        System.Console.WriteLine(x());
    }
}
";
            var compilation = CreateCompilationWithIL(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Method");
        }

        [Fact]
        [WorkItem(58520, "https://github.com/dotnet/roslyn/issues/58520")]
        public void Issue58520_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C1`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig static 
        string Method () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 6 (0x6)
        .maxstack 8

        IL_0000: ldstr ""Method""
        IL_0005: ret
    } // end of method C1`1::Method

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C1`1::.ctor

} // end of class C1`1

.class public auto ansi beforefieldinit C2`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi beforefieldinit C3`1<T>
    extends class C1`1<int32 modopt(class C2`1<!T>)>
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x205f
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void class C1`1<int32>::.ctor()
        IL_0006: ret
    } // end of method C3`1::.ctor

} // end of class C3`1
";

            var source = @"
class Test
{
    static void Main()
    {
        M<int>();
    }

    static void M<T>()
    {
        System.Func<string> x0 = C1<int>.Method;
        System.Func<string> x1 = C3<T>.Method;
        System.Console.WriteLine(x0()+x1());
    }
}
";
            var compilation = CreateCompilationWithIL(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"MethodMethod");
        }
    }
}
