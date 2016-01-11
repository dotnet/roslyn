// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: "Test");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options:TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: "Test");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            var withModifiers = cl3.BaseType.BaseType;
            var withoutModifiers = withModifiers.OriginalDefinition.Construct(withModifiers.TypeArguments);
            Assert.True(withModifiers.HasTypeArgumentsCustomModifiers);
            Assert.False(withoutModifiers.HasTypeArgumentsCustomModifiers);
            Assert.True(withoutModifiers.Equals(withModifiers, ignoreCustomModifiersAndArraySizesAndLowerBounds:true));
            Assert.NotEqual(withoutModifiers, withModifiers);

            CompileAndVerify(compilation, expectedOutput: "Overriden");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overriden");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overriden");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref System.Int32 modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput: "Overriden");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(ref modopt(System.Runtime.CompilerServices.IsConst) System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile) modopt(System.Runtime.CompilerServices.IsLong) x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:"Overriden");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
            System.Console.WriteLine(""Get Overriden"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""Set Overriden"");
        }
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<PropertySymbol>("Test");
            Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL3.Test { get; set; }", test.ToTestDisplayString());
            Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL3.Test.get", test.GetMethod.ToTestDisplayString());
            Assert.True(test.GetMethod.ReturnTypeCustomModifiers.SequenceEqual(test.SetMethod.Parameters.First().CustomModifiers));

            CompileAndVerify(compilation, expectedOutput: @"Set Overriden
Get Overriden");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl3 = compilation.GetTypeByMetadataName("CL3");
            var test = cl3.GetMember<MethodSymbol>("Test");
            Assert.Equal("void CL3.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) [] x)", test.ToTestDisplayString());

            CompileAndVerify(compilation, expectedOutput:"Overriden");
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options:TestOptions.ReleaseExe);

            var cl2 = compilation.GetTypeByMetadataName("CL2");
            Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong) CL1<System.Int32 modopt(System.Runtime.CompilerServices.IsLong)>.Test", cl2.BaseType.GetMember("Test").ToTestDisplayString());

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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options:TestOptions.ReleaseExe);

            var base1 = compilation.GetTypeByMetadataName("CL2").BaseType;
            var base2 = compilation.GetTypeByMetadataName("CL3").BaseType;
            var base3 = compilation.GetTypeByMetadataName("CL4").BaseType;

            Assert.True(base1.HasTypeArgumentsCustomModifiers);
            Assert.True(base2.HasTypeArgumentsCustomModifiers);
            Assert.True(base1.Equals(base2, ignoreCustomModifiersAndArraySizesAndLowerBounds:true));
            Assert.NotEqual(base1, base2);

            Assert.True(base3.HasTypeArgumentsCustomModifiers);
            Assert.True(base1.Equals(base3, ignoreCustomModifiersAndArraySizesAndLowerBounds: true));
            Assert.Equal(base1, base3);
            Assert.NotSame(base1, base3);
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
            var compilation1 = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseDll);

            CompileAndVerify(compilation1);

            var test = compilation1.GetTypeByMetadataName("Module1").GetMember<MethodSymbol>("Test");

            Assert.Equal("void Module1.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsLong)? x)", test.ToTestDisplayString());

            Assert.Same(compilation1.SourceModule.CorLibrary(), test.Parameters.First().Type.OriginalDefinition.ContainingAssembly);
            Assert.Same(compilation1.SourceModule.CorLibrary(), ((NamedTypeSymbol)test.Parameters.First().Type).TypeArgumentsCustomModifiers.First().First().Modifier.ContainingAssembly);

            var compilation2 = CreateCompilationWithMscorlib45(new SyntaxTree[] {}, references: new [] {new CSharpCompilationReference(compilation1)});

            test = compilation2.GetTypeByMetadataName("Module1").GetMember<MethodSymbol>("Test");
            Assert.Equal("void Module1.Test(System.Int32 modopt(System.Runtime.CompilerServices.IsLong)? x)", test.ToTestDisplayString());

            Assert.IsType<CSharp.Symbols.Retargeting.RetargetingAssemblySymbol>(test.ContainingAssembly);
            Assert.Same(compilation2.SourceModule.CorLibrary(), test.Parameters.First().Type.OriginalDefinition.ContainingAssembly);
            Assert.Same(compilation2.SourceModule.CorLibrary(), ((NamedTypeSymbol)test.Parameters.First().Type).TypeArgumentsCustomModifiers.First().First().Modifier.ContainingAssembly);

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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseDll);

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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseDll);

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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseDll);

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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseDll);

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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseDll);

            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsLong)>", compilation.GetTypeByMetadataName("ITest1`1").Interfaces.First().ToTestDisplayString());
            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsConst)>", compilation.GetTypeByMetadataName("ITest2`1").Interfaces.First().ToTestDisplayString());

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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseDll);

            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsLong) modopt(System.Runtime.CompilerServices.IsConst)>", compilation.GetTypeByMetadataName("ITest1`1").Interfaces.First().ToTestDisplayString());
            Assert.Equal("ITest0<T modopt(System.Runtime.CompilerServices.IsConst)>", compilation.GetTypeByMetadataName("ITest2`1").Interfaces.First().ToTestDisplayString());

            compilation.VerifyDiagnostics();
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm), WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe, references: new[] { SystemCoreRef });

            System.Action<IModuleSymbol> validator = (m) =>
            {
                var cl3 = ((ModuleSymbol)m).GlobalNamespace.GetTypeMember("CL3");
                var test = cl3.GetMember<MethodSymbol>("Test");
                Assert.Equal("void CL3.Test(System.Collections.Generic.Dictionary<dynamic modopt(System.Runtime.CompilerServices.IsConst), dynamic> a, System.Collections.Generic.Dictionary<dynamic, dynamic modopt(System.Runtime.CompilerServices.IsConst)> b, System.Collections.Generic.Dictionary<dynamic modopt(System.Runtime.CompilerServices.IsConst), dynamic modopt(System.Runtime.CompilerServices.IsConst)> c)", test.ToTestDisplayString());
            };

            CompileAndVerify(compilation, expectedOutput: @"Overriden
[System.Runtime.CompilerServices.DynamicAttribute(new Boolean[3] { False, True, True })]
[System.Runtime.CompilerServices.DynamicAttribute(new Boolean[3] { False, True, True })]
[System.Runtime.CompilerServices.DynamicAttribute(new Boolean[3] { False, True, True })]",
                             sourceSymbolValidator:validator, symbolValidator:validator);
        }

        [Fact, WorkItem(4163, "https://github.com/dotnet/roslyn/issues/4163")]
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
        System.Console.WriteLine(""Overriden"");
        return x(y);
    }
}
";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test 1
Test 2
Overriden
Test 3
Overriden
Test 4
MyDelegate
Test 5
MyDelegate
Test 6");
        }

        [Fact, WorkItem(4623, "https://github.com/dotnet/roslyn/issues/4623")]
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
        System.Console.WriteLine(""Overriden"");
    }
}";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overriden");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm), WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")]
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
        System.Console.WriteLine(""Overriden"");
    }
}";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overriden");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm), WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")]
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
        System.Console.WriteLine(""Overriden"");
    }
}";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overriden");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm), WorkItem(5725, "https://github.com/dotnet/roslyn/issues/5725")]
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
        System.Console.WriteLine(""Overriden"");
        return null;
    }
}";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Test
Overriden");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm), WorkItem(5993, "https://github.com/dotnet/roslyn/issues/5993")]
        public void ConcatModifiersAndByRef_05()
        {
            var ilSource = @"
.class interface public abstract auto ansi beforefieldinit X.I
{
  .method public newslot abstract virtual 
          instance void  A(uint32& modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) x) cil managed
  {
  } // end of method I::A

  .method public newslot abstract virtual 
          instance void  B(uint32& x) cil managed
  {
  } // end of method I::B

} // end of class X.I
";

            var source = @"
using X;

namespace ConsoleApplication21
{
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
    }
}";
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"Implemented A
Implemented B");
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
            var compilation = CreateCompilationWithCustomILSource(source, ilSource, options: TestOptions.ReleaseExe);

            var cl1 = compilation.GetTypeByMetadataName("CL1`1");
            var test1 = cl1.GetMember<MethodSymbol>("Test1");
            Assert.Equal("void CL1<T1>.Test1(CL1<T1 modopt(System.Runtime.CompilerServices.IsConst)> t1)", test1.ToTestDisplayString());

            var test2 = cl1.GetMember<MethodSymbol>("Test2");
            Assert.Equal("void CL1<T1>.Test2(CL1<T1> t1)", test2.ToTestDisplayString());

            var t1 = test1.Parameters[0].Type;
            var t2 = test2.Parameters[0].Type;

            Assert.False(t1.Equals(t2));
            Assert.False(t2.Equals(t1));

            Assert.True(t1.Equals(t2, ignoreCustomModifiersAndArraySizesAndLowerBounds: true));
            Assert.True(t2.Equals(t1, ignoreCustomModifiersAndArraySizesAndLowerBounds: true));
        }

    }
}