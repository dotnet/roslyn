// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class InAttributeModifierTests : CSharpTestBase
    {
        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_Methods_Parameters()
        {
            var reference = CreateCompilation(@"
public class TestRef
{
    public void M(in int p)
    {
        System.Console.WriteLine(p);
    }
}");

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.M(value);
    }
}";

            var verifier = CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            verifyParameter(verifier.Compilation);

            verifier = CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
            verifyParameter(verifier.Compilation);

            void verifyParameter(Compilation comp)
            {
                var m = (IMethodSymbol)comp.GetMember("TestRef.M");
                Assert.Empty(m.Parameters[0].GetAttributes());
            }
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_Methods_ReturnTypes()
        {
            var reference = CreateCompilation(@"
public class TestRef
{
    private int value = 5;
    public ref readonly int M()
    {
        return ref value;
    }
}");

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.M());
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_Properties()
        {
            var reference = CreateCompilation(@"
public class TestRef
{
    private int value = 5;
    public ref readonly int P => ref value;
}");

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.P);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_Indexers_Parameters()
        {
            var reference = CreateCompilation(@"
public class TestRef
{
    public int this[in int p]
    {
        set { System.Console.WriteLine(p); }
    }
}");

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj[value] = 0;
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_Indexers_ReturnTypes()
        {
            var reference = CreateCompilation(@"
public class TestRef
{
    private int value = 5;
    public ref readonly int this[int p] => ref value;
}");

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj[0]);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_Delegates_Parameters()
        {
            var reference = CreateCompilation(@"
public delegate void D(in int p);
");

            var code = @"
public class Test
{
    public static void Main()
    {
        Process((in int p) => System.Console.WriteLine(p));
    }

    private static void Process(D func)
    {
        int value = 5;
        func(value);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_Delegates_ReturnTypes()
        {
            var reference = CreateCompilation(@"
public delegate ref readonly int D();
");

            var code = @"
public class Test
{
    private static int value = 5;

    public static void Main()
    {
        Process(() => ref value);
    }

    private static void Process(D func)
    {
        System.Console.WriteLine(func());
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_IL_Methods_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.M(value);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_IL_Methods_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot virtual 
          instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) 
          M() cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  1
    .locals init (int32& V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a
    IL_000a:  ldloc.0
    IL_000b:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.M());
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_IL_Properties()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot specialname virtual
          instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)
          get_P() cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 TestRef::'value'
    IL_0006:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }

  .property instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)
          P()
  {
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) TestRef::get_P()
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.P);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_IL_Indexers_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
  .method public hidebysig newslot specialname virtual
          instance void  set_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p, int32 'value') cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }

  .property instance int32 Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute))
  {
    .set instance void TestRef::set_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj[value] = 0;
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_IL_Indexers_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
  .field private int32 'value'
  .method public hidebysig newslot specialname virtual
          instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)
          get_Item(int32 p) cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  1
    .locals init (int32& V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a
    IL_000a:  ldloc.0
    IL_000b:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }

  .property instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) Item(int32)
  {
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) TestRef::get_Item(int32)
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj[0]);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_IL_Delegates_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance void Invoke(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p) runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p, class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance void  EndInvoke(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p, class [mscorlib]System.IAsyncResult result) runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        Process((in int p) => System.Console.WriteLine(p));
    }

    private static void Process(D func)
    {
        int value = 5;
        func(value);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsConsumedInRefCustomModifiersPosition_IL_Delegates_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) Invoke() runtime managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
  }

  .method public hidebysig newslot virtual
          instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    private static int value = 5;

    public static void Main()
    {
        Process(() => ref value);
    }

    private static void Process(D func)
    {
        System.Console.WriteLine(func());
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void InAttributeModReqIsNotAllowedInCustomModifiersPosition_Methods_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute)& x) cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.M(value);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.M(in ?)' is not supported by the language
                //         obj.M(value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M(in ?)").WithLocation(8, 13));
        }

        [Fact]
        public void InAttributeModReqIsNotAllowedInCustomModifiersPosition_Methods_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot virtual 
          instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) &
          M() cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  1
    .locals init (int32& V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a
    IL_000a:  ldloc.0
    IL_000b:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.M());
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS0570: 'TestRef.M()' is not supported by the language
                //         System.Console.WriteLine(obj.M());
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M()").WithLocation(7, 38));
        }

        [Fact]
        public void InAttributeModReqIsNotAllowedInCustomModifiersPosition_Properties()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot specialname virtual
          instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) &
          get_P() cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 TestRef::'value'
    IL_0006:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }

  .property instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) &
          P()
  {
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & TestRef::get_P()
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.P);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS1546: Property, indexer, or event 'TestRef.P' is not supported by the language; try directly calling accessor method 'TestRef.get_P()'
                //         System.Console.WriteLine(obj.P);
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "P").WithArguments("TestRef.P", "TestRef.get_P()").WithLocation(7, 38));

            code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.get_P());
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS0570: 'TestRef.get_P()' is not supported by the language
                //         System.Console.WriteLine(obj.get_P());
                Diagnostic(ErrorCode.ERR_BindToBogus, "get_P").WithArguments("TestRef.get_P()").WithLocation(7, 38));
        }

        [Fact]
        public void InAttributeModReqIsNotAllowedInCustomModifiersPosition_Indexers_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
  .method public hidebysig newslot specialname virtual
          instance void  set_Item(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & p, int32 'value') cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }

  .property instance int32 Item(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) &)
  {
    .set instance void TestRef::set_Item(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) &, int32)
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj[value] = 0;
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,9): error CS1546: Property, indexer, or event 'TestRef.this[in ?]' is not supported by the language; try directly calling accessor method 'TestRef.set_Item(in ?, ?)'
                //         obj[value] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "obj[value]").WithArguments("TestRef.this[in ?]", "TestRef.set_Item(in ?, ?)").WithLocation(8, 9));

            code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.set_Item(value, 0);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.set_Item(in ?, ?)' is not supported by the language
                //         obj.set_Item(value, 0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Item").WithArguments("TestRef.set_Item(in ?, ?)").WithLocation(8, 13));
        }

        [Fact]
        public void InAttributeModReqIsNotAllowedInCustomModifiersPosition_Indexers_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
  .field private int32 'value'
  .method public hidebysig newslot specialname virtual
          instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) &
          get_Item(int32 p) cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  1
    .locals init (int32& V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a
    IL_000a:  ldloc.0
    IL_000b:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }

  .property instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & Item(int32)
  {
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & TestRef::get_Item(int32)
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj[0]);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,34): error CS1546: Property, indexer, or event 'TestRef.this[?]' is not supported by the language; try directly calling accessor method 'TestRef.get_Item(?)'
                //         System.Console.WriteLine(obj[0]);
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "obj[0]").WithArguments("TestRef.this[?]", "TestRef.get_Item(?)").WithLocation(7, 34));

            code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.get_Item(0));
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS0570: 'TestRef.get_Item(?)' is not supported by the language
                //         System.Console.WriteLine(obj.get_Item(0));
                Diagnostic(ErrorCode.ERR_BindToBogus, "get_Item").WithArguments("TestRef.get_Item(?)").WithLocation(7, 38));
        }

        [Fact]
        public void InAttributeModReqIsNotAllowedInCustomModifiersPosition_Delegates_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance void Invoke(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & p) runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & p, class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance void  EndInvoke(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & p, class [mscorlib]System.IAsyncResult result) runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        Process((in int p) => System.Console.WriteLine(p));
    }

    private static void Process(D func)
    {
        int value = 5;
        func(value);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (6,17): error CS0570: 'D.Invoke(in ?)' is not supported by the language
                //         Process((in int p) => System.Console.WriteLine(p));
                Diagnostic(ErrorCode.ERR_BindToBogus, "(in int p) => System.Console.WriteLine(p)").WithArguments("D.Invoke(in ?)").WithLocation(6, 17),
                // (12,9): error CS0570: 'D.Invoke(in ?)' is not supported by the language
                //         func(value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "func(value)").WithArguments("D.Invoke(in ?)").WithLocation(12, 9));
        }

        [Fact]
        public void InAttributeModReqIsNotAllowedInCustomModifiersPosition_Delegates_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & Invoke() runtime managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
  }

  .method public hidebysig newslot virtual
          instance int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute) & EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    private static int value = 5;

    public static void Main()
    {
        Process(() => ref value);
    }

    private static void Process(D func)
    {
        System.Console.WriteLine(func());
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,17): error CS0570: 'D.Invoke()' is not supported by the language
                //         Process(() => ref value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "() => ref value").WithArguments("D.Invoke()").WithLocation(8, 17),
                // (13,34): error CS0570: 'D.Invoke()' is not supported by the language
                //         System.Console.WriteLine(func());
                Diagnostic(ErrorCode.ERR_BindToBogus, "func()").WithArguments("D.Invoke()").WithLocation(13, 34));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForInSignatures_Methods_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) x) cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.M(value);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.M(?)' is not supported by the language
                //         obj.M(value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M(?)").WithLocation(8, 13));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForRefReadOnlySignatures_Methods_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot virtual 
          instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute)
          M() cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  1
    .locals init (int32& V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a
    IL_000a:  ldloc.0
    IL_000b:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.M());
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS0570: 'TestRef.M()' is not supported by the language
                //         System.Console.WriteLine(obj.M());
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M()").WithLocation(7, 38));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForRefReadOnlySignatures_Properties()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot specialname virtual
          instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute)
          get_P() cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 TestRef::'value'
    IL_0006:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }

  .property instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute)
          P()
  {
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) TestRef::get_P()
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.P);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS1546: Property, indexer, or event 'TestRef.P' is not supported by the language; try directly calling accessor method 'TestRef.get_P()'
                //         System.Console.WriteLine(obj.P);
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "P").WithArguments("TestRef.P", "TestRef.get_P()").WithLocation(7, 38));

            code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.get_P());
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS0570: 'TestRef.get_P()' is not supported by the language
                //         System.Console.WriteLine(obj.get_P());
                Diagnostic(ErrorCode.ERR_BindToBogus, "get_P").WithArguments("TestRef.get_P()").WithLocation(7, 38));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForInSignatures_Indexers_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
  .method public hidebysig newslot specialname virtual
          instance void  set_Item(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) p, int32 'value') cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }

  .property instance int32 Item(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute))
  {
    .set instance void TestRef::set_Item(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute), int32)
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj[value] = 0;
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,9): error CS1546: Property, indexer, or event 'TestRef.this[?]' is not supported by the language; try directly calling accessor method 'TestRef.set_Item(?, ?)'
                //         obj[value] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "obj[value]").WithArguments("TestRef.this[?]", "TestRef.set_Item(?, ?)").WithLocation(8, 9));

            code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.set_Item(value, 0);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.set_Item(?, ?)' is not supported by the language
                //         obj.set_Item(value, 0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Item").WithArguments("TestRef.set_Item(?, ?)").WithLocation(8, 13));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForRefReadOnlySignatures_Indexers_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
  .field private int32 'value'
  .method public hidebysig newslot specialname virtual
          instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute)
          get_Item(int32 p) cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  1
    .locals init (int32& V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a
    IL_000a:  ldloc.0
    IL_000b:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  }

  .property instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) Item(int32)
  {
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) TestRef::get_Item(int32)
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj[0]);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,34): error CS1546: Property, indexer, or event 'TestRef.this[?]' is not supported by the language; try directly calling accessor method 'TestRef.get_Item(?)'
                //         System.Console.WriteLine(obj[0]);
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "obj[0]").WithArguments("TestRef.this[?]", "TestRef.get_Item(?)").WithLocation(7, 34));

            code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.get_Item(0));
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS0570: 'TestRef.get_Item(?)' is not supported by the language
                //         System.Console.WriteLine(obj.get_Item(0));
                Diagnostic(ErrorCode.ERR_BindToBogus, "get_Item").WithArguments("TestRef.get_Item(?)").WithLocation(7, 38));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForInSignatures_Delegates_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance void Invoke(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) p) runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) p, class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance void  EndInvoke(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) p, class [mscorlib]System.IAsyncResult result) runtime managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        Process((in int p) => System.Console.WriteLine(p));
    }

    private static void Process(D func)
    {
        int value = 5;
        func(value);
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (6,17): error CS0570: 'D.Invoke(?)' is not supported by the language
                //         Process((in int p) => System.Console.WriteLine(p));
                Diagnostic(ErrorCode.ERR_BindToBogus, "(in int p) => System.Console.WriteLine(p)").WithArguments("D.Invoke(?)").WithLocation(6, 17),
                // (12,9): error CS0570: 'D.Invoke(?)' is not supported by the language
                //         func(value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "func(value)").WithArguments("D.Invoke(?)").WithLocation(12, 9));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForRefReadOnlySignatures_Delegates_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) Invoke() runtime managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
  }

  .method public hidebysig newslot virtual
          instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  }
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    private static int value = 5;

    public static void Main()
    {
        Process(() => ref value);
    }

    private static void Process(D func)
    {
        System.Console.WriteLine(func());
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,17): error CS0570: 'D.Invoke()' is not supported by the language
                //         Process(() => ref value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "() => ref value").WithArguments("D.Invoke()").WithLocation(8, 17),
                // (13,34): error CS0570: 'D.Invoke()' is not supported by the language
                //         System.Console.WriteLine(func());
                Diagnostic(ErrorCode.ERR_BindToBogus, "func()").WithArguments("D.Invoke()").WithLocation(13, 34));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibInAttributeIsNotAvailable_Methods_Parameters()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
}
class Test
{
    public virtual void M(in object x) { }
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (9,27): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                //     public virtual void M(in object x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in object x").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(9, 27));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibInAttributeIsNotAvailable_Methods_ReturnTypes()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
}
class Test
{
    private object value = null;
    public virtual ref readonly object M() => ref value;
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (10,20): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                //     public virtual ref readonly object M() => ref value;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(10, 20));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibInAttributeIsNotAvailable_Properties()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
}
class Test
{
    private object value = null;
    public virtual ref readonly object M => ref value;
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (10,20): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                //     public virtual ref readonly object M => ref value;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(10, 20));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibInAttributeIsNotAvailable_Indexers_Parameters()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
}
class Test
{
    public virtual object this[in object p] => null;
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (9,32): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                //     public virtual object this[in object p] => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in object p").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(9, 32));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibInAttributeIsNotAvailable_Indexers_ReturnTypes()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
}
class Test
{
    private object value = null;
    public virtual ref readonly object this[object p] => ref value;
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (10,20): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                //     public virtual ref readonly object this[object p] => ref value;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(10, 20));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibInAttributeIsNotAvailable_Delegates_Parameters()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class IntPtr {}
    public class Int32 {}
    public class Delegate {}
    public class MulticastDelegate : Delegate {}
}
public delegate void D(in int p);";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (11,24): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                // public delegate void D(in int p);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "in int p").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(11, 24));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibInAttributeIsNotAvailable_Delegates_ReturnTypes()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class IntPtr {}
    public class Int32 {}
    public class Delegate {}
    public class MulticastDelegate : Delegate {}
}
public delegate ref readonly int D();";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (11,17): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                // public delegate ref readonly int D();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(11, 17));
        }

        [Fact]
        public void InAttributeIsWrittenOnInMembers_Methods_Parameters_Virtual()
        {
            var code = @"
class Test
{
    public virtual void Method(in int x) { }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnInMembers_Methods_Parameters_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract void Method(in int x);
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_Virtual()
        {
            var code = @"
class Test
{
    private int x = 0;
    public virtual ref readonly int Method() => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract ref readonly int Method();
}";

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_NoModifiers()
        {
            var code = @"
class Test
{
    private int x = 0;
    public ref readonly int Method() => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_Static()
        {
            var code = @"
class Test
{
    private static int x = 0;
    public static ref readonly int Method() => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Properties_Virtual()
        {
            var code = @"
class Test
{
    private int x = 0;
    public virtual ref readonly int Property => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Properties_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract ref readonly int Property { get; }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Properties_NoModifiers()
        {
            var code = @"
class Test
{
    private int x = 0;
    public ref readonly int Property => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Properties_Static()
        {
            var code = @"
class Test
{
    private static int x = 0;
    public static ref readonly int Property => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnInMembers_Indexers_Parameters_Virtual()
        {
            var code = @"
class Test
{
    public virtual int this[in int x] => x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnInMembers_Indexers_Parameters_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract int this[in int x] { get; }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Indexers_ReturnTypes_Virtual()
        {
            var code = @"
class Test
{
    private int x;
    public virtual ref readonly int this[int p] => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Indexers_ReturnTypes_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract ref readonly int this[int p] { get; }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Indexers_ReturnTypes_NoModifiers()
        {
            var code = @"
class Test
{
    private int x;
    public ref readonly int this[int p] => ref x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnInMembers_Delegates_Parameters()
        {
            var code = "public delegate void D(in int p);";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("D");

                var invokeParameter = type.DelegateInvokeMethod.Parameters.Single();
                Assert.Empty(invokeParameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(invokeParameter.RefCustomModifiers);

                var beginInvokeParameter = type.GetMethod("BeginInvoke").Parameters.First();
                Assert.Empty(beginInvokeParameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(beginInvokeParameter.RefCustomModifiers);

                var endInvokeParameter = type.GetMethod("EndInvoke").Parameters.First();
                Assert.Empty(endInvokeParameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(endInvokeParameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsWrittenOnRefReadOnlyMembers_Delegates_ReturnTypes()
        {
            var code = "public delegate ref readonly int D();";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("D");

                var invokeMethod = type.DelegateInvokeMethod;
                Assert.Empty(invokeMethod.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(invokeMethod.RefCustomModifiers);

                var endInvokeMethod = type.GetMethod("EndInvoke");
                Assert.Empty(endInvokeMethod.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(endInvokeMethod.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsNotWrittenOnInMembers_Methods_Parameters_NoModifiers()
        {
            var code = @"
class Test
{
    public void Method(in int x) { }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsNotWrittenOnInMembers_Methods_Parameters_Static()
        {
            var code = @"
class Test
{
    public static void Method(in int x) { }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsNotWrittenOnInMembers_Indexers_Parameters_NoModifiers()
        {
            var code = @"
class Test
{
    public int this[in int x] => x;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsNotWrittenOnRefReadOnlyMembers_Operators_Unary()
        {
            var code = @"
public class Test
{
    public static bool operator!(in Test obj) => false;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("op_LogicalNot").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsNotWrittenOnRefReadOnlyMembers_Operators_Binary()
        {
            var code = @"
public class Test
{
    public static bool operator+(in Test obj1, in Test obj2) => false;
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameters = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("op_Addition").Parameters;
                Assert.Equal(2, parameters.Length);

                Assert.Empty(parameters[0].TypeWithAnnotations.CustomModifiers);
                Assert.Empty(parameters[0].RefCustomModifiers);

                Assert.Empty(parameters[1].TypeWithAnnotations.CustomModifiers);
                Assert.Empty(parameters[1].RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeIsNotWrittenOnRefReadOnlyMembers_Constructors()
        {
            var code = @"
public class Test
{
    public Test(in int x) { }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod(".ctor").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, verify: Verification.Passes, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void InAttributeModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Methods_Parameters()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit TestRef extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance void M (int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .maxstack 8
        IL_0000: nop                  // Do nothing (No operation)
        IL_0001: ret                  // Return from method, possibly with a value
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0              // Load argument 0 onto the stack
        IL_0001: call instance void [mscorlib]System.Object::.ctor() // Call method indicated on the stack with arguments
        IL_0006: nop                  // Do nothing (No operation)
        IL_0007: ret                  // Return from method, possibly with a value
    }
}";

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.M(ref value);
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.M(ref int)' is not supported by the language
                //         obj.M(ref value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M(ref int)").WithLocation(8, 13));
        }

        [Fact]
        public void InAttributeModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Methods_ReturnTypes()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit TestRef extends [mscorlib]System.Object
{
    .field private int32 'value'

    .method public hidebysig newslot virtual instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) M () cil managed 
    {
        .maxstack 1
        .locals init ([0] int32&)

        IL_0000: nop                  // Do nothing (No operation)
        IL_0001: ldarg.0              // Load argument 0 onto the stack
        IL_0002: ldflda int32 TestRef::'value' // Push the address of field of object obj on the stack
        IL_0007: stloc.0              // Pop a value from stack into local variable 0
        IL_0008: br.s IL_000a         // Branch to target, short form
        IL_000a: ldloc.0              // Load local variable 0 onto stack
        IL_000b: ret                  // Return from method, possibly with a value
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0              // Load argument 0 onto the stack
        IL_0001: ldc.i4.0             // Push 0 onto the stack as int32
        IL_0002: stfld int32 TestRef::'value' // Replace the value of field of the object obj with value
        IL_0007: ldarg.0              // Load argument 0 onto the stack
        IL_0008: call instance void [mscorlib]System.Object::.ctor() // Call method indicated on the stack with arguments
        IL_000d: nop                  // Do nothing (No operation)
        IL_000e: ret                  // Return from method, possibly with a value
    }
}";

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        var value = obj.M();
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (7,25): error CS0570: 'TestRef.M()' is not supported by the language
                //         var value = obj.M();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M()").WithLocation(7, 25));
        }

        [Fact]
        public void InAttributeModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Properties()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit TestRef extends [mscorlib]System.Object
{
    .field private int32 'value'

    .method public hidebysig specialname newslot virtual instance int32& get_P () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0              // Load argument 0 onto the stack
        IL_0001: ldflda int32 TestRef::'value' // Push the address of field of object obj on the stack
        IL_0006: ret                  // Return from method, possibly with a value
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0              // Load argument 0 onto the stack
        IL_0001: ldc.i4.0             // Push 0 onto the stack as int32
        IL_0002: stfld int32 TestRef::'value' // Replace the value of field of the object obj with value
        IL_0007: ldarg.0              // Load argument 0 onto the stack
        IL_0008: call instance void [mscorlib]System.Object::.ctor() // Call method indicated on the stack with arguments
        IL_000d: nop                  // Do nothing (No operation)
        IL_000e: ret                  // Return from method, possibly with a value
    }

    .property instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) P()
    {
        .get instance int32& TestRef::get_P()
    }
}";

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        var value = obj.P;
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (7,25): error CS0570: 'TestRef.P' is not supported by the language
                //         var value = obj.P;
                Diagnostic(ErrorCode.ERR_BindToBogus, "P").WithArguments("TestRef.P").WithLocation(7, 25));
        }

        [Fact]
        public void InAttributeModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Indexers_Parameters()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
  .method public hidebysig newslot specialname virtual
          instance void  set_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p, int32 'value') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }

  .property instance int32 Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute))
  {
    .set instance void TestRef::set_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
  }
}";

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj[value] = 0;
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (8,9): error CS1546: Property, indexer, or event 'TestRef.this[ref int]' is not supported by the language; try directly calling accessor method 'TestRef.set_Item(ref int, int)'
                //         obj[value] = 0;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "obj[value]").WithArguments("TestRef.this[ref int]", "TestRef.set_Item(ref int, int)").WithLocation(8, 9));

            code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.set_Item(value, 0);
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.set_Item(ref int, int)' is not supported by the language
                //         obj.set_Item(value, 0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "set_Item").WithArguments("TestRef.set_Item(ref int, int)").WithLocation(8, 13));
        }

        [Fact]
        public void InAttributeModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Indexers_ReturnTypes()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit TestRef extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)
    
    .field private int32 'value'

    .method public hidebysig specialname newslot virtual instance int32& get_Item (int32 p) cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0              // Load argument 0 onto the stack
        IL_0001: ldflda int32 TestRef::'value' // Push the address of field of object obj on the stack
        IL_0006: ret                  // Return from method, possibly with a value
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0              // Load argument 0 onto the stack
        IL_0001: ldc.i4.0             // Push 0 onto the stack as int32
        IL_0002: stfld int32 TestRef::'value' // Replace the value of field of the object obj with value
        IL_0007: ldarg.0              // Load argument 0 onto the stack
        IL_0008: call instance void [mscorlib]System.Object::.ctor() // Call method indicated on the stack with arguments
        IL_000d: nop                  // Do nothing (No operation)
        IL_000e: ret                  // Return from method, possibly with a value
    }

    .property instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) Item(int32 p)
    {
        .get instance int32& TestRef::get_Item(int32)
    }
}";

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        var value = obj[5];
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (7,21): error CS0570: 'TestRef.this[int]' is not supported by the language
                //         var value = obj[5];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[5]").WithArguments("TestRef.this[int]").WithLocation(7, 21));
        }

        [Fact]
        public void InAttributeModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Delegates_Parameters()
        {
            var ilSource = @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance void Invoke(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p) runtime managed
  {
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p, class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
  }

  .method public hidebysig newslot virtual
          instance void  EndInvoke(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p, class [mscorlib]System.IAsyncResult result) runtime managed
  {
  }
}";

            var code = @"
public class Test
{
    public static void Main()
    {
        Process((in int p) => System.Console.WriteLine(p));
    }

    private static void Process(D func)
    {
        int value = 5;
        func(value);
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (6,17): error CS0570: 'D.Invoke(ref int)' is not supported by the language
                //         Process((in int p) => System.Console.WriteLine(p));
                Diagnostic(ErrorCode.ERR_BindToBogus, "(in int p) => System.Console.WriteLine(p)").WithArguments("D.Invoke(ref int)").WithLocation(6, 17),
                // (12,9): error CS0570: 'D.Invoke(ref int)' is not supported by the language
                //         func(value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "func(value)").WithArguments("D.Invoke(ref int)").WithLocation(12, 9));
        }

        [Fact]
        public void InAttributeModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Delegates_ReturnTypes()
        {
            var ilSource = @"
.class public auto ansi sealed D
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object', native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) Invoke() runtime managed
  {
  }

  .method public hidebysig newslot virtual
          instance class [mscorlib]System.IAsyncResult
          BeginInvoke(class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed
  {
  }

  .method public hidebysig newslot virtual
          instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  }
}";

            var code = @"
public class Test
{
    private static int value = 5;

    public static void Main()
    {
        Process(() => ref value);
    }

    private static void Process(D func)
    {
        System.Console.WriteLine(func());
    }
}";

            CreateCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (8,17): error CS0570: 'D.Invoke()' is not supported by the language
                //         Process(() => ref value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "() => ref value").WithArguments("D.Invoke()").WithLocation(8, 17),
                // (13,34): error CS0570: 'D.Invoke()' is not supported by the language
                //         System.Console.WriteLine(func());
                Diagnostic(ErrorCode.ERR_BindToBogus, "func()").WithArguments("D.Invoke()").WithLocation(13, 34));
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_Parameters_Class_Abstract()
        {
            var reference = CreateCompilation(@"
public abstract class Parent
{
    public abstract void M(in int p);
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override void M(in int p)
    {
        System.Console.WriteLine(p);
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj.M(5);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_Parameters_Class_Virtual()
        {
            var reference = CreateCompilation(@"
public class Parent
{
    public virtual void M(in int p) {}
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override void M(in int p)
    {
        System.Console.WriteLine(p);
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj.M(5);
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_Parameters_ImplicitInterfaces_NonVirtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    void M(in int p);
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public void M(in int p)
    {
        System.Console.WriteLine(p);
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj.M(5);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var implicitParameter = type.GetMethod("M").Parameters.Single();
                Assert.Empty(implicitParameter.TypeWithAnnotations.CustomModifiers);
                Assert.Empty(implicitParameter.RefCustomModifiers);

                var explicitImplementation = type.GetMethod("Parent.M");
                Assert.Equal("void Parent.M(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 p)", explicitImplementation.ExplicitInterfaceImplementations.Single().ToTestDisplayString());

                var explicitParameter = explicitImplementation.Parameters.Single();
                Assert.Empty(explicitParameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(explicitParameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_Parameters_ImplicitInterfaces_Virtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    void M(in int p);
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public virtual void M(in int p)
    {
        System.Console.WriteLine(p);
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj.M(5);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_Parameters_ExplicitInterfaces()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    void M(in int p);
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetMethod("M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    void Parent.M(in int p)
    {
        System.Console.WriteLine(p);
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj.M(5);
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetMethod("Parent.M").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_ReturnTypes_Class_Abstract()
        {
            var reference = CreateCompilation(@"
public abstract class Parent
{
    public abstract ref readonly int M();
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var method = type.GetMethod("M");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var method = type.GetMethod("M");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_ReturnTypes_Class_Virtual()
        {
            var reference = CreateCompilation(@"
public class Parent
{
    public virtual ref readonly int M() { throw null; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var method = type.GetMethod("M");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var method = type.GetMethod("M");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_ReturnTypes_ImplicitInterfaces_NonVirtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int M();
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var method = type.GetMethod("M");

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var implicitMethod = type.GetMethod("M");
                Assert.Empty(implicitMethod.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(implicitMethod.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_ReturnTypes_ImplicitInterfaces_Virtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int M();
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var implicitMethod = type.GetMethod("M");

                Assert.Empty(implicitMethod.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(implicitMethod.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public virtual ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var implicitMethod = type.GetMethod("M");

                Assert.Empty(implicitMethod.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(implicitMethod.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Methods_ReturnTypes_ExplicitInterfaces()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int M();
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var implicitMethod = type.GetMethod("M");

                Assert.Empty(implicitMethod.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(implicitMethod.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    ref readonly int Parent.M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var implicitMethod = type.GetMethod("Parent.M");

                Assert.Empty(implicitMethod.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(implicitMethod.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Properties_Class_Abstract()
        {
            var reference = CreateCompilation(@"
public abstract class Parent
{
    public abstract ref readonly int P { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override ref readonly int P => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.P);
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Properties_Class_Virtual()
        {
            var reference = CreateCompilation(@"
public class Parent
{
    public virtual ref readonly int P { get { throw null; } }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override ref readonly int P => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.P);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Properties_ImplicitInterface_NonVirtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int P { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public ref readonly int P => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.P);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var implicitproperty = type.GetProperty("P");
                Assert.Empty(implicitproperty.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(implicitproperty.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Properties_ImplicitInterface_Virtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int P { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public virtual ref readonly int P => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.P);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Properties_ExplicitInterface()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int P { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var property = type.GetProperty("P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    ref readonly int Parent.P => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.P);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var property = type.GetProperty("Parent.P");

                Assert.Empty(property.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(property.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_Parameters_Class_Abstract()
        {
            var reference = CreateCompilation(@"
public abstract class Parent
{
    public abstract int this[in int p] { set; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override int this[in int p]
    {
        set { System.Console.WriteLine(p); }
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj[5] = 0;
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_Parameters_Class_Virtual()
        {
            var reference = CreateCompilation(@"
public class Parent
{
    public virtual int this[in int p] { set { } }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override int this[in int p]
    {
        set { System.Console.WriteLine(p); }
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj[5] = 0;
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_Parameters_ImplicitInterface_NonVirtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    int this[in int p] { set; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public int this[in int p]
    {
        set { System.Console.WriteLine(p); }
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj[5] = 0;
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var implicitParameter = type.GetProperty("this[]").Parameters.Single();
                Assert.Empty(implicitParameter.TypeWithAnnotations.CustomModifiers);
                Assert.Empty(implicitParameter.RefCustomModifiers);

                var explicitImplementation = type.GetMethod("Parent.set_Item");
                Assert.Equal("void Parent.this[in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 p].set", explicitImplementation.ExplicitInterfaceImplementations.Single().ToTestDisplayString());

                var explicitParameter = explicitImplementation.Parameters.First();
                Assert.Empty(explicitParameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(explicitParameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_Parameters_ImplicitInterface_Virtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    int this[in int p] { set; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public virtual int this[in int p]
    {
        set { System.Console.WriteLine(p); }
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj[5] = 0;
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_Parameters_ExplicitInterface()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    int this[in int p] { set; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var parameter = type.GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    int Parent.this[in int p]
    {
        set { System.Console.WriteLine(p); }
    }
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        obj[5] = 0;
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var parameter = type.GetProperty("Parent.Item").Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_ReturnTypes_Class_Abstract()
        {
            var reference = CreateCompilation(@"
public abstract class Parent
{
    public abstract ref readonly int this[int p] { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override ref readonly int this[int p] => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj[0]);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_ReturnTypes_Class_Virtual()
        {
            var reference = CreateCompilation(@"
public class Parent
{
    public virtual ref readonly int this[int p] { get { throw null; } }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public override ref readonly int this[int p] => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj[0]);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_ReturnTypes_ImplicitInterface_NonVirtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int this[int p] { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public ref readonly int this[int p] => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj[0]);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_ReturnTypes_ImplicitInterface_Virtual()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int this[int p] { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    public virtual ref readonly int this[int p] => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj[0]);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void WhenImplementingParentWithModifiersCopyThem_Indexers_ReturnTypes_ExplicitInterface()
        {
            var reference = CreateCompilation(@"
public interface Parent
{
    ref readonly int this[int p] { get; }
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Parent");
                var indexer = type.GetProperty("this[]");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            });

            var code = @"
public class Child : Parent
{
    ref readonly int Parent.this[int p] => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj[0]);
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");
                var indexer = type.GetProperty("Parent.Item");

                Assert.Empty(indexer.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(indexer.RefCustomModifiers);
            };

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5", symbolValidator: validator);
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5", symbolValidator: validator);
        }

        [Fact]
        public void CreatingLambdasOfDelegatesWithModifiersCanBeExecuted_Parameters()
        {
            var reference = CreateCompilation("public delegate void D(in int p);");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod.Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
public class Test
{
    public static void Main()
    {
        Run((in int p) => System.Console.WriteLine(p));
    }

    public static void Run(D lambda)
    {
        lambda(value);
    }

    private static int value = 5;
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void CreatingLambdasOfDelegatesWithModifiersCanBeExecuted_ReturnTypes()
        {
            var reference = CreateCompilation("public delegate ref readonly int D();");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            });

            var code = @"
public class Test
{
    private static int value = 5;

    public static void Main()
    {
        Run(() => ref value);
    }

    public static void Run(D lambda)
    {
        System.Console.WriteLine(lambda());
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void CreatingLambdasOfDelegatesWithModifiersCanBeExecuted_Parameters_DuplicateModifierTypes()
        {
            var reference = CreateCompilation(@"

namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public delegate void D(in int p);");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod.Parameters.Single();

                Assert.Empty(parameter.TypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(parameter.RefCustomModifiers);
            });

            var code = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Test
{
    public static void Main()
    {
        Run((in int p) => System.Console.WriteLine(p));
    }

    public static void Run(D lambda)
    {
        lambda(value);
    }

    private static int value = 5;
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void CreatingLambdasOfDelegatesWithModifiersCanBeExecuted_ReturnTypes_DuplicateModifierTypes()
        {
            var reference = CreateCompilation(@"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public delegate ref readonly int D();");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;

                Assert.Empty(method.ReturnTypeWithAnnotations.CustomModifiers);
                AssertSingleInAttributeRequiredModifier(method.RefCustomModifiers);
            });

            var code = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Test
{
    private static int value = 5;

    public static void Main()
    {
        Run(() => ref value);
    }

    public static void Run(D lambda)
    {
        System.Console.WriteLine(lambda());
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "5");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "5");
        }

        [Fact]
        public void OverridingMethodSymbolDoesNotCopyModifiersIfItWasRefKindNone_Interface()
        {
            var code = @"
public interface ITest
{
    ref readonly int M();
}
public class Test : ITest
{
    public int M() => 0;
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (6,21): error CS8152: 'Test' does not implement interface member 'ITest.M()'. 'Test.M()' cannot implement 'ITest.M()' because it does not have matching return by reference.
                // public class Test : ITest
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "ITest").WithArguments("Test", "ITest.M()", "Test.M()").WithLocation(6, 21));

            var interfaceMethod = comp.GetTypeByMetadataName("ITest").GetMethod("M");
            Assert.Equal(RefKind.RefReadOnly, interfaceMethod.RefKind);
            Assert.Empty(interfaceMethod.ReturnTypeWithAnnotations.CustomModifiers);
            AssertSingleInAttributeRequiredModifier(interfaceMethod.RefCustomModifiers);

            var classMethod = comp.GetTypeByMetadataName("Test").GetMethod("M");
            Assert.Equal(RefKind.None, classMethod.RefKind);
            Assert.Empty(classMethod.ReturnTypeWithAnnotations.CustomModifiers);
            Assert.Empty(classMethod.RefCustomModifiers);
        }

        [Fact]
        public void OverridingMethodSymbolDoesNotCopyModifiersIfItWasRefKindNone_Class()
        {
            var code = @"
public abstract class ParentTest
{
    public abstract ref readonly int M();
}
public class Test : ParentTest
{
    public override int M() => 0;
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,25): error CS8148: 'Test.M()' must match by reference return of overridden member 'ParentTest.M()'
                //     public override int M() => 0;
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "M").WithArguments("Test.M()", "ParentTest.M()").WithLocation(8, 25));

            var parentMethod = comp.GetTypeByMetadataName("ParentTest").GetMethod("M");
            Assert.Equal(RefKind.RefReadOnly, parentMethod.RefKind);
            Assert.Empty(parentMethod.ReturnTypeWithAnnotations.CustomModifiers);
            AssertSingleInAttributeRequiredModifier(parentMethod.RefCustomModifiers);

            var classMethod = comp.GetTypeByMetadataName("Test").GetMethod("M");
            Assert.Equal(RefKind.None, classMethod.RefKind);
            Assert.Empty(classMethod.ReturnTypeWithAnnotations.CustomModifiers);
            Assert.Empty(classMethod.RefCustomModifiers);
        }

        [Fact]
        public void OverloadResolutionShouldBeAbleToPickOverloadsWithNoModreqsOverOnesWithModreq_Methods_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual
          instance void  M(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p) cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method TestRef::M

  .method public hidebysig newslot virtual
          instance void  M(int64 p) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int64)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method TestRef::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj.M(value);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void OverloadResolutionShouldBeAbleToPickOverloadsWithNoModreqsOverOnesWithModreq_Methods_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
   .field private int32 'value'
  .method public hidebysig newslot virtual
          instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)
          M(int32 p) cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 TestRef::'value'
    IL_0006:  ret
  } // end of method TestRef::M

  .method public hidebysig newslot virtual
          instance int32  M(int64 p) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 TestRef::'value'
    IL_0006:  ret
  } // end of method TestRef::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  } // end of method TestRef::.ctor
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj.M(0));
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void OverloadResolutionShouldBeAbleToPickOverloadsWithNoModreqsOverOnesWithModreq_Indexers_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .method public hidebysig newslot specialname virtual
          instance void  set_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) p,
                                  int32 'value') cil managed
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0008:  nop
    IL_0009:  ret
  } // end of method TestRef::set_Item

  .method public hidebysig newslot specialname virtual
          instance void  set_Item(int64 p,
                                  int32 'value') cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int64)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method TestRef::set_Item

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestRef::.ctor

  .property instance int32 Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute))
  {
    .set instance void TestRef::set_Item(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute),
                                         int32)
  } // end of property TestRef::Item
  .property instance int32 Item(int64)
  {
    .set instance void TestRef::set_Item(int64,
                                         int32)
  } // end of property TestRef::Item
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        int value = 5;
        var obj = new TestRef();
        obj[value] = 0;
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void OverloadResolutionShouldBeAbleToPickOverloadsWithNoModreqsOverOnesWithModreq_Indexers_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .field private int32 'value'
  .method public hidebysig newslot specialname virtual
          instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)
          get_Item(int32 p) cil managed
  {
    .param [0]
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (int32& V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method TestRef::get_Item

  .method public hidebysig newslot specialname virtual
          instance int32  get_Item(int64 p) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int32 TestRef::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method TestRef::get_Item

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  stfld      int32 TestRef::'value'
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  nop
    IL_000e:  ret
  } // end of method TestRef::.ctor

  .property instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)
          Item(int32)
  {
    .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) TestRef::get_Item(int32)
  } // end of property TestRef::Item
  .property instance int32 Item(int64)
  {
    .get instance int32 TestRef::get_Item(int64)
  } // end of property TestRef::Item
}";

            var reference = CompileIL(ilSource, prependDefaultHeader: false);

            var code = @"
public class Test
{
    public static void Main()
    {
        var obj = new TestRef();
        System.Console.WriteLine(obj[0]);
    }
}";

            CompileAndVerify(code, references: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void UsingInAttributeFromReferenceWhileHavingDuplicateInCompilation_Class_Virtual()
        {
            var testRef = CreateCompilation(@"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Parent
{
    public virtual ref readonly int M() { throw null; }
}", assemblyName: "testRef");

            CompileAndVerify(testRef, symbolValidator: module =>
            {
                var parentModifier = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", parentModifier.ContainingAssembly.Name);
            });

            var userCode = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Child : Parent
{
    public override ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var childModifier = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", childModifier.ContainingAssembly.Name);
            };

            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.ToMetadataReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.EmitToImageReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
        }

        [Fact]
        public void UsingInAttributeFromReferenceWhileHavingDuplicateInCompilation_Class_Abstract()
        {
            var testRef = CreateCompilation(@"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public abstract class Parent
{
    public abstract ref readonly int M();
}", assemblyName: "testRef");

            CompileAndVerify(testRef, symbolValidator: module =>
            {
                var parentModifier = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", parentModifier.ContainingAssembly.Name);
            });

            var userCode = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Child : Parent
{
    public override ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var childModifier = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", childModifier.ContainingAssembly.Name);
            };

            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.ToMetadataReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.EmitToImageReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
        }

        [Fact]
        public void UsingInAttributeFromReferenceWhileHavingDuplicateInCompilation_ExplicitInterface()
        {
            var testRef = CreateCompilation(@"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public interface Parent
{
    ref readonly int M();
}", assemblyName: "testRef");

            CompileAndVerify(testRef, symbolValidator: module =>
            {
                var parentModifier = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", parentModifier.ContainingAssembly.Name);
            });

            var userCode = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Child : Parent
{
    ref readonly int Parent.M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var explicitModifier = type.GetMethod("Parent.M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", explicitModifier.ContainingAssembly.Name);
            };

            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.ToMetadataReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.EmitToImageReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
        }

        [Fact]
        public void UsingInAttributeFromReferenceWhileHavingDuplicateInCompilation_ImplicitInterface_Virtual()
        {
            var testRef = CreateCompilation(@"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public interface Parent
{
    ref readonly int M();
}", assemblyName: "testRef");

            CompileAndVerify(testRef, symbolValidator: module =>
            {
                var parentModifier = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", parentModifier.ContainingAssembly.Name);
            });

            var userCode = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Child : Parent
{
    public virtual ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var implicitModifier = type.GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal(module.ContainingAssembly.Name, implicitModifier.ContainingAssembly.Name);

                var explicitModifier = type.GetMethod("Parent.M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", explicitModifier.ContainingAssembly.Name);
            };

            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.ToMetadataReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.EmitToImageReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
        }

        [Fact]
        public void UsingInAttributeFromReferenceWhileHavingDuplicateInCompilation_ImplicitInterface_NonVirtual()
        {
            var testRef = CreateCompilation(@"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public interface Parent
{
    ref readonly int M();
}", assemblyName: "testRef");

            CompileAndVerify(testRef, symbolValidator: module =>
            {
                var parentModifier = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", parentModifier.ContainingAssembly.Name);
            });

            var userCode = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}
public class Child : Parent
{
    public ref readonly int M() => ref value;
    private int value = 5;
}
public class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M());
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Child");

                var implicitModifier = type.GetMethod("M").RefCustomModifiers.Single().Modifier;
                Assert.Equal(module.ContainingAssembly.Name, implicitModifier.ContainingAssembly.Name);

                var explicitModifier = type.GetMethod("Parent.M").RefCustomModifiers.Single().Modifier;
                Assert.Equal("testRef", explicitModifier.ContainingAssembly.Name);
            };

            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.ToMetadataReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
            CompileAndVerify(source: userCode, expectedOutput: "5", references: new[] { testRef.EmitToImageReference() }, options: TestOptions.ReleaseExe, symbolValidator: validator);
        }

        [Fact]
        public void DuplicateInAttributeTypeInReferences()
        {
            var refCode = @"
namespace System.Runtime.InteropServices
{
    public class InAttribute {}
}";

            var ref1 = CreateCompilation(refCode).EmitToImageReference();
            var ref2 = CreateCompilation(refCode).EmitToImageReference();

            var user = @"
public class Test
{
    public ref readonly int M() => throw null;
}";

            CreateCompilation(user, references: new[] { ref1, ref2 }).VerifyDiagnostics(
                // (4,12): error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported
                //     public ref readonly int M() => throw null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly int").WithArguments("System.Runtime.InteropServices.InAttribute").WithLocation(4, 12));
        }

        [Fact]
        public void ParentClassOfProxiedInterfaceFunctionHasNoModreq_ImplementedInChild()
        {
            var code = @"
class Parent
{
    public void M(in int x) { }
}
interface IM
{
    void M(in int x);
}
class Child: Parent, IM
{
    public void M(in int x) { }
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                // Nothing on Parent
                var parentMethod = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M");
                Assert.False(parentMethod.IsMetadataVirtual());
                Assert.Empty(parentMethod.Parameters.Single().RefCustomModifiers);

                // Nothing on Child
                var childMethod = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M");
                Assert.False(childMethod.IsMetadataVirtual());
                Assert.Empty(childMethod.Parameters.Single().RefCustomModifiers);

                // Modreq on Interface
                var interfaceMethod = module.ContainingAssembly.GetTypeByMetadataName("IM").GetMethod("M");
                Assert.True(interfaceMethod.IsMetadataVirtual());
                AssertSingleInAttributeRequiredModifier(interfaceMethod.Parameters.Single().RefCustomModifiers);

                // Modreq on proxy
                var proxyMethod = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("IM.M");
                Assert.True(proxyMethod.IsMetadataVirtual());
                AssertSingleInAttributeRequiredModifier(proxyMethod.Parameters.Single().RefCustomModifiers);
            }).VerifyDiagnostics(
                // (12,17): warning CS0108: 'Child.M(in int)' hides inherited member 'Parent.M(in int)'. Use the new keyword if hiding was intended.
                //     public void M(in int x) { }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("Child.M(in int)", "Parent.M(in int)").WithLocation(12, 17));
        }

        [Fact]
        public void ParentClassOfProxiedInterfaceFunctionHasNoModreq_NotImplementedInChild()
        {
            var code = @"
class Parent
{
    public void M(in int x) { }
}
interface IM
{
    void M(in int x);
}
class Child: Parent, IM
{
}";

            CompileAndVerify(code, verify: Verification.Passes, symbolValidator: module =>
            {
                // Nothing on Parent
                var parentMethod = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M");
                Assert.False(parentMethod.IsMetadataVirtual());
                Assert.Empty(parentMethod.Parameters.Single().RefCustomModifiers);

                // No method on Child
                Assert.DoesNotContain("M", module.ContainingAssembly.GetTypeByMetadataName("Child").MemberNames);

                // Modreq on Interface
                var interfaceMethod = module.ContainingAssembly.GetTypeByMetadataName("IM").GetMethod("M");
                Assert.True(interfaceMethod.IsMetadataVirtual());
                AssertSingleInAttributeRequiredModifier(interfaceMethod.Parameters.Single().RefCustomModifiers);

                // Modreq on proxy
                var proxyMethod = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("IM.M");
                Assert.True(proxyMethod.IsMetadataVirtual());
                AssertSingleInAttributeRequiredModifier(proxyMethod.Parameters.Single().RefCustomModifiers);
            }).VerifyDiagnostics();
        }

        private void AssertSingleInAttributeRequiredModifier(ImmutableArray<CustomModifier> modifiers)
        {
            var modifier = modifiers.Single();
            var typeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_InteropServices_InAttribute);

            Assert.False(modifier.IsOptional);
            Assert.Equal(typeName, modifier.Modifier.ToDisplayString());
        }

        private const string IsReadOnlyAttributeIL = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly Test
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78 63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module Test.dll
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001

.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  }
}
";
    }
}
