// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
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
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"12");

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

            comp = CompileAndVerify(text, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Passes, expectedOutput: @"12");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  1
  .locals init (Program.S1 V_0)
  IL_0000:  ldsfld     ""Program.S1 Program.sf""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""string Program.S1.M1()""
  IL_000d:  call       ""void System.Console.Write(string)""
  IL_0012:  ldsfld     ""Program.S1 Program.sf""
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  constrained. ""Program.S1""
  IL_0020:  callvirt   ""string object.ToString()""
  IL_0025:  call       ""void System.Console.Write(string)""
  IL_002a:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
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

            var comp1 = CreateCompilation(text1, assemblyName: "A");
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

            var comp = CompileAndVerify(text, new[] { ref1 }, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"12");

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

            comp = CompileAndVerify(text, new[] { ref1 }, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Passes, expectedOutput: @"12");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldsfld     ""S1 Program.sf""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""string S1.M1()""
  IL_000d:  call       ""void System.Console.Write(string)""
  IL_0012:  ldsfld     ""S1 Program.sf""
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  constrained. ""S1""
  IL_0020:  callvirt   ""string object.ToString()""
  IL_0025:  call       ""void System.Console.Write(string)""
  IL_002a:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"12");

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

            comp = CompileAndVerify(text, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Passes, expectedOutput: @"12");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Program.S1 V_0)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""Program.S1 Program.f""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""string Program.S1.M1()""
  IL_0013:  call       ""void System.Console.Write(string)""
  IL_0018:  ldfld      ""Program.S1 Program.f""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  constrained. ""Program.S1""
  IL_0026:  callvirt   ""string object.ToString()""
  IL_002b:  call       ""void System.Console.Write(string)""
  IL_0030:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"hello2");

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

            comp = CompileAndVerify(text, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Passes, expectedOutput: @"hello2");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (Program.S1<string> V_0)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""Program.S1<string> Program.f""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldstr      ""hello""
  IL_0013:  call       ""string Program.S1<string>.M1(string)""
  IL_0018:  call       ""void System.Console.Write(string)""
  IL_001d:  ldfld      ""Program.S1<string> Program.f""
  IL_0022:  stloc.0
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. ""Program.S1<string>""
  IL_002b:  callvirt   ""string object.ToString()""
  IL_0030:  call       ""void System.Console.Write(string)""
  IL_0035:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
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

            var comp1 = CreateCompilation(text1, assemblyName: "A");
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

            var comp = CompileAndVerify(text, new[] { ref1 }, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"hello2");

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

            comp = CompileAndVerify(text, new[] { ref1 }, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Passes, expectedOutput: @"hello2");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (S1<string> V_0)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  dup
  IL_0006:  ldfld      ""S1<string> Program.f""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldstr      ""hello""
  IL_0013:  call       ""string S1<string>.M1(string)""
  IL_0018:  call       ""void System.Console.Write(string)""
  IL_001d:  ldfld      ""S1<string> Program.f""
  IL_0022:  stloc.0
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. ""S1<string>""
  IL_002b:  callvirt   ""string object.ToString()""
  IL_0030:  call       ""void System.Console.Write(string)""
  IL_0035:  ret
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"12");

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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"12");

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
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (Program.S1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  dup
  IL_0003:  initobj    ""Program.S1""
  IL_0009:  call       ""void Program.S1.Test()""
  IL_000e:  ret
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"Program+S1Program+S1");

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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"4242");

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

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (27,13): error CS1604: Cannot assign to 'this' because it is read-only
                //             this = default; // error
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(27, 13),
                // (32,26): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //             TakesRef(ref this); // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(32, 26),
                // (37,26): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //             TakesRef(ref this.x); // error
                Diagnostic(ErrorCode.ERR_RefReadonly, "this.x").WithLocation(37, 26)
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

            var comp = CreateCompilation(text);
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

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular);

            // S1
            NamedTypeSymbol namedType = comp.GetTypeByMetadataName("Program+S1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

            void validate(ModuleSymbol module)
            {
                var test = module.ContainingAssembly.GetTypeByMetadataName("Program+S1");

                var peModule = (PEModuleSymbol)module;
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PENamedTypeSymbol)test).Handle));
                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute, Accessibility.Internal);
            }
            CompileAndVerify(comp, symbolValidator: validate);

            // S1<T>
            namedType = comp.GetTypeByMetadataName("Program+S1`1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // T
            TypeSymbol type = namedType.TypeParameters[0];
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S1<object>
            namedType = namedType.Construct(comp.ObjectType);
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

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

            // S1 from image
            var clientComp = CreateCompilation("", references: new[] { comp.EmitToImageReference() });
            NamedTypeSymbol s1 = clientComp.GetTypeByMetadataName("Program+S1");
            Assert.True(s1.IsReadOnly);
            Assert.Empty(s1.GetAttributes());
            Assert.Equal(RefKind.Out, s1.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, s1.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, s1.GetMethod("ToString").ThisParameter.RefKind);
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
            var comp1 = CreateCompilation(text1, assemblyName: "A");
            var ref1 = comp1.EmitToImageReference();

            var comp = CreateCompilation("//NO CODE HERE", new[] { ref1 }, parseOptions: TestOptions.Regular);

            // S1
            NamedTypeSymbol namedType = comp.GetTypeByMetadataName("Program+S1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S1<T>
            namedType = comp.GetTypeByMetadataName("Program+S1`1");
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // T
            TypeSymbol type = namedType.TypeParameters[0];
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

            // S1<object>
            namedType = namedType.Construct(comp.ObjectType);
            Assert.True(namedType.IsReadOnly);
            Assert.Equal(RefKind.Out, namedType.Constructors[0].ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("M1").ThisParameter.RefKind);
            Assert.Equal(RefKind.In, namedType.GetMethod("ToString").ThisParameter.RefKind);

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
        public void CorrectOverloadOfStackAllocSpanChosen()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    unsafe public static void Main()
    {
        bool condition = false;

        var span1 = condition ? stackalloc int[1] : new Span<int>(null, 2);
        Console.Write(span1.Length);

        var span2 = condition ? stackalloc int[1] : stackalloc int[4];
        Console.Write(span2.Length);
    }
}", TestOptions.UnsafeReleaseExe);

            CompileAndVerify(comp, expectedOutput: "24", verify: Verification.Fails);
        }

        [Fact]
        public void StackAllocExpressionIL()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public static void Main()
    {
        Span<int> x = stackalloc int[10];
        Console.WriteLine(x.Length);
    }
}", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "10", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.Span<int> V_0) //x
  IL_0000:  ldc.i4.s   40
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  ldc.i4.s   10
  IL_0007:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int System.Span<int>.Length.get""
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ret
}");
        }

        [Fact]
        public void StackAllocSpanLengthNotEvaluatedTwice()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    private static int length = 0;

    private static int GetLength()
    {
        return ++length;
    }

    public static void Main()
    {       
        for (int i = 0; i < 5; i++)
        {
            Span<int> x = stackalloc int[GetLength()];
            Console.Write(x.Length);
        }
    }
}", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "12345", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (int V_0, //i
                System.Span<int> V_1, //x
                int V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0027
  IL_0004:  call       ""int Test.GetLength()""
  IL_0009:  stloc.2
  IL_000a:  ldloc.2
  IL_000b:  conv.u
  IL_000c:  ldc.i4.4
  IL_000d:  mul.ovf.un
  IL_000e:  localloc
  IL_0010:  ldloc.2
  IL_0011:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""int System.Span<int>.Length.get""
  IL_001e:  call       ""void System.Console.Write(int)""
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.0
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.5
  IL_0029:  blt.s      IL_0004
  IL_002b:  ret
}");
        }

        [Fact]
        public void StackAllocSpanLengthConstantFolding()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public static void Main()
    {
        const int a = 5, b = 6;
        Span<int> x = stackalloc int[a * b];
        Console.Write(x.Length);
    }
}", TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "30", verify: Verification.Fails).VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.Span<int> V_0) //x
  IL_0000:  ldc.i4.s   120
  IL_0002:  conv.u
  IL_0003:  localloc
  IL_0005:  ldc.i4.s   30
  IL_0007:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int System.Span<int>.Length.get""
  IL_0014:  call       ""void System.Console.Write(int)""
  IL_0019:  ret
}");
        }

        [Fact]
        public void StackAllocSpanLengthOverflow()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    static void M()
    {
        Span<int> x = stackalloc int[int.MaxValue];
    }

    public static void Main()
    {
        try
        {
            M();
        }
        catch (OverflowException)
        {
            Console.WriteLine(""overflow"");
        }
    }
}", TestOptions.ReleaseExe);

            var expectedIL = @"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldc.i4     0x7fffffff
  IL_0005:  conv.u
  IL_0006:  ldc.i4.4
  IL_0007:  mul.ovf.un
  IL_0008:  localloc
  IL_000a:  ldc.i4     0x7fffffff
  IL_000f:  newobj     ""System.Span<int>..ctor(void*, int)""
  IL_0014:  pop
  IL_0015:  ret
}";

            var isx86 = (IntPtr.Size == 4);
            if (isx86)
            {
                CompileAndVerify(comp, expectedOutput: "overflow", verify: Verification.Fails).VerifyIL("Test.M", expectedIL);
            }
            else
            {
                // On 64bit the native int does not overflow, so we get StackOverflow instead
                // therefore we will just check the IL
                CompileAndVerify(comp, verify: Verification.Fails).VerifyIL("Test.M", expectedIL);
            }
        }

        [Fact]
        public void ImplicitCastOperatorOnStackAllocIsLoweredCorrectly()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public static void Main()
    {
        Test obj1 = stackalloc int[10];
        Console.Write(""|"");
        Test obj2 = stackalloc double[10];
    }
    
    public static implicit operator Test(Span<int> value) 
    {
        Console.Write(""SpanOpCalled"");
        return default(Test);
    }
    
    public static implicit operator Test(double* value) 
    {
        Console.Write(""PointerOpCalled"");
        return default(Test);
    }
}", TestOptions.UnsafeReleaseExe);

            CompileAndVerify(comp, expectedOutput: "SpanOpCalled|PointerOpCalled", verify: Verification.Fails);
        }

        [Fact]
        public void ExplicitCastOperatorOnStackAllocIsLoweredCorrectly()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public static void Main()
    {
        Test obj1 = (Test)stackalloc int[10];
    }
    
    public static explicit operator Test(Span<int> value) 
    {
        Console.Write(""SpanOpCalled"");
        return default(Test);
    }
}", TestOptions.UnsafeReleaseExe);

            CompileAndVerify(comp, expectedOutput: "SpanOpCalled", verify: Verification.Fails);
        }

        [Fact]
        public void ReadOnlyMembers_Metadata()
        {
            var csharp = @"
public struct S
{
    public void M1() {}
    public readonly void M2() {}

    public int P1 { get; set; }
    public readonly int P2 => 42;
    public int P3 { readonly get => 123; set {} }
    public int P4 { get => 123; readonly set {} }
    public static int P5 { get; set; }
}
";
            CompileAndVerify(csharp, symbolValidator: validate);

            void validate(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("S");
                var m1 = type.GetMethod("M1");
                var m2 = type.GetMethod("M2");

                var p1 = type.GetProperty("P1");
                var p2 = type.GetProperty("P2");
                var p3 = type.GetProperty("P3");
                var p4 = type.GetProperty("P4");
                var p5 = type.GetProperty("P5");

                var peModule = (PEModuleSymbol)module;

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m1).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m1).Signature.ReturnParam.Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m2).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m2).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p1).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.GetMethod).Signature.ReturnParam.Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.SetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.SetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p2).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p2.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p2.GetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p3).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.GetMethod).Signature.ReturnParam.Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.SetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.SetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p4).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.GetMethod).Signature.ReturnParam.Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.SetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.SetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p5).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.GetMethod).Signature.ReturnParam.Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.SetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.SetMethod).Signature.ReturnParam.Handle));

                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute, Accessibility.Internal);
            }
        }

        [Fact]
        public void ReadOnlyMembers_RefReturn_Metadata()
        {
            var csharp = @"
public struct S
{
    static int i;

    public ref int M1() => ref i;
    public readonly ref int M2() => ref i;
    public ref readonly int M3() => ref i;
    public readonly ref readonly int M4() => ref i;

    public ref int P1 { get => ref i; }
    public readonly ref int P2 { get => ref i; }
    public ref readonly int P3 { get => ref i; }
    public readonly ref readonly int P4 { get => ref i; }
}
";
            CompileAndVerify(csharp, symbolValidator: validate);

            void validate(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("S");
                var m1 = type.GetMethod("M1");
                var m2 = type.GetMethod("M2");
                var m3 = type.GetMethod("M3");
                var m4 = type.GetMethod("M4");

                var p1 = type.GetProperty("P1");
                var p2 = type.GetProperty("P2");
                var p3 = type.GetProperty("P3");
                var p4 = type.GetProperty("P4");

                var peModule = (PEModuleSymbol)module;

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m1).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m1).Signature.ReturnParam.Handle));

                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m2).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m2).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m3).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m3).Signature.ReturnParam.Handle));

                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m4).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m4).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p1).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.GetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p2).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p2.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p2.GetMethod).Signature.ReturnParam.Handle));

                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p3).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.GetMethod).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.GetMethod).Signature.ReturnParam.Handle));

                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p4).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.GetMethod).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.GetMethod).Signature.ReturnParam.Handle));

                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute, Accessibility.Internal);
            }
        }

        [Fact]
        public void ReadOnlyStruct_ReadOnlyMembers_Metadata()
        {
            var csharp = @"
// note that both the type and member declarations are marked 'readonly'
public readonly struct S
{
    public void M1() {}
    public readonly void M2() {}

    public int P1 { get; }
    public readonly int P2 => 42;
    public int P3 { readonly get => 123; set {} }
    public int P4 { get => 123; readonly set {} }
    public static int P5 { get; set; }
}
";
            CompileAndVerify(csharp, symbolValidator: validate);

            void validate(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("S");
                var m1 = type.GetMethod("M1");
                var m2 = type.GetMethod("M2");

                var p1 = type.GetProperty("P1");
                var p2 = type.GetProperty("P2");
                var p3 = type.GetProperty("P3");
                var p4 = type.GetProperty("P4");
                var p5 = type.GetProperty("P5");

                var peModule = (PEModuleSymbol)module;

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m1).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m1).Signature.ReturnParam.Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m2).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)m2).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p1).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p1.GetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p2).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p2.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p2.GetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p3).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.GetMethod).Signature.ReturnParam.Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.SetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p3.SetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p4).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.GetMethod).Signature.ReturnParam.Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.SetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p4.SetMethod).Signature.ReturnParam.Handle));

                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEPropertySymbol)p5).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.GetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.GetMethod).Signature.ReturnParam.Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.SetMethod).Handle));
                Assert.False(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)p5.SetMethod).Signature.ReturnParam.Handle));

                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute, Accessibility.Internal);
            }
        }

        [Fact]
        public void ReadOnlyMembers_MetadataRoundTrip()
        {
            var external = @"
using System;

public struct S1
{
    public void M1() {}
    public readonly void M2() {}

    public int P1 { get; set; }
    public readonly int P2 => 42;
    public int P3 { readonly get => 123; set {} }
    public int P4 { get => 123; readonly set {} }
    public static int P5 { get; set; }
    public readonly event Action<EventArgs> E { add {} remove {} }
}

public readonly struct S2
{
    public void M1() {}
    public int P1 { get; }
    public int P2 => 42;
    public int P3 { set {} }
    public static int P4 { get; set; }
    public event Action<EventArgs> E { add {} remove {} }
}
";
            var externalComp = CreateCompilation(external);
            externalComp.VerifyDiagnostics();
            verify(externalComp);

            var comp = CreateCompilation("", references: new[] { externalComp.EmitToImageReference() });
            verify(comp);

            var comp2 = CreateCompilation("", references: new[] { externalComp.ToMetadataReference() });
            verify(comp2);

            void verify(CSharpCompilation comp)
            {
                var s1 = comp.GetMember<NamedTypeSymbol>("S1");

                verifyReadOnly(s1.GetMethod("M1"), false, RefKind.Ref);
                verifyReadOnly(s1.GetMethod("M2"), true, RefKind.RefReadOnly);

                verifyReadOnly(s1.GetProperty("P1").GetMethod, true, RefKind.RefReadOnly);
                verifyReadOnly(s1.GetProperty("P1").SetMethod, false, RefKind.Ref);

                verifyReadOnly(s1.GetProperty("P2").GetMethod, true, RefKind.RefReadOnly);

                verifyReadOnly(s1.GetProperty("P3").GetMethod, true, RefKind.RefReadOnly);
                verifyReadOnly(s1.GetProperty("P3").SetMethod, false, RefKind.Ref);

                verifyReadOnly(s1.GetProperty("P4").GetMethod, false, RefKind.Ref);
                verifyReadOnly(s1.GetProperty("P4").SetMethod, true, RefKind.RefReadOnly);

                verifyReadOnly(s1.GetProperty("P5").GetMethod, false, null);
                verifyReadOnly(s1.GetProperty("P5").SetMethod, false, null);

                verifyReadOnly(s1.GetEvent("E").AddMethod, true, RefKind.RefReadOnly);
                verifyReadOnly(s1.GetEvent("E").RemoveMethod, true, RefKind.RefReadOnly);

                var s2 = comp.GetMember<NamedTypeSymbol>("S2");

                verifyReadOnly(s2.GetMethod("M1"), true, RefKind.RefReadOnly);

                verifyReadOnly(s2.GetProperty("P1").GetMethod, true, RefKind.RefReadOnly);
                verifyReadOnly(s2.GetProperty("P2").GetMethod, true, RefKind.RefReadOnly);
                verifyReadOnly(s2.GetProperty("P3").SetMethod, true, RefKind.RefReadOnly);

                verifyReadOnly(s2.GetProperty("P4").GetMethod, false, null);
                verifyReadOnly(s2.GetProperty("P4").SetMethod, false, null);

                verifyReadOnly(s2.GetEvent("E").AddMethod, true, RefKind.RefReadOnly);
                verifyReadOnly(s2.GetEvent("E").RemoveMethod, true, RefKind.RefReadOnly);

                void verifyReadOnly(MethodSymbol method, bool isReadOnly, RefKind? refKind)
                {
                    Assert.Equal(isReadOnly, method.IsEffectivelyReadOnly);
                    Assert.Equal(refKind, method.ThisParameter?.RefKind);
                }
            }
        }

        [Fact]
        public void StaticReadOnlyMethod_FromMetadata()
        {
            var il = @"
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute
    extends [mscorlib]System.Attribute
{
    // Methods
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    } // end of method IsReadOnlyAttribute::.ctor

} // end of class System.Runtime.CompilerServices.IsReadOnlyAttribute

.class public sequential ansi sealed beforefieldinit S
	extends [mscorlib]System.ValueType
{
	.pack 0
	.size 1
	// Methods
	.method public hidebysig
		instance void M1 () cil managed
	{
		.custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2058
		// Code size 1 (0x1)
		.maxstack 8
		IL_0000: ret
	} // end of method S::M1

	// Methods
	.method public hidebysig static
		void M2 () cil managed
	{
		.custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2058
		// Code size 1 (0x1)
		.maxstack 8
		IL_0000: ret
	} // end of method S::M2
} // end of class S
";
            var ilRef = CompileIL(il);

            var comp = CreateCompilation("", references: new[] { ilRef });
            var s = comp.GetMember<NamedTypeSymbol>("S");

            var m1 = s.GetMethod("M1");
            Assert.True(m1.IsDeclaredReadOnly);
            Assert.True(m1.IsEffectivelyReadOnly);

            // even though the IsReadOnlyAttribute is in metadata,
            // we ruled out the possibility of the method being readonly because it's static
            var m2 = s.GetMethod("M2");
            Assert.False(m2.IsDeclaredReadOnly);
            Assert.False(m2.IsEffectivelyReadOnly);
        }

        [Fact]
        public void ReadOnlyMethod_CallNormalMethod()
        {
            var csharp = @"
public struct S
{
    public int i;

    public readonly void M1()
    {
        // should create local copy
        M2();
        System.Console.Write(i);

        // explicit local copy, no warning
        var copy = this;
        copy.M2();
        System.Console.Write(copy.i);
    }

    void M2()
    {
        i = 23;
    }

    static void Main()
    {
        var s = new S { i = 1 };
        s.M1();
    }
}
";

            var verifier = CompileAndVerify(csharp, expectedOutput: "123");

            verifier.VerifyDiagnostics(
                // (9,9): warning CS8655: Call to non-readonly member 'S.M2()' from a 'readonly' member results in an implicit copy of 'this'.
                //         M2();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "M2").WithArguments("S.M2()", "this").WithLocation(9, 9));

            verifier.VerifyIL("S.M1", @"
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (S V_0, //copy
                S V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""void S.M2()""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int S.i""
  IL_0014:  call       ""void System.Console.Write(int)""
  IL_0019:  ldarg.0
  IL_001a:  ldobj      ""S""
  IL_001f:  stloc.0
  IL_0020:  ldloca.s   V_0
  IL_0022:  call       ""void S.M2()""
  IL_0027:  ldloc.0
  IL_0028:  ldfld      ""int S.i""
  IL_002d:  call       ""void System.Console.Write(int)""
  IL_0032:  ret
}");
        }

        [Fact]
        public void InMethod_CallNormalMethod()
        {
            var csharp = @"
public struct S
{
    public int i;

    public static void M1(in S s)
    {
        // should create local copy
        s.M2();
        System.Console.Write(s.i);

        // explicit local copy, no warning
        var copy = s;
        copy.M2();
        System.Console.Write(copy.i);
    }

    void M2()
    {
        i = 23;
    }

    static void Main()
    {
        var s = new S { i = 1 };
        M1(in s);
    }
}
";

            var verifier = CompileAndVerify(csharp, expectedOutput: "123");
            // should warn about calling s.M2 in warning wave (see https://github.com/dotnet/roslyn/issues/33968)
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M1", @"
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (S V_0, //copy
                S V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""void S.M2()""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int S.i""
  IL_0014:  call       ""void System.Console.Write(int)""
  IL_0019:  ldarg.0
  IL_001a:  ldobj      ""S""
  IL_001f:  stloc.0
  IL_0020:  ldloca.s   V_0
  IL_0022:  call       ""void S.M2()""
  IL_0027:  ldloc.0
  IL_0028:  ldfld      ""int S.i""
  IL_002d:  call       ""void System.Console.Write(int)""
  IL_0032:  ret
}");
        }

        [Fact]
        public void InMethod_CallMethodFromMetadata()
        {
            var external = @"
public struct S
{
    public int i;

    public readonly void M1() {}

    public void M2()
    {
        i = 23;
    }
}
";
            var image = CreateCompilation(external).EmitToImageReference();

            var csharp = @"
public static class C
{
    public static void M1(in S s)
    {
        // should not copy, no warning
        s.M1();
        System.Console.Write(s.i);

        // should create local copy, warn in warning wave
        s.M2();
        System.Console.Write(s.i);

        // explicit local copy, no warning
        var copy = s;
        copy.M2();
        System.Console.Write(copy.i);
    }

    static void Main()
    {
        var s = new S { i = 1 };
        M1(in s);
    }
}
";

            var verifier = CompileAndVerify(csharp, references: new[] { image }, expectedOutput: "1123");
            // should warn about calling s.M2 in warning wave (see https://github.com/dotnet/roslyn/issues/33968)
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M1", @"
{
  // Code size       68 (0x44)
  .maxstack  1
  .locals init (S V_0, //copy
                S V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""readonly void S.M1()""
  IL_0006:  ldarg.0
  IL_0007:  ldfld      ""int S.i""
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  ldarg.0
  IL_0012:  ldobj      ""S""
  IL_0017:  stloc.1
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""void S.M2()""
  IL_001f:  ldarg.0
  IL_0020:  ldfld      ""int S.i""
  IL_0025:  call       ""void System.Console.Write(int)""
  IL_002a:  ldarg.0
  IL_002b:  ldobj      ""S""
  IL_0030:  stloc.0
  IL_0031:  ldloca.s   V_0
  IL_0033:  call       ""void S.M2()""
  IL_0038:  ldloc.0
  IL_0039:  ldfld      ""int S.i""
  IL_003e:  call       ""void System.Console.Write(int)""
  IL_0043:  ret
}");
        }

        [Fact]
        public void InMethod_CallGetAccessorFromMetadata()
        {
            var external = @"
public struct S
{
    public int i;

    public readonly int P1 => 42;

    public int P2 => i = 23;
}
";
            var image = CreateCompilation(external).EmitToImageReference();

            var csharp = @"
public static class C
{
    public static void M1(in S s)
    {
        // should not copy, no warning
        _ = s.P1;
        System.Console.Write(s.i);

        // should create local copy, warn in warning wave
        _ = s.P2;
        System.Console.Write(s.i);

        // explicit local copy, no warning
        var copy = s;
        _ = copy.P2;
        System.Console.Write(copy.i);
    }

    static void Main()
    {
        var s = new S { i = 1 };
        M1(in s);
    }
}
";

            var verifier = CompileAndVerify(csharp, references: new[] { image }, expectedOutput: "1123");
            // should warn about calling s.M2 in warning wave (see https://github.com/dotnet/roslyn/issues/33968)
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M1", @"
{
  // Code size       71 (0x47)
  .maxstack  1
  .locals init (S V_0, //copy
                S V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""readonly int S.P1.get""
  IL_0006:  pop
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""int S.i""
  IL_000d:  call       ""void System.Console.Write(int)""
  IL_0012:  ldarg.0
  IL_0013:  ldobj      ""S""
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_1
  IL_001b:  call       ""int S.P2.get""
  IL_0020:  pop
  IL_0021:  ldarg.0
  IL_0022:  ldfld      ""int S.i""
  IL_0027:  call       ""void System.Console.Write(int)""
  IL_002c:  ldarg.0
  IL_002d:  ldobj      ""S""
  IL_0032:  stloc.0
  IL_0033:  ldloca.s   V_0
  IL_0035:  call       ""int S.P2.get""
  IL_003a:  pop
  IL_003b:  ldloc.0
  IL_003c:  ldfld      ""int S.i""
  IL_0041:  call       ""void System.Console.Write(int)""
  IL_0046:  ret
}");
        }

        [Fact]
        public void ReadOnlyMethod_CallReadOnlyMethod()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly int M1() => M2() + 1;
    public readonly int M2() => i;
}
";
            var comp = CompileAndVerify(csharp);
            comp.VerifyIL("S.M1", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""readonly int S.M2()""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyGetAccessor_CallReadOnlyGetAccessor()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly int P1 { get => P2 + 1; }
    public readonly int P2 { get => i; }
}
";
            var comp = CompileAndVerify(csharp);
            comp.VerifyIL("S.P1.get", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""readonly int S.P2.get""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyGetAccessor_CallAutoGetAccessor()
        {
            var csharp = @"
public struct S
{
    public readonly int P1 { get => P2 + 1; }
    public int P2 { get; }
}
";
            var comp = CompileAndVerify(csharp);
            comp.VerifyIL("S.P1.get", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""readonly int S.P2.get""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void InParam_CallReadOnlyMethod()
        {
            var csharp = @"
public struct S
{
    public int i;
    public static int M1(in S s) => s.M2() + 1;
    public readonly int M2() => i;
}
";
            var comp = CompileAndVerify(csharp);
            comp.VerifyIL("S.M1", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""readonly int S.M2()""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_CallReadOnlyMethodOnField()
        {
            var csharp = @"
public struct S1
{
    public readonly void M1() {}
}

public struct S2
{
    S1 s1;

    public readonly void M2()
    {
        s1.M1();
    }
}
";
            var comp = CompileAndVerify(csharp);

            comp.VerifyIL("S2.M2", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""S1 S2.s1""
  IL_0006:  call       ""readonly void S1.M1()""
  IL_000b:  ret
}");

            comp.VerifyDiagnostics(
                // (9,8): warning CS0649: Field 'S2.s1' is never assigned to, and will always have its default value
                //     S1 s1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "s1").WithArguments("S2.s1", "").WithLocation(9, 8)
);
        }

        [Fact]
        public void ReadOnlyMethod_CallNormalMethodOnField()
        {
            var csharp = @"
public struct S1
{
    public void M1() {}
}

public struct S2
{
    S1 s1;

    public readonly void M2()
    {
        s1.M1();
    }
}
";
            var comp = CompileAndVerify(csharp);

            comp.VerifyIL("S2.M2", @"
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""S1 S2.s1""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""void S1.M1()""
  IL_000e:  ret
}");

            // should warn about calling s2.M2 in warning wave (see https://github.com/dotnet/roslyn/issues/33968)
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_CallBaseMethod()
        {
            var csharp = @"
public struct S
{
    readonly void M()
    {
        // no warnings for calls to base members
        GetType();
        ToString();
        GetHashCode();
        Equals(null);
    }
}
";
            var comp = CompileAndVerify(csharp);
            comp.VerifyDiagnostics();

            // ToString/GetHashCode/Equals should pass the address of 'this' (not a temp). GetType should box 'this'.
            comp.VerifyIL("S.M", @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  box        ""S""
  IL_000b:  call       ""System.Type object.GetType()""
  IL_0010:  pop
  IL_0011:  ldarg.0
  IL_0012:  constrained. ""S""
  IL_0018:  callvirt   ""string object.ToString()""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  constrained. ""S""
  IL_0025:  callvirt   ""int object.GetHashCode()""
  IL_002a:  pop
  IL_002b:  ldarg.0
  IL_002c:  ldnull
  IL_002d:  constrained. ""S""
  IL_0033:  callvirt   ""bool object.Equals(object)""
  IL_0038:  pop
  IL_0039:  ret
}");
        }

        [Fact]
        public void ReadOnlyMethod_OverrideBaseMethod()
        {
            var csharp = @"
public struct S
{
    // note: GetType can't be overridden
    public override string ToString() => throw null;
    public override int GetHashCode() => throw null;
    public override bool Equals(object o) => throw null;

    readonly void M()
    {
        // should warn--non-readonly invocation
        ToString();
        GetHashCode();
        Equals(null);

        // ok
        base.ToString();
        base.GetHashCode();
        base.Equals(null);
    }
}
";
            var verifier = CompileAndVerify(csharp);

            verifier.VerifyDiagnostics(
                // (12,9): warning CS8655: Call to non-readonly member 'S.ToString()' from a 'readonly' member results in an implicit copy of 'this'.
                //         ToString();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "ToString").WithArguments("S.ToString()", "this").WithLocation(12, 9),
                // (13,9): warning CS8655: Call to non-readonly member 'S.GetHashCode()' from a 'readonly' member results in an implicit copy of 'this'.
                //         GetHashCode();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "GetHashCode").WithArguments("S.GetHashCode()", "this").WithLocation(13, 9),
                // (14,9): warning CS8655: Call to non-readonly member 'S.Equals(object)' from a 'readonly' member results in an implicit copy of 'this'.
                //         Equals(null);
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "Equals").WithArguments("S.Equals(object)", "this").WithLocation(14, 9));

            // Verify that calls to non-readonly overrides pass the address of a temp, not the address of 'this'
            verifier.VerifyIL("S.M", @"
{
  // Code size      117 (0x75)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  constrained. ""S""
  IL_000f:  callvirt   ""string object.ToString()""
  IL_0014:  pop
  IL_0015:  ldarg.0
  IL_0016:  ldobj      ""S""
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  constrained. ""S""
  IL_0024:  callvirt   ""int object.GetHashCode()""
  IL_0029:  pop
  IL_002a:  ldarg.0
  IL_002b:  ldobj      ""S""
  IL_0030:  stloc.0
  IL_0031:  ldloca.s   V_0
  IL_0033:  ldnull
  IL_0034:  constrained. ""S""
  IL_003a:  callvirt   ""bool object.Equals(object)""
  IL_003f:  pop
  IL_0040:  ldarg.0
  IL_0041:  ldobj      ""S""
  IL_0046:  box        ""S""
  IL_004b:  call       ""string System.ValueType.ToString()""
  IL_0050:  pop
  IL_0051:  ldarg.0
  IL_0052:  ldobj      ""S""
  IL_0057:  box        ""S""
  IL_005c:  call       ""int System.ValueType.GetHashCode()""
  IL_0061:  pop
  IL_0062:  ldarg.0
  IL_0063:  ldobj      ""S""
  IL_0068:  box        ""S""
  IL_006d:  ldnull
  IL_006e:  call       ""bool System.ValueType.Equals(object)""
  IL_0073:  pop
  IL_0074:  ret
}");
        }

        [Fact]
        public void ReadOnlyMethod_ReadOnlyOverrideBaseMethod()
        {
            var csharp = @"
public struct S
{
    // note: GetType can't be overridden
    public readonly override string ToString() => throw null;
    public readonly override int GetHashCode() => throw null;
    public readonly override bool Equals(object o) => throw null;

    readonly void M()
    {
        // no warnings
        ToString();
        GetHashCode();
        Equals(null);

        base.ToString();
        base.GetHashCode();
        base.Equals(null);
    }
}
";
            var verifier = CompileAndVerify(csharp);
            verifier.VerifyDiagnostics();

            // Verify that calls to readonly override members pass the address of 'this' (not a temp)
            verifier.VerifyIL("S.M", @"
{
  // Code size       93 (0x5d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  constrained. ""S""
  IL_0007:  callvirt   ""string object.ToString()""
  IL_000c:  pop
  IL_000d:  ldarg.0
  IL_000e:  constrained. ""S""
  IL_0014:  callvirt   ""int object.GetHashCode()""
  IL_0019:  pop
  IL_001a:  ldarg.0
  IL_001b:  ldnull
  IL_001c:  constrained. ""S""
  IL_0022:  callvirt   ""bool object.Equals(object)""
  IL_0027:  pop
  IL_0028:  ldarg.0
  IL_0029:  ldobj      ""S""
  IL_002e:  box        ""S""
  IL_0033:  call       ""string System.ValueType.ToString()""
  IL_0038:  pop
  IL_0039:  ldarg.0
  IL_003a:  ldobj      ""S""
  IL_003f:  box        ""S""
  IL_0044:  call       ""int System.ValueType.GetHashCode()""
  IL_0049:  pop
  IL_004a:  ldarg.0
  IL_004b:  ldobj      ""S""
  IL_0050:  box        ""S""
  IL_0055:  ldnull
  IL_0056:  call       ""bool System.ValueType.Equals(object)""
  IL_005b:  pop
  IL_005c:  ret
}");
        }

        [Fact]
        public void ReadOnlyMethod_NewBaseMethod()
        {
            var csharp = @"
public struct S
{
    public new System.Type GetType() => throw null;
    public new string ToString() => throw null;
    public new int GetHashCode() => throw null;
    public new bool Equals(object o) => throw null;

    readonly void M()
    {
        // should warn--non-readonly invocation
        GetType();
        ToString();
        GetHashCode();
        Equals(null);

        // ok
        base.GetType();
        base.ToString();
        base.GetHashCode();
        base.Equals(null);
    }
}
";
            var verifier = CompileAndVerify(csharp);

            verifier.VerifyDiagnostics(
                // (12,9): warning CS8655: Call to non-readonly member 'S.GetType()' from a 'readonly' member results in an implicit copy of 'this'.
                //         GetType();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "GetType").WithArguments("S.GetType()", "this").WithLocation(12, 9),
                // (13,9): warning CS8655: Call to non-readonly member 'S.ToString()' from a 'readonly' member results in an implicit copy of 'this'.
                //         ToString();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "ToString").WithArguments("S.ToString()", "this").WithLocation(13, 9),
                // (14,9): warning CS8655: Call to non-readonly member 'S.GetHashCode()' from a 'readonly' member results in an implicit copy of 'this'.
                //         GetHashCode();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "GetHashCode").WithArguments("S.GetHashCode()", "this").WithLocation(14, 9),
                // (15,9): warning CS8655: Call to non-readonly member 'S.Equals(object)' from a 'readonly' member results in an implicit copy of 'this'.
                //         Equals(null);
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "Equals").WithArguments("S.Equals(object)", "this").WithLocation(15, 9));

            // Verify that calls to new non-readonly members pass an address to a temp and that calls to base members use a box.
            verifier.VerifyIL("S.M", @"
{
  // Code size      131 (0x83)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""System.Type S.GetType()""
  IL_000e:  pop
  IL_000f:  ldarg.0
  IL_0010:  ldobj      ""S""
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""string S.ToString()""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldobj      ""S""
  IL_0024:  stloc.0
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""int S.GetHashCode()""
  IL_002c:  pop
  IL_002d:  ldarg.0
  IL_002e:  ldobj      ""S""
  IL_0033:  stloc.0
  IL_0034:  ldloca.s   V_0
  IL_0036:  ldnull
  IL_0037:  call       ""bool S.Equals(object)""
  IL_003c:  pop
  IL_003d:  ldarg.0
  IL_003e:  ldobj      ""S""
  IL_0043:  box        ""S""
  IL_0048:  call       ""System.Type object.GetType()""
  IL_004d:  pop
  IL_004e:  ldarg.0
  IL_004f:  ldobj      ""S""
  IL_0054:  box        ""S""
  IL_0059:  call       ""string System.ValueType.ToString()""
  IL_005e:  pop
  IL_005f:  ldarg.0
  IL_0060:  ldobj      ""S""
  IL_0065:  box        ""S""
  IL_006a:  call       ""int System.ValueType.GetHashCode()""
  IL_006f:  pop
  IL_0070:  ldarg.0
  IL_0071:  ldobj      ""S""
  IL_0076:  box        ""S""
  IL_007b:  ldnull
  IL_007c:  call       ""bool System.ValueType.Equals(object)""
  IL_0081:  pop
  IL_0082:  ret
}");
        }

        [Fact]
        public void ReadOnlyMethod_ReadOnlyNewBaseMethod()
        {
            var csharp = @"
public struct S
{
    public readonly new System.Type GetType() => throw null;
    public readonly new string ToString() => throw null;
    public readonly new int GetHashCode() => throw null;
    public readonly new bool Equals(object o) => throw null;

    readonly void M()
    {
        // no warnings
        GetType();
        ToString();
        GetHashCode();
        Equals(null);

        base.GetType();
        base.ToString();
        base.GetHashCode();
        base.Equals(null);
    }
}
";
            var verifier = CompileAndVerify(csharp);
            verifier.VerifyDiagnostics();

            // Verify that calls to readonly new members pass the address of 'this' (not a temp) and that calls to base members use a box.
            verifier.VerifyIL("S.M", @"
{
  // Code size       99 (0x63)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""readonly System.Type S.GetType()""
  IL_0006:  pop
  IL_0007:  ldarg.0
  IL_0008:  call       ""readonly string S.ToString()""
  IL_000d:  pop
  IL_000e:  ldarg.0
  IL_000f:  call       ""readonly int S.GetHashCode()""
  IL_0014:  pop
  IL_0015:  ldarg.0
  IL_0016:  ldnull
  IL_0017:  call       ""readonly bool S.Equals(object)""
  IL_001c:  pop
  IL_001d:  ldarg.0
  IL_001e:  ldobj      ""S""
  IL_0023:  box        ""S""
  IL_0028:  call       ""System.Type object.GetType()""
  IL_002d:  pop
  IL_002e:  ldarg.0
  IL_002f:  ldobj      ""S""
  IL_0034:  box        ""S""
  IL_0039:  call       ""string System.ValueType.ToString()""
  IL_003e:  pop
  IL_003f:  ldarg.0
  IL_0040:  ldobj      ""S""
  IL_0045:  box        ""S""
  IL_004a:  call       ""int System.ValueType.GetHashCode()""
  IL_004f:  pop
  IL_0050:  ldarg.0
  IL_0051:  ldobj      ""S""
  IL_0056:  box        ""S""
  IL_005b:  ldnull
  IL_005c:  call       ""bool System.ValueType.Equals(object)""
  IL_0061:  pop
  IL_0062:  ret
}");
        }

        [Fact]
        public void ReadOnlyMethod_FixedThis()
        {
            var csharp = @"
struct S
{
    int i;

    readonly unsafe void M()
    {
        fixed (S* sp = &this)
        {
			sp->i = 42;
        }
    }

    static void Main()
    {
        var s = new S();
        s.M();
        System.Console.Write(s.i);
    }
}
";
            CompileAndVerify(csharp, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: "42");
        }

        public static TheoryData<bool, CSharpParseOptions, Verification> ReadOnlyGetter_LangVersion_Data() =>
            new TheoryData<bool, CSharpParseOptions, Verification>
            {
                {  false, TestOptions.Regular7_3, Verification.Passes },
                {  true, null, Verification.Fails }
            };

        [Theory]
        [MemberData(nameof(ReadOnlyGetter_LangVersion_Data))]
        public void ReadOnlyGetter_LangVersion(bool isReadOnly, CSharpParseOptions parseOptions, Verification verify)
        {
            var csharp = @"
struct S
{
    public int P { get; }

    static readonly S Field = default;
    static void M()
    {
        _ = Field.P;
    }
}
";
            var verifier = CompileAndVerify(csharp, parseOptions: parseOptions, verify: verify);
            var type = verifier.Compilation.GetMember<NamedTypeSymbol>("S");
            Assert.Equal(isReadOnly, type.GetProperty("P").GetMethod.IsDeclaredReadOnly);
            Assert.Equal(isReadOnly, type.GetProperty("P").GetMethod.IsEffectivelyReadOnly);
        }

        [Fact]
        public void ReadOnlyEvent_Emit()
        {
            var csharp = @"
public struct S
{
    public readonly event System.Action E { add { } remove { } }
}
";
            CompileAndVerify(csharp, symbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol module)
            {
                var testStruct = module.ContainingAssembly.GetTypeByMetadataName("S");

                var peModule = (PEModuleSymbol)module;
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)testStruct.GetEvent("E").AddMethod).Handle));
                Assert.True(peModule.Module.HasIsReadOnlyAttribute(((PEMethodSymbol)testStruct.GetEvent("E").RemoveMethod).Handle));
                AssertDeclaresType(peModule, WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute, Accessibility.Internal);
            }
        }
    }
}
