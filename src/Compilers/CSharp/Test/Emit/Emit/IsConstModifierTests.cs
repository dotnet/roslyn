// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class IsConstModifierTests : CSharpTestBase
    {
        [Fact]
        public void IsConstModReqIsConsumedInRefCustomModifiersPosition_Methods_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M(int32& modreq([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed
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

            CompileAndVerify(code, additionalRefs: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void IsConstModReqIsConsumedInRefCustomModifiersPosition_Methods_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot virtual 
          instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsConst) 
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

            CompileAndVerify(code, additionalRefs: new[] { reference }, expectedOutput: "5");
        }

        [Fact]
        public void IsConstModReqIsNotAllowedInCustomModifiersPosition_Methods_Parameters()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsConst)& x) cil managed
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

            CreateStandardCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.M(in ?)' is not supported by the language
                //         obj.M(value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M(in ?)").WithLocation(8, 13));
        }

        [Fact]
        public void IsConstModReqIsNotAllowedInCustomModifiersPosition_Methods_ReturnTypes()
        {
            var ilSource = IsReadOnlyAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .field private int32 'value'
  .method public hidebysig newslot virtual 
          instance int32 modreq([mscorlib]System.Runtime.CompilerServices.IsConst) &
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

            CreateStandardCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (7,38): error CS0570: 'TestRef.M()' is not supported by the language
                //         System.Console.WriteLine(obj.M());
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M()").WithLocation(7, 38));
        }

        [Fact]
        public void OtherModReqsAreNotAllowedOnRefCustomModifiersForRefReadOnlySignatures_Methods_Parameters()
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

            CreateStandardCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.M(?)' is not supported by the language
                //         obj.M(value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M(?)").WithLocation(8, 13));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibIsConstIsNotAvailable_Methods_Parameters()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
}
class Test
{
    public virtual void M(ref readonly object x) { }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (9,27): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsConst' is not defined or imported
                //     public virtual void M(ref readonly object x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object x").WithArguments("System.Runtime.CompilerServices.IsConst").WithLocation(9, 27));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibIsConstIsNotAvailable_Methods_ReturnTypes()
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

            CreateCompilation(code).VerifyDiagnostics(
                // (10,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsConst' is not defined or imported
                //     public virtual ref readonly object M() => ref value;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object").WithArguments("System.Runtime.CompilerServices.IsConst").WithLocation(10, 20));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibIsConstIsNotAvailable_Properties()
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

            CreateCompilation(code).VerifyDiagnostics(
                // (10,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsConst' is not defined or imported
                //     public virtual ref readonly object M => ref value;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object").WithArguments("System.Runtime.CompilerServices.IsConst").WithLocation(10, 20));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibIsConstIsNotAvailable_Indexers_Parameters()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
}
class Test
{
    public virtual object this[ref readonly object p] => null;
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (9,32): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsConst' is not defined or imported
                //     public virtual object this[ref readonly object p] => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object p").WithArguments("System.Runtime.CompilerServices.IsConst").WithLocation(9, 32));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfMscorlibIsConstIsNotAvailable_Indexers_ReturnTypes()
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

            CreateCompilation(code).VerifyDiagnostics(
                // (10,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsConst' is not defined or imported
                //     public virtual ref readonly object this[object p] => ref value;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "ref readonly object").WithArguments("System.Runtime.CompilerServices.IsConst").WithLocation(10, 20));
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Methods_Parameters_Virtual()
        {
            var code = @"
class Test
{
    public virtual void Method(ref readonly int x) { }
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                AssertSingleIsConstRequiredModifier(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Methods_Parameters_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract void Method(ref readonly int x);
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                AssertSingleIsConstRequiredModifier(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_Virtual()
        {
            var code = @"
class Test
{
    private int x = 0;
    public virtual ref readonly int Method() => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeCustomModifiers);
                AssertSingleIsConstRequiredModifier(method.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract ref readonly int Method();
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeCustomModifiers);
                AssertSingleIsConstRequiredModifier(method.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Properties_Virtual()
        {
            var code = @"
class Test
{
    private int x = 0;
    public virtual ref readonly int Property => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeCustomModifiers);
                AssertSingleIsConstRequiredModifier(property.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Properties_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract ref readonly int Property { get; }
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeCustomModifiers);
                AssertSingleIsConstRequiredModifier(property.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Indexers_Parameters_Virtual()
        {
            var code = @"
class Test
{
    public virtual int this[ref readonly int x] => x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                AssertSingleIsConstRequiredModifier(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Indexers_Parameters_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract int this[ref readonly int x] { get; }
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                AssertSingleIsConstRequiredModifier(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Indexers_ReturnTypes_Virtual()
        {
            var code = @"
class Test
{
    private int x;
    public virtual ref readonly int this[int p] => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");

                Assert.Empty(indexer.TypeCustomModifiers);
                AssertSingleIsConstRequiredModifier(indexer.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsWrittenOnRefReadOnlyMembers_Indexers_ReturnTypes_Abstract()
        {
            var code = @"
abstract class Test
{
    public abstract ref readonly int this[int p] { get; }
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");

                Assert.Empty(indexer.TypeCustomModifiers);
                AssertSingleIsConstRequiredModifier(indexer.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Methods_Parameters_NoModifiers()
        {
            var code = @"
class Test
{
    public void Method(ref readonly int x) { }
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Methods_Parameters_Static()
        {
            var code = @"
class Test
{
    public static void Method(ref readonly int x) { }
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_NoModifiers()
        {
            var code = @"
class Test
{
    private int x = 0;
    public ref readonly int Method() => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeCustomModifiers);
                Assert.Empty(method.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Methods_ReturnTypes_Static()
        {
            var code = @"
class Test
{
    private static int x = 0;
    public static ref readonly int Method() => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var method = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("Method");

                Assert.Empty(method.ReturnTypeCustomModifiers);
                Assert.Empty(method.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Properties_NoModifiers()
        {
            var code = @"
class Test
{
    private int x = 0;
    public ref readonly int Property => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeCustomModifiers);
                Assert.Empty(property.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Properties_Static()
        {
            var code = @"
class Test
{
    private static int x = 0;
    public static ref readonly int Property => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var property = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("Property");

                Assert.Empty(property.TypeCustomModifiers);
                Assert.Empty(property.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Indexers_Parameters_NoModifiers()
        {
            var code = @"
class Test
{
    public int this[ref readonly int x] => x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Indexers_ReturnTypes_NoModifiers()
        {
            var code = @"
class Test
{
    private int x;
    public ref readonly int this[int p] => ref x;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var indexer = module.ContainingAssembly.GetTypeByMetadataName("Test").GetProperty("this[]");

                Assert.Empty(indexer.TypeCustomModifiers);
                Assert.Empty(indexer.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Delegates()
        {
            var code = "delegate ref readonly int D(ref readonly int x);";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var @delegate = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod;

                Assert.Empty(@delegate.ReturnTypeCustomModifiers);
                Assert.Empty(@delegate.RefCustomModifiers);

                var parameter = @delegate.Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Operators()
        {
            var code = @"
public class Test
{
    public static bool operator!(ref readonly Test obj) => false;
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod("op_LogicalNot").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstIsNotWrittenOnRefReadOnlyMembers_Constructors()
        {
            var code = @"
public class Test
{
    public Test(ref readonly int x) { }
}";

            CompileAndVerify(code, verify: false, symbolValidator: module =>
            {
                var parameter = module.ContainingAssembly.GetTypeByMetadataName("Test").GetMethod(".ctor").Parameters.Single();

                Assert.Empty(parameter.CustomModifiers);
                Assert.Empty(parameter.RefCustomModifiers);
            });
        }

        [Fact]
        public void IsConstModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Methods_Parameters()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit TestRef extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance void M (int32& modreq([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed 
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

            CreateStandardCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (8,13): error CS0570: 'TestRef.M(ref int)' is not supported by the language
                //         obj.M(ref value);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M(ref int)").WithLocation(8, 13));
        }

        [Fact]
        public void IsConstModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Methods_ReturnTypes()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit TestRef extends [mscorlib]System.Object
{
    .field private int32 'value'

    .method public hidebysig newslot virtual instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsConst) M () cil managed 
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

            CreateStandardCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (7,25): error CS0570: 'TestRef.M()' is not supported by the language
                //         var value = obj.M();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("TestRef.M()").WithLocation(7, 25));
        }

        [Fact]
        public void IsConstModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Properties()
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

    .property instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsConst) P()
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

            CreateStandardCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (7,25): error CS0570: 'TestRef.P' is not supported by the language
                //         var value = obj.P;
                Diagnostic(ErrorCode.ERR_BindToBogus, "P").WithArguments("TestRef.P").WithLocation(7, 25));
        }

        [Fact]
        public void IsConstModReqIsRejectedOnSignaturesWithoutIsReadOnlyAttribute_Indexers_ReturnTypes()
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

    .property instance int32& modreq([mscorlib]System.Runtime.CompilerServices.IsConst) Item(int32 p)
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

            CreateStandardCompilation(code, references: new[] { CompileIL(ilSource) }).VerifyDiagnostics(
                // (7,21): error CS0570: 'TestRef.this[int]' is not supported by the language
                //         var value = obj[5];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[5]").WithArguments("TestRef.this[int]").WithLocation(7, 21));
        }

        private void AssertSingleIsConstRequiredModifier(ImmutableArray<CustomModifier> modifiers)
        {
            var modifier = modifiers.Single();
            var typeName = WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_IsConst);

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
