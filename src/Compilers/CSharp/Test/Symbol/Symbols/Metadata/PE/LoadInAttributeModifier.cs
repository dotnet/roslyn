// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadInAttributeModifier : CSharpTestBase
    {
        [Fact]
        public void MissingInAttributeModreq_Delegates_Parameters()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance void Invoke ([in] int32& x) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke ([in] int32& x, class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance void EndInvoke ([in] int32& x, class [mscorlib]System.IAsyncResult result) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            CreateCompilation(@"
class Test
{
    void M(D d) => d(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,20): error CS0570: 'D.Invoke(in int)' is not supported by the language
                //     void M(D d) => d(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "d(0)").WithArguments("D.Invoke(in int)").WithLocation(4, 20));
        }

        [Fact]
        public void MissingInAttributeModreq_Delegates_Parameters_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance void Invoke (
        [in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x
    ) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (
        [in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x,
        class [mscorlib]System.AsyncCallback callback,
        object 'object'
    ) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance void EndInvoke (
        [in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x,
        class [mscorlib]System.IAsyncResult result
    ) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            CreateCompilation(@"
class Test
{
    void M(D d) => d(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,20): error CS0570: 'D.Invoke(in int)' is not supported by the language
                //     void M(D d) => d(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "d(0)").WithArguments("D.Invoke(in int)").WithLocation(4, 20));
        }

        [Fact]
        public void MissingInAttributeModreq_Delegates_ReturnTypes()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& Invoke () runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& EndInvoke (class [mscorlib]System.IAsyncResult result) runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            var c = CreateCompilation(@"
class Test
{
    ref readonly int M(D d) => ref d();
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,36): error CS0570: 'D.Invoke()' is not supported by the language
                //     ref readonly int M(D d) => ref d();
                Diagnostic(ErrorCode.ERR_BindToBogus, "d()").WithArguments("D.Invoke()").WithLocation(4, 36));
        }

        [Fact]
        public void MissingInAttributeModreq_Delegates_ReturnTypes_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) Invoke () runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) EndInvoke (class [mscorlib]System.IAsyncResult result) runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            CreateCompilation(@"
class Test
{
    ref readonly int M(D d) => ref d();
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,36): error CS0570: 'D.Invoke()' is not supported by the language
                //     ref readonly int M(D d) => ref d();
                Diagnostic(ErrorCode.ERR_BindToBogus, "d()").WithArguments("D.Invoke()").WithLocation(4, 36));
        }

        [Fact]
        public void MissingInAttributeModreq_Properties()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig specialname instance int32& get_X () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& X()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& RefTest::get_X()
    }
}");

            CreateCompilation(@"
class Test
{
    public ref readonly int M(RefTest obj) => ref obj.X;
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,55): error CS0570: 'RefTest.X' is not supported by the language
                //     public ref readonly int M(RefTest obj) => ref obj.X;
                Diagnostic(ErrorCode.ERR_BindToBogus, "X").WithArguments("RefTest.X").WithLocation(4, 55));
        }

        [Fact]
        public void MissingInAttributeModreq_Properties_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig specialname instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) get_X () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) X()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) RefTest::get_X()
    }
}");

            CreateCompilation(@"
class Test
{
    public ref readonly int M(RefTest obj) => ref obj.X;
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,55): error CS0570: 'RefTest.X' is not supported by the language
                //     public ref readonly int M(RefTest obj) => ref obj.X;
                Diagnostic(ErrorCode.ERR_BindToBogus, "X").WithArguments("RefTest.X").WithLocation(4, 55));
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Virtual()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance void M ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Virtual_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance void M ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Override()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance string M ([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Parent""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void[mscorlib] System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .method public hidebysig virtual instance string M([in] int32& x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Child""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
{
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        Console.WriteLine(new Parent().M(0));
        Console.WriteLine(new Child().M(0));
    }
}";

            // Child method is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent
Parent",
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetMethod("M");

                    Assert.True(method.IsOverride);
                    Assert.True(method.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, method.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Override_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance string M ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Parent""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void[mscorlib] System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .method public hidebysig virtual instance string M([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Child""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
{
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }
}");

            CreateCompilation(@"
using System;
class Test
{
    public static void Main()
    {
        Console.WriteLine(new Parent().M(0));
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,40): error CS0570: 'Parent.M(in int)' is not supported by the language
                //         Console.WriteLine(new Parent().M(0));
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("Parent.M(in int)").WithLocation(7, 40));

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        Console.WriteLine(new Child().M(0));
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "Child");
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Override_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance string M ([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Parent""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void[mscorlib] System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .method public hidebysig virtual instance string M([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Child""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
{
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        Console.WriteLine(new Parent().M(0));
        Console.WriteLine(new Child().M(0));
    }
}";

            // Child method is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent
Parent",
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetMethod("M");

                    Assert.True(method.IsOverride);
                    Assert.True(method.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, method.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Override_ModOpt_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance string M ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Parent""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void[mscorlib] System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .method public hidebysig virtual instance string M([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldstr ""Child""
        IL_0005: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
{
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }
}");

            CreateCompilation(@"
using System;
class Test
{
    public static void Main()
    {
        Console.WriteLine(new Parent().M(0));
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,40): error CS0570: 'Parent.M(in int)' is not supported by the language
                //         Console.WriteLine(new Parent().M(0));
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("Parent.M(in int)").WithLocation(7, 40));

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        Console.WriteLine(new Child().M(0));
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "Child");
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Abstract()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot abstract virtual instance void M ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void MissingInAttributeModreq_Method_Parameters_Abstract_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot abstract virtual instance void M ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Abstract()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance int32 get_Item ([in] int32&  x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method public hidebysig specialname newslot abstract virtual instance void set_Item ([in] int32& x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 RefTest::get_Item(int32&)
        .set instance void RefTest::set_Item(int32&, int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Abstract_Get()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance int32 get_Item ([in] int32&  x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 RefTest::get_Item(int32&)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        int x = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,17): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         int x = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 17));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Abstract_Set()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance void set_Item ([in] int32& x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .set instance void RefTest::set_Item(int32&, int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = 0;
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Abstract_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method public hidebysig specialname newslot abstract virtual instance void set_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 RefTest::get_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute))
        .set instance void RefTest::set_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Abstract_ModOpt_Get()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 RefTest::get_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute))
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        int x = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,17): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         int x = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 17));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Abstract_ModOpt_Set()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance void set_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void RefTest::set_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = 0;
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Virtual()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item ([in] int32& x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 RefTest::get_Item(int32&)
        .set instance void RefTest::set_Item(int32&, int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Virtual_Get()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 RefTest::get_Item(int32&)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        int x = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,17): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         int x = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 17));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Virtual_Set()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance void set_Item ([in] int32& x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .set instance void RefTest::set_Item(int32&, int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = 0;
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Virtual_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 RefTest::get_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute))
        .set instance void RefTest::set_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Virtual_ModOpt_Get()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 RefTest::get_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute))
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        int x = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,17): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         int x = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 17));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Virtual_ModOpt_Set()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance void set_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void RefTest::set_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = 0;
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Parent::get_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) )
        .set instance void Parent::set_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname virtual instance void set_Item([in] int32& x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 Child::get_Item(int32&)
        .set instance void Child::set_Item(int32&, int32)
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = parent[1];

        var child = new Child();
        child[0] = child[1];
    }
}";

            // Child property is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent Get
Parent Set
Parent Get
Parent Set",
                symbolValidator: module =>
                {
                    var indexer = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetIndexer<PEPropertySymbol>("Item");

                    Assert.True(indexer.IsOverride);
                    Assert.True(indexer.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, indexer.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 Parent::get_Item(int32&)
        .set instance void Parent::set_Item(int32&, int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname virtual instance void set_Item([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Child::get_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute))
        .set instance void Child::set_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = parent[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,9): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         parent[0] = parent[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[0]").WithArguments("Parent.this[in int]").WithLocation(7, 9),
                // (7,21): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         parent[0] = parent[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[1]").WithArguments("Parent.this[in int]").WithLocation(7, 21));

            var code = @"
class Test
{
    public static void Main()
    {
        var child = new Child();
        child[0] = child[1];
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Child Get
Child Set");
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_Get()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Parent::get_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) )
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 Child::get_Item(int32&)
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        int x = parent[1];

        var child = new Child();
        int y = child[1];
    }
}";

            // Child property is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent Get
Parent Get",
                symbolValidator: module =>
                {
                    var indexer = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetIndexer<PEPropertySymbol>("Item");

                    Assert.True(indexer.IsOverride);
                    Assert.True(indexer.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, indexer.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_Get_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 Parent::get_Item(int32&)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Child::get_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute))
    }
}");

            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        int x = parent[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,17): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         int x = parent[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[1]").WithArguments("Parent.this[in int]").WithLocation(7, 17));

            var code = @"
class Test
{
    public static void Main()
    {
        var child = new Child();
        int x = child[1];
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "Child Get");
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_Set()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void Parent::set_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname virtual instance void set_Item([in] int32& x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .set instance void Child::set_Item(int32&, int32)
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = 0;

        var child = new Child();
        child[0] = 0;
    }
}";

            // Child property is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent Set
Parent Set",
                symbolValidator: module =>
                {
                    var indexer = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetIndexer<PEPropertySymbol>("Item");

                    Assert.True(indexer.IsOverride);
                    Assert.True(indexer.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, indexer.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_Set_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .set instance void Parent::set_Item(int32&, int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname virtual instance void set_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void Child::set_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = 0;
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,9): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         parent[0] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[0]").WithArguments("Parent.this[in int]").WithLocation(7, 9));

            var code = @"
class Test
{
    public static void Main()
    {
        var child = new Child();
        child[0] = 0;
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "Child Set");
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Parent::get_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) )
        .set instance void Parent::set_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname virtual instance void set_Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Child::get_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) )
        .set instance void Child::set_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = parent[1];

        var child = new Child();
        child[0] = child[1];
    }
}";

            // Child property is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent Get
Parent Set
Parent Get
Parent Set",
                symbolValidator: module =>
                {
                    var indexer = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetIndexer<PEPropertySymbol>("Item");

                    Assert.True(indexer.IsOverride);
                    Assert.True(indexer.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, indexer.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_ModOpt_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Parent::get_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) )
        .set instance void Parent::set_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname virtual instance void set_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Child::get_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) )
        .set instance void Child::set_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}");

            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = parent[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,9): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         parent[0] = parent[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[0]").WithArguments("Parent.this[in int]").WithLocation(7, 9),
                // (7,21): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         parent[0] = parent[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[1]").WithArguments("Parent.this[in int]").WithLocation(7, 21));

            var code = @"
class Test
{
    public static void Main()
    {
        var child = new Child();
        child[0] = child[1];
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Child Get
Child Set");
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_ModOpt_Get()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Parent::get_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) )
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Child::get_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) )
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        int x = parent[1];

        var child = new Child();
        int y = child[1];
    }
}";

            // Child property is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent Get
Parent Get",
                symbolValidator: module =>
                {
                    var indexer = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetIndexer<PEPropertySymbol>("Item");

                    Assert.True(indexer.IsOverride);
                    Assert.True(indexer.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, indexer.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_ModOpt_Get_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Parent Get""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Parent::get_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) )
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance int32 get_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x) cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 1
        .locals init ([0] int32)

        IL_0000: nop
        IL_0001: ldstr ""Child Get""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010
        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 Child::get_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) )
    }
}");

            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        int x = parent[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,17): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         int x = parent[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[1]").WithArguments("Parent.this[in int]").WithLocation(7, 17));

            var code = @"
class Test
{
    public static void Main()
    {
        var child = new Child();
        int x = child[1];
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "Child Get");
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_ModOpt_Set()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void Parent::set_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance void set_Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void Child::set_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}");

            var code = @"
using System;
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = 0;

        var child = new Child();
        child[0] = 0;
    }
}";

            // Child property is bad, so it binds to the parent
            CompileAndVerify(code, references: new[] { reference }, expectedOutput: @"
Parent Set
Parent Set",
                symbolValidator: module =>
                {
                    var indexer = module.ContainingAssembly.BoundReferences()
                        .Single(assembly => !assembly.Identity.Equals(module.ContainingAssembly.CorLibrary.Identity))
                        .GetTypeByMetadataName("Child").GetIndexer<PEPropertySymbol>("Item");

                    Assert.True(indexer.IsOverride);
                    Assert.True(indexer.HasUseSiteError);
                    Assert.Equal((int)ErrorCode.ERR_BindToBogus, indexer.GetUseSiteDiagnostic().Code);
                });
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_Parameters_Override_ModOpt_Set_Inverse()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance void set_Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void[mscorlib] System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Parent Set""
        IL_0006: call void[mscorlib] System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]
        System.Object::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void Parent::set_Item(int32& modopt([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}

.class public auto ansi beforefieldinit Child extends Parent
{
    .custom instance void [mscorlib]
    System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .method public hidebysig specialname virtual instance void set_Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldstr ""Child Set""
        IL_0006: call void [mscorlib]
    System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void Parent::.ctor()
        IL_0006: nop
       IL_0007: ret
    }

    .property instance int32 Item([in] int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) x)
    {
        .set instance void Child::set_Item(int32& modreq([mscorlib] System.Runtime.InteropServices.InAttribute) , int32)
    }
}");

            CreateCompilation(@"
class Test
{
    public static void Main()
    {
        var parent = new Parent();
        parent[0] = 0;
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (7,9): error CS0570: 'Parent.this[in int]' is not supported by the language
                //         parent[0] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogus, "parent[0]").WithArguments("Parent.this[in int]").WithLocation(7, 9));

            var code = @"
class Test
{
    public static void Main()
    {
        var child = new Child();
        child[0] = 0;
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "Child Set");
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_ReturnType()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname instance int32& get_Item (int32 x) cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& Item(int32 x)
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& RefTest::get_Item(int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj[0];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,34): error CS0570: 'RefTest.this[int]' is not supported by the language
                //         ref readonly int x = ref obj[0];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[int]").WithLocation(6, 34));
        }

        [Fact]
        public void MissingInAttributeModreq_Indexers_ReturnType_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) get_Item (int32 x) cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) Item(int32 x)
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) RefTest::get_Item(int32)
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj[0];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,34): error CS0570: 'RefTest.this[int]' is not supported by the language
                //         ref readonly int x = ref obj[0];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[int]").WithLocation(6, 34));
        }

        [Fact]
        public void MissingInAttributeModreq_Methods_ReturnType()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig instance int32& M () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj.M();
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,38): error CS0570: 'RefTest.M()' is not supported by the language
                //         ref readonly int x = ref obj.M();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M()").WithLocation(6, 38));
        }

        [Fact]
        public void MissingInAttributeModreq_Methods_ReturnType_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) M () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj.M();
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,38): error CS0570: 'RefTest.M()' is not supported by the language
                //         ref readonly int x = ref obj.M();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M()").WithLocation(6, 38));
        }
    }
}
