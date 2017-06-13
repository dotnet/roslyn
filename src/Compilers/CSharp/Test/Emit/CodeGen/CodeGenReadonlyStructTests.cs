﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class CodeGenReadOnlyStructTests : CompilingTestBase
    {
        [Fact]
        public void InvokeOnReadOnlyStaticField()
        {
            var text = @"
class Program
{
    static readonly S1 sf;
    static void Main()
    {
        System.Console.Write(sf.M1());
        System.Console.Write(sf.ToString());
    }
    readonly struct S1
    {
        public string M1()
        {
            return ""1"";
        }
        public override string ToString()
        {
            return ""2"";
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"12");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  1
  IL_0000:  ldsflda    ""Program.S1 Program.sf""
  IL_0005:  call       ""string Program.S1.M1()""
  IL_000a:  call       ""void System.Console.Write(string)""
  IL_000f:  ldsflda    ""Program.S1 Program.sf""
  IL_0014:  constrained. ""Program.S1""
  IL_001a:  callvirt   ""string object.ToString()""
  IL_001f:  call       ""void System.Console.Write(string)""
  IL_0024:  ret
}");
        }

        [Fact]
        public void InvokeOnReadOnlyStaticFieldMetadata()
        {
            var text1 = @"
    public readonly struct S1
    {
        public string M1()
        {
            return ""1"";
        }
        public override string ToString()
        {
            return ""2"";
        }
    }
";

            var comp1 = CreateStandardCompilation(text1, assemblyName: "A");
            var ref1 = comp1.EmitToImageReference();

            var text = @"
class Program
{
    static readonly S1 sf;
    static void Main()
    {
        System.Console.Write(sf.M1());
        System.Console.Write(sf.ToString());
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef, ref1}, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"12");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  1
  IL_0000:  ldsflda    ""S1 Program.sf""
  IL_0005:  call       ""string S1.M1()""
  IL_000a:  call       ""void System.Console.Write(string)""
  IL_000f:  ldsflda    ""S1 Program.sf""
  IL_0014:  constrained. ""S1""
  IL_001a:  callvirt   ""string object.ToString()""
  IL_001f:  call       ""void System.Console.Write(string)""
  IL_0024:  ret
}");
        }

        [Fact]
        public void InvokeOnReadOnlyInstanceField()
        {
            var text = @"
class Program
{
    readonly S1 f;
    static void Main()
    {
        var p = new Program();
        System.Console.Write(p.f.M1());
        System.Console.Write(p.f.ToString());
    }
    readonly struct S1
    {
        public string M1()
        {
            return ""1"";
        }
        public override string ToString()
        {
            return ""2"";
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"12");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""Program.S1 Program.f""
  IL_000b:  call       ""string Program.S1.M1()""
  IL_0010:  call       ""void System.Console.Write(string)""
  IL_0015:  ldflda     ""Program.S1 Program.f""
  IL_001a:  constrained. ""Program.S1""
  IL_0020:  callvirt   ""string object.ToString()""
  IL_0025:  call       ""void System.Console.Write(string)""
  IL_002a:  ret
}");
        }

        [Fact]
        public void InvokeOnReadOnlyInstanceFieldGeneric()
        {
            var text = @"
class Program
{
    readonly S1<string> f;

    static void Main()
    {
        var p = new Program();
        System.Console.Write(p.f.M1(""hello""));
        System.Console.Write(p.f.ToString());
    }

    readonly struct S1<T>
    {
        public T M1(T arg)
        {
            return arg;
        }

        public override string ToString()
        {
            return ""2"";
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"hello2");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       48 (0x30)
  .maxstack  3
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""Program.S1<string> Program.f""
  IL_000b:  ldstr      ""hello""
  IL_0010:  call       ""string Program.S1<string>.M1(string)""
  IL_0015:  call       ""void System.Console.Write(string)""
  IL_001a:  ldflda     ""Program.S1<string> Program.f""
  IL_001f:  constrained. ""Program.S1<string>""
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  call       ""void System.Console.Write(string)""
  IL_002f:  ret
}");
        }

        [Fact]
        public void InvokeOnReadOnlyInstanceFieldGenericMetadata()
        {
            var text1 = @"
    readonly public struct S1<T>
    {
        public T M1(T arg)
        {
            return arg;
        }

        public override string ToString()
        {
            return ""2"";
        }
    }
";

            var comp1 = CreateStandardCompilation(text1, assemblyName: "A");
            var ref1 = comp1.EmitToImageReference();

            var text = @"
class Program
{
    readonly S1<string> f;

    static void Main()
    {
        var p = new Program();
        System.Console.Write(p.f.M1(""hello""));
        System.Console.Write(p.f.ToString());
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef, ref1 }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"hello2");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       48 (0x30)
  .maxstack  3
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""S1<string> Program.f""
  IL_000b:  ldstr      ""hello""
  IL_0010:  call       ""string S1<string>.M1(string)""
  IL_0015:  call       ""void System.Console.Write(string)""
  IL_001a:  ldflda     ""S1<string> Program.f""
  IL_001f:  constrained. ""S1<string>""
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  call       ""void System.Console.Write(string)""
  IL_002f:  ret
}");
        }

        [Fact]
        public void InvokeOnReadOnlyThis()
        {
            var text = @"
class Program
{
    static void Main()
    {
        Test(default(S1));
    }
    static void Test(in S1 arg)
    {
        System.Console.Write(arg.M1());
        System.Console.Write(arg.ToString());
    }
    readonly struct S1
    {
        public string M1()
        {
            return ""1"";
        }
        public override string ToString()
        {
            return ""2"";
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"12");

            comp.VerifyIL("Program.Test", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""string Program.S1.M1()""
  IL_0006:  call       ""void System.Console.Write(string)""
  IL_000b:  ldarg.0
  IL_000c:  constrained. ""Program.S1""
  IL_0012:  callvirt   ""string object.ToString()""
  IL_0017:  call       ""void System.Console.Write(string)""
  IL_001c:  ret
}");
        }

        [Fact]
        public void InvokeOnThis()
        {
            var text = @"
class Program
{
    static void Main()
    {
        default(S1).Test();
    }
    readonly struct S1
    {
        public void Test()
        {
            System.Console.Write(this.M1());
            System.Console.Write(ToString());
        }
        public string M1()
        {
            return ""1"";
        }
        public override string ToString()
        {
            return ""2"";
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"12");

            comp.VerifyIL("Program.S1.Test()", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""string Program.S1.M1()""
  IL_0006:  call       ""void System.Console.Write(string)""
  IL_000b:  ldarg.0
  IL_000c:  constrained. ""Program.S1""
  IL_0012:  callvirt   ""string object.ToString()""
  IL_0017:  call       ""void System.Console.Write(string)""
  IL_001c:  ret
}");

            comp.VerifyIL("Program.Main()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (Program.S1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Program.S1""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""void Program.S1.Test()""
  IL_0011:  ret
}");

        }

        [Fact]
        public void InvokeOnThisBaseMethods()
        {
            var text = @"
class Program
{
    static void Main()
    {
        default(S1).Test();
    }
    readonly struct S1
    {
        public void Test()
        {
            System.Console.Write(this.GetType());
            System.Console.Write(ToString());
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"Program+S1Program+S1");

            comp.VerifyIL("Program.S1.Test()", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""Program.S1""
  IL_0006:  box        ""Program.S1""
  IL_000b:  call       ""System.Type object.GetType()""
  IL_0010:  call       ""void System.Console.Write(object)""
  IL_0015:  ldarg.0
  IL_0016:  constrained. ""Program.S1""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  call       ""void System.Console.Write(string)""
  IL_0026:  ret
}");
        }

        [Fact]
        public void AssignThis()
        {
            var text = @"
class Program
{
    static void Main()
    {
        S1 v = new S1(42);
        System.Console.Write(v.x);

        S1 v2 = new S1(v);
        System.Console.Write(v2.x);
    }

    readonly struct S1
    {
        public readonly int x;

        public S1(int i)
        {            
            x = i; // OK
        }

        public S1(S1 arg)
        {
            this = arg; // OK
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"4242");

            comp.VerifyIL("Program.S1..ctor(int)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int Program.S1.x""
  IL_0007:  ret
}");

            comp.VerifyIL("Program.S1..ctor(Program.S1)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stobj      ""Program.S1""
  IL_0007:  ret
}");
        }

        [Fact]
        public void AssignThisErr()
        {
            var text = @"
class Program
{
    static void Main()
    {
    }

    static void TakesRef(ref S1 arg){}
    static void TakesRef(ref int arg){}

    readonly struct S1
    {
        readonly int x;

        public S1(int i)
        {            
            x = i; // OK
        }

        public S1(S1 arg)
        {
            this = arg; // OK
        }

        public void Test1()
        {            
            this = default; // error
        }

        public void Test2()
        {           
            TakesRef(ref this); // error
        }

        public void Test3()
        {            
            TakesRef(ref this.x); // error
        }
    }
}
";

            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics(
                // (27,13): error CS1604: Cannot assign to 'this' because it is read-only
                //             this = default; // error
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(27, 13),
                // (32,26): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //             TakesRef(ref this); // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(32, 26),
                // (37,26): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //             TakesRef(ref this.x); // error
                Diagnostic(ErrorCode.ERR_RefReadonly, "this.x").WithLocation(37, 26),
                // (37,26): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //             TakesRef(ref this.x); // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this.x").WithArguments("this").WithLocation(37, 26)
                );

        }

        [Fact]
        public void AssignThisNestedMethods()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
    }

    static void TakesRef(ref S1 arg){}
    static void TakesRef(ref int arg){}

    readonly struct S1
    {
        readonly int x;

        public S1(int i)
        {            
            void F() { x = i;} // Error           
            Action a = () => { x = i;}; // Error 
            F();
        }

        public S1(S1 arg)
        {
            void F() { this = arg;} // Error
            Action a = () => { this = arg;}; // Error 
            F();
        }

        public void Test1()
        {            
            void F() { this = default;} // Error
            Action a = () => { this = default;}; // Error 
            F();
        }

        public void Test2()
        {           
            void F() { TakesRef(ref this);} // Error
            Action a = () => { TakesRef(ref this);}; // Error 
            F();
        }

        public void Test3()
        {            
            void F() { TakesRef(ref this.x);} // Error
            Action a = () => { TakesRef(ref this.x);}; // Error 
            F();
        }
    }
}
";

            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics(
                // (19,24): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             void F() { x = i;} // Error           
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "x").WithLocation(19, 24),
                // (20,32): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             Action a = () => { x = i;}; // Error 
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "x").WithLocation(20, 32),
                // (26,24): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             void F() { this = arg;} // Error
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(26, 24),
                // (27,32): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             Action a = () => { this = arg;}; // Error 
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(27, 32),
                // (33,24): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             void F() { this = default;} // Error
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(33, 24),
                // (34,32): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             Action a = () => { this = default;}; // Error 
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(34, 32),
                // (40,37): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             void F() { TakesRef(ref this);} // Error
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(40, 37),
                // (41,45): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             Action a = () => { TakesRef(ref this);}; // Error 
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(41, 45),
                // (47,37): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             void F() { TakesRef(ref this.x);} // Error
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(47, 37),
                // (48,45): error CS1673: Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
                //             Action a = () => { TakesRef(ref this.x);}; // Error 
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(48, 45)
                );

        }

        [Fact]
        public void ReadOnlyStructApi()
        {
            var text = @"
class Program
{
    readonly struct S1
    {
        public S1(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }

    readonly struct S1<T>
    {
        public S1(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }

    struct S2
    {
        public S2(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }

    class C1
    {
        public C1(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }  

    delegate int D1();
}
";

            var comp = CreateStandardCompilation(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular);

            // S1
            NamedTypeSymbol namedType = comp.GetTypeByMetadataName("Program+S1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S1<T>
            namedType = comp.GetTypeByMetadataName("Program+S1`1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // T
            TypeSymbol type = namedType.TypeParameters[0];
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S1<object>
            namedType = namedType.Construct(comp.ObjectType);
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S2
            namedType = comp.GetTypeByMetadataName("Program+S2");
            Assert.False(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.Ref, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.Ref, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // C1
            namedType = comp.GetTypeByMetadataName("Program+C1");
            Assert.False(namedType.IsReadOnly);
            Assert.Equal(RefKind.None, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.None, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.None, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // D1
            namedType = comp.GetTypeByMetadataName("Program+D1");
            Assert.False(namedType.IsReadOnly);
            Assert.Equal(RefKind.None, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.None, namedType.GetMethod("Invoke").ThisParameter.RefKind);

            // object[]
            type = comp.CreateArrayTypeSymbol(comp.ObjectType);
            Assert.False(type.IsReadOnly);

            // dynamic
            type = comp.DynamicType;
            Assert.False(type.IsReadOnly);

            // object
            type = comp.ObjectType;
            Assert.False(type.IsReadOnly);

            // anonymous type
            type = (TypeSymbol)comp.CreateAnonymousTypeSymbol(ImmutableArray.Create<ITypeSymbol>(comp.ObjectType), ImmutableArray.Create("qq"));
            Assert.False(type.IsReadOnly);

            // pointer type
            type = (TypeSymbol)comp.CreatePointerTypeSymbol(comp.ObjectType);
            Assert.False(type.IsReadOnly);

            // tuple type
            type = (TypeSymbol)comp.CreateTupleTypeSymbol(ImmutableArray.Create<ITypeSymbol>(comp.ObjectType, comp.ObjectType));
            Assert.False(type.IsReadOnly);

        }

        [Fact]
        public void ReadOnlyStructApiMetadata()
        {
            var text1 = @"
class Program
{
    readonly struct S1
    {
        public S1(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }

    readonly struct S1<T>
    {
        public S1(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }

    struct S2
    {
        public S2(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }

    class C1
    {
        public C1(int dummy)
        {
        }

        public string M1()
        {
            return ""1"";
        }

        public override string ToString()
        {
            return ""2"";
        }
    }  

    delegate int D1();
}
";
            var comp1 = CreateStandardCompilation(text1, assemblyName: "A");
            var ref1 = comp1.EmitToImageReference();

            var comp = CreateStandardCompilation("//NO CODE HERE", new[] { ValueTupleRef, SystemRuntimeFacadeRef, ref1 }, parseOptions: TestOptions.Regular);

            // S1
            NamedTypeSymbol namedType = comp.GetTypeByMetadataName("Program+S1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S1<T>
            namedType = comp.GetTypeByMetadataName("Program+S1`1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // T
            TypeSymbol type = namedType.TypeParameters[0];
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S1<object>
            namedType = namedType.Construct(comp.ObjectType);
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.RefReadOnly, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S2
            namedType = comp.GetTypeByMetadataName("Program+S2");
            Assert.False(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.Ref, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.Ref, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // C1
            namedType = comp.GetTypeByMetadataName("Program+C1");
            Assert.False(namedType.IsReadOnly);
            Assert.Equal(RefKind.None, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.None, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.None, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // D1
            namedType = comp.GetTypeByMetadataName("Program+D1");
            Assert.False(namedType.IsReadOnly);
            Assert.Equal(RefKind.None, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.None, namedType.GetMethod("Invoke").ThisParameter.RefKind);

            // object[]
            type = comp.CreateArrayTypeSymbol(comp.ObjectType);
            Assert.False(type.IsReadOnly);

            // dynamic
            type = comp.DynamicType;
            Assert.False(type.IsReadOnly);

            // object
            type = comp.ObjectType;
            Assert.False(type.IsReadOnly);

            // anonymous type
            type = (TypeSymbol)comp.CreateAnonymousTypeSymbol(ImmutableArray.Create<ITypeSymbol>(comp.ObjectType), ImmutableArray.Create("qq"));
            Assert.False(type.IsReadOnly);

            // pointer type
            type = (TypeSymbol)comp.CreatePointerTypeSymbol(comp.ObjectType);
            Assert.False(type.IsReadOnly);

            // tuple type
            type = (TypeSymbol)comp.CreateTupleTypeSymbol(ImmutableArray.Create<ITypeSymbol>(comp.ObjectType, comp.ObjectType));
            Assert.False(type.IsReadOnly);

        }

    }
}