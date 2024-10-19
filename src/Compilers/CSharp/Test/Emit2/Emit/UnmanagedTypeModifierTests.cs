// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class UnmanagedTypeModifierTests : CSharpTestBase
    {
        [Fact]
        public void LoadingADifferentModifierTypeForUnmanagedConstraint()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.InAttribute)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

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
        var obj = new TestRef();

        obj.M1<int>();      // valid
        obj.M2<int>();      // invalid
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (9,13): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M2<T>()'. There is no boxing conversion from 'int' to '?'.
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M2<int>").WithArguments("TestRef.M2<T>()", "?", "T", "int").WithLocation(9, 13),
                // (9,13): error CS0570: 'T' is not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BindToBogus, "M2<int>").WithArguments("T").WithLocation(9, 13),
                // (9,13): error CS0648: '' is a type not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BogusType, "M2<int>").WithArguments("").WithLocation(9, 13)
                );
        }

        [Fact]
        public void LoadingUnmanagedTypeModifier_OptionalIsError()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modopt([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

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
        var obj = new TestRef();

        obj.M1<int>();      // valid
        obj.M2<int>();      // invalid
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (9,13): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M2<T>()'. There is no boxing conversion from 'int' to '?'.
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M2<int>").WithArguments("TestRef.M2<T>()", "?", "T", "int").WithLocation(9, 13),
                // (9,13): error CS0570: 'T' is not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BindToBogus, "M2<int>").WithArguments("T").WithLocation(9, 13),
                // (9,13): error CS0648: '' is a type not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BogusType, "M2<int>").WithArguments("").WithLocation(9, 13)
                );
        }

        [Fact]
        public void LoadingUnmanagedTypeModifier_MoreThanOneModifier()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modopt([mscorlib]System.DateTime) modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

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
        var obj = new TestRef();

        obj.M1<int>();      // valid
        obj.M2<int>();      // invalid
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (9,13): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M2<T>()'. There is no boxing conversion from 'int' to '?'.
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M2<int>").WithArguments("TestRef.M2<T>()", "?", "T", "int").WithLocation(9, 13),
                // (9,13): error CS0570: 'T' is not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BindToBogus, "M2<int>").WithArguments("T").WithLocation(9, 13),
                // (9,13): error CS0648: '' is a type not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BogusType, "M2<int>").WithArguments("").WithLocation(9, 13)
                );
        }

        [Fact]
        public void LoadingUnmanagedTypeModifier_ModreqWithoutAttribute()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

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
        var obj = new TestRef();

        obj.M1<int>();      // valid
        obj.M2<int>();      // invalid
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (9,13): error CS0570: 'T' is not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BindToBogus, "M2<int>").WithArguments("T").WithLocation(9, 13)
                );
        }

        [Fact]
        public void LoadingUnmanagedTypeModifier_AttributeWithoutModreq()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M1<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M1

  .method public hidebysig instance void
          M2<valuetype .ctor (class [mscorlib]System.ValueType) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method TestRef::M2

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
        var obj = new TestRef();

        obj.M1<int>();      // valid
        obj.M2<int>();      // invalid
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (9,13): error CS0570: 'T' is not supported by the language
                //         obj.M2<int>();      // invalid
                Diagnostic(ErrorCode.ERR_BindToBogus, "M2<int>").WithArguments("T").WithLocation(9, 13)
                );
        }

        [Fact]
        public void ProperErrorsArePropagatedIfModreqTypeIsNotAvailable_Class()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class ValueType {}
}
class Test<T> where T : unmanaged
{
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (8,25): error CS0518: Predefined type 'System.Runtime.InteropServices.UnmanagedType' is not defined or imported
                // class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.Runtime.InteropServices.UnmanagedType").WithLocation(8, 25));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfModreqTypeIsNotAvailable_Method()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class ValueType {}
}
class Test
{
    public void M<T>() where T : unmanaged {}
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (10,34): error CS0518: Predefined type 'System.Runtime.InteropServices.UnmanagedType' is not defined or imported
                //     public void M<T>() where T : unmanaged {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.Runtime.InteropServices.UnmanagedType").WithLocation(10, 34));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfModreqTypeIsNotAvailable_Delegate()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class ValueType {}
    public class IntPtr {}
    public class MulticastDelegate {}
}
public delegate void D<T>() where T : unmanaged;";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (10,39): error CS0518: Predefined type 'System.Runtime.InteropServices.UnmanagedType' is not defined or imported
                // public delegate void D<T>() where T : unmanaged;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.Runtime.InteropServices.UnmanagedType").WithLocation(10, 39));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfModreqTypeIsNotAvailable_LocalFunction()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class ValueType {}
    public class IntPtr {}
    public class MulticastDelegate {}
}
public class Test
{
    public struct S {}

    public void M()
    {
        void N<T>() where T : unmanaged
        {
        }

        N<S>();
    }
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (16,31): error CS0518: Predefined type 'System.Runtime.InteropServices.UnmanagedType' is not defined or imported
                //         void N<T>() where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.Runtime.InteropServices.UnmanagedType").WithLocation(16, 31));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfValueTypeIsNotAvailable_Class()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}

    namespace Runtime
    {
        namespace InteropServices
        {
            public class UnmanagedType {}
        }
    }
}
class Test<T> where T : unmanaged
{
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (15,25): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                // class Test<T> where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.ValueType").WithLocation(15, 25));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfValueTypeIsNotAvailable_Method()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}

    namespace Runtime
    {
        namespace InteropServices
        {
            public class UnmanagedType {}
        }
    }
}
class Test
{
    public void M<T>() where T : unmanaged {}
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (17,34): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                //     public void M<T>() where T : unmanaged {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.ValueType").WithLocation(17, 34));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfValueTypeIsNotAvailable_Delegate()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public class IntPtr {}
    public class MulticastDelegate {}

    namespace Runtime
    {
        namespace InteropServices
        {
            public class UnmanagedType {}
        }
    }
}
public delegate void M<T>() where T : unmanaged;";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (17,39): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                // public delegate void M<T>() where T : unmanaged;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.ValueType").WithLocation(17, 39));
        }

        [Fact]
        public void ProperErrorsArePropagatedIfValueTypeIsNotAvailable_LocalFunctions()
        {
            var code = @"
namespace System
{
    public class Object {}
    public class Void {}
    public struct Int32 {}

    namespace Runtime
    {
        namespace InteropServices
        {
            public class UnmanagedType {}
        }
    }
}
class Test
{
    public void M()
    {
        void N<T>() where T : unmanaged
        {
        }

        N<int>();
    }
}";

            CreateEmptyCompilation(code).VerifyDiagnostics(
                // (6,19): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                //     public struct Int32 {}
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Int32").WithArguments("System.ValueType").WithLocation(6, 19),
                // (20,31): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                //         void N<T>() where T : unmanaged
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.ValueType").WithLocation(20, 31));
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Virtual_Compilation()
        {
            var reference = CompileAndVerify(@"
public class Parent
{
    public virtual string M<T>() where T : unmanaged => ""Parent"";
}
public class Child : Parent
{
    public override string M<T>() => ""Child"";
}", symbolValidator: module =>
            {
                var parentTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(parentTypeParameter.HasValueTypeConstraint);
                Assert.True(parentTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, parentTypeParameter, module.ContainingAssembly.Name);

                var childTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(childTypeParameter.HasValueTypeConstraint);
                Assert.True(childTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, childTypeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Parent().M<int>());
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { reference.Compilation.EmitToImageReference() }, expectedOutput: @"
Parent
Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Virtual_Reference()
        {
            var parent = CompileAndVerify(@"
public class Parent
{
    public virtual string M<T>() where T : unmanaged => ""Parent"";
}", symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            var child = CompileAndVerify(@"
public class Child : Parent
{
    public override string M<T>() => ""Child"";
}", references: new[] { parent.Compilation.EmitToImageReference() }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Parent().M<int>());
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { parent.Compilation.EmitToImageReference(), child.Compilation.EmitToImageReference() }, expectedOutput: @"
Parent
Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Abstract_Compilation()
        {
            var reference = CompileAndVerify(@"
public abstract class Parent
{
    public abstract string M<T>() where T : unmanaged;
}
public class Child : Parent
{
    public override string M<T>() => ""Child"";
}", symbolValidator: module =>
            {
                var parentTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(parentTypeParameter.HasValueTypeConstraint);
                Assert.True(parentTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, parentTypeParameter, module.ContainingAssembly.Name);

                var childTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(childTypeParameter.HasValueTypeConstraint);
                Assert.True(childTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, childTypeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { reference.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Abstract_Reference()
        {
            var parent = CompileAndVerify(@"
public abstract class Parent
{
    public abstract string M<T>() where T : unmanaged;
}", symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            var child = CompileAndVerify(@"
public class Child : Parent
{
    public override string M<T>() => ""Child"";
}", references: new[] { parent.Compilation.EmitToImageReference() }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { parent.Compilation.EmitToImageReference(), child.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Interface_Implicit_Nonvirtual_Compilation()
        {
            var reference = CompileAndVerify(@"
public interface Parent
{
    string M<T>() where T : unmanaged;
}
public class Child : Parent
{
    public string M<T>() where T : unmanaged => ""Child"";
}", symbolValidator: module =>
            {
                var parentTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(parentTypeParameter.HasValueTypeConstraint);
                Assert.True(parentTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, parentTypeParameter, module.ContainingAssembly.Name);

                var childTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(childTypeParameter.HasValueTypeConstraint);
                Assert.True(childTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, childTypeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { reference.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Interface_Implicit_Nonvirtual_Reference()
        {
            var parent = CompileAndVerify(@"
public interface Parent
{
    string M<T>() where T : unmanaged;
}", symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            var child = CompileAndVerify(@"
public class Child : Parent
{
    public string M<T>() where T : unmanaged => ""Child"";
}", references: new[] { parent.Compilation.EmitToImageReference() }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { parent.Compilation.EmitToImageReference(), child.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Interface_Implicit_Virtual_Compilation()
        {
            var reference = CompileAndVerify(@"
public interface Parent
{
    string M<T>() where T : unmanaged;
}
public class Child : Parent
{
    public virtual string M<T>() where T : unmanaged => ""Child"";
}", symbolValidator: module =>
            {
                var parentTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(parentTypeParameter.HasValueTypeConstraint);
                Assert.True(parentTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, parentTypeParameter, module.ContainingAssembly.Name);

                var childTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(childTypeParameter.HasValueTypeConstraint);
                Assert.True(childTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, childTypeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { reference.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Interface_Implicit_Virtual_Reference()
        {
            var parent = CompileAndVerify(@"
public interface Parent
{
    string M<T>() where T : unmanaged;
}", symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            var child = CompileAndVerify(@"
public class Child : Parent
{
    public virtual string M<T>() where T : unmanaged => ""Child"";
}", references: new[] { parent.Compilation.EmitToImageReference() }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(new Child().M<int>());
    }
}", references: new[] { parent.Compilation.EmitToImageReference(), child.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Interface_Explicit_Compilation()
        {
            var reference = CompileAndVerify(@"
public interface Parent
{
    string M<T>() where T : unmanaged;
}
public class Child : Parent
{
    string Parent.M<T>() => ""Child"";
}", symbolValidator: module =>
            {
                var parentTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(parentTypeParameter.HasValueTypeConstraint);
                Assert.True(parentTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, parentTypeParameter, module.ContainingAssembly.Name);

                var childTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("Parent.M").TypeParameters.Single();
                Assert.True(childTypeParameter.HasValueTypeConstraint);
                Assert.True(childTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, childTypeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M<int>());
    }
}", references: new[] { reference.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToOverrides_Interface_Explicit_Reference()
        {
            var parent = CompileAndVerify(@"
public interface Parent
{
    string M<T>() where T : unmanaged;
}", symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Parent").GetMethod("M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            var child = CompileAndVerify(@"
public class Child : Parent
{
    string Parent.M<T>() => ""Child"";
}", references: new[] { parent.Compilation.EmitToImageReference() }, symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("Parent.M").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        Parent obj = new Child();
        System.Console.WriteLine(obj.M<int>());
    }
}", references: new[] { parent.Compilation.EmitToImageReference(), child.Compilation.EmitToImageReference() }, expectedOutput: "Child");
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToLambda_Compilation()
        {
            CompileAndVerify(@"
public delegate T D<T>() where T : unmanaged;
public class TestRef
{
    public static void Print<T>(D<T> lambda) where T : unmanaged
    {
        System.Console.WriteLine(lambda());
    }
}
public class Program
{
    static void Test<T>(T arg)  where T : unmanaged
    {
        TestRef.Print(() => arg);
    }
    
    public static void Main()
    {
        Test(5);
    }
}",
                expectedOutput: "5",
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
            {
                var delegateTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("D`1").TypeParameters.Single();
                Assert.True(delegateTypeParameter.HasValueTypeConstraint);
                Assert.True(delegateTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, delegateTypeParameter, module.ContainingAssembly.Name);

                var lambdaTypeParameter = module.ContainingAssembly.GetTypeByMetadataName("Program").GetTypeMember("<>c__DisplayClass0_0").TypeParameters.Single();
                Assert.True(lambdaTypeParameter.HasValueTypeConstraint);
                Assert.True(lambdaTypeParameter.HasUnmanagedTypeConstraint);

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, lambdaTypeParameter, module.ContainingAssembly.Name);
            });
        }

        [Fact]
        public void UnmanagedTypeModreqIsCopiedToLambda_Reference()
        {
            var reference = CompileAndVerify(@"
public delegate T D<T>() where T : unmanaged;
public class TestRef
{
    public static void Print<T>(D<T> lambda) where T : unmanaged
    {
        System.Console.WriteLine(lambda());
    }
}", symbolValidator: module =>
            {
                var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("D`1").TypeParameters.Single();
                Assert.True(typeParameter.HasValueTypeConstraint);
                Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                Assert.False(typeParameter.HasConstructorConstraint);   // .ctor  is an artifact of emit, we will ignore it on importing.

                AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
            });

            CompileAndVerify(@"
public class Program
{
    static void Test<T>(T arg)  where T : unmanaged
    {
        TestRef.Print(() => arg);
    }
    
    public static void Main()
    {
        Test(5);
    }
}",
                expectedOutput: "5",
                references: new[] { reference.Compilation.EmitToImageReference() },
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var typeParameter = module.ContainingAssembly.GetTypeByMetadataName("Program").GetTypeMember("<>c__DisplayClass0_0").TypeParameters.Single();
                    Assert.True(typeParameter.HasValueTypeConstraint);
                    Assert.True(typeParameter.HasUnmanagedTypeConstraint);
                    Assert.False(typeParameter.HasConstructorConstraint);  // .ctor  is an artifact of emit, we will ignore it on importing.

                    AttributeValidation.AssertReferencedIsUnmanagedAttribute(Accessibility.Internal, typeParameter, module.ContainingAssembly.Name);
                });
        }

        [Fact]
        public void DuplicateUnmanagedTypeInReferences()
        {
            var refCode = @"
namespace System.Runtime.InteropServices
{
    public class UnmanagedType {}
}";

            var ref1 = CreateCompilation(refCode).EmitToImageReference();
            var ref2 = CreateCompilation(refCode).EmitToImageReference();

            var user = @"
public class Test<T> where T : unmanaged
{
}";

            CreateCompilation(user, references: new[] { ref1, ref2 }).VerifyDiagnostics();
        }

        [Fact]
        public void DuplicateUnmanagedTypeInReferences_NoTypeInCorlib()
        {
            var corlib_cs = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public struct Boolean { }
    public class String { }
    public class ValueType { }
    public struct Void { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
";

            var corlibWithoutUnmanagedTypeRef = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

            var refCode = @"
namespace System.Runtime.InteropServices
{
    public class UnmanagedType {}
}";

            var ref1 = CreateEmptyCompilation(refCode, references: new[] { corlibWithoutUnmanagedTypeRef }).EmitToImageReference();
            var ref2 = CreateEmptyCompilation(refCode, references: new[] { corlibWithoutUnmanagedTypeRef }).EmitToImageReference();

            var user = @"
public class Test<T> where T : unmanaged
{
}";

            CreateEmptyCompilation(user, references: new[] { ref1, ref2, corlibWithoutUnmanagedTypeRef })
                .VerifyDiagnostics(
                    // (2,32): error CS0518: Predefined type 'System.Runtime.InteropServices.UnmanagedType' is not defined or imported
                    // public class Test<T> where T : unmanaged
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "unmanaged").WithArguments("System.Runtime.InteropServices.UnmanagedType").WithLocation(2, 32));
        }

        [Fact]
        public void UnmanagedConstraintWithClassConstraint_IL()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<class (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
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
        new TestRef().M<int>();
        new TestRef().M<string>();
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (6,23): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'TestRef.M<T>()'
                //         new TestRef().M<int>();
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "M<int>").WithArguments("TestRef.M<T>()", "T", "int").WithLocation(6, 23),
                // (7,23): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M<T>()'. There is no implicit reference conversion from 'string' to 'System.ValueType'.
                //         new TestRef().M<string>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<string>").WithArguments("TestRef.M<T>()", "System.ValueType", "T", "string").WithLocation(7, 23),
                // (7,23): error CS0570: 'T' is not supported by the language
                //         new TestRef().M<string>();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M<string>").WithArguments("T").WithLocation(7, 23)
                );
        }

        [Fact]
        public void UnmanagedConstraintWithConstructorConstraint_IL()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<.ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
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
        new TestRef().M<int>();
        new TestRef().M<string>();
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (6,23): error CS0570: 'T' is not supported by the language
                //         new TestRef().M<int>();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M<int>").WithArguments("T").WithLocation(6, 23),
                // (7,23): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M<T>()'. There is no implicit reference conversion from 'string' to 'System.ValueType'.
                //         new TestRef().M<string>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<string>").WithArguments("TestRef.M<T>()", "System.ValueType", "T", "string").WithLocation(7, 23),
                // (7,23): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'TestRef.M<T>()'
                //         new TestRef().M<string>();
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "M<string>").WithArguments("TestRef.M<T>()", "T", "string").WithLocation(7, 23),
                // (7,23): error CS0570: 'T' is not supported by the language
                //         new TestRef().M<string>();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M<string>").WithArguments("T").WithLocation(7, 23));
        }

        [Fact]
        public void UnmanagedConstraintWithoutValueTypeConstraint_IL()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<(class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
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
        new TestRef().M<int>();
        new TestRef().M<string>();
    }
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (6,23): error CS0570: 'T' is not supported by the language
                //         new TestRef().M<int>();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M<int>").WithArguments("T").WithLocation(6, 23),
                // (7,23): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M<T>()'. There is no implicit reference conversion from 'string' to 'System.ValueType'.
                //         new TestRef().M<string>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<string>").WithArguments("TestRef.M<T>()", "System.ValueType", "T", "string").WithLocation(7, 23),
                // (7,23): error CS0570: 'T' is not supported by the language
                //         new TestRef().M<string>();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M<string>").WithArguments("T").WithLocation(7, 23));
        }

        [Fact]
        public void UnmanagedConstraintWithTypeConstraint_IL()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<valuetype .ctor (class [mscorlib]System.IComparable, class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
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
        // OK
        new TestRef().M<int>();

        // Not IComparable
        new TestRef().M<S1>();
    }

    struct S1{}
}";

            CreateCompilation(code, references: new[] { reference }).VerifyDiagnostics(
                // (10,23): error CS0315: The type 'Test.S1' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M<T>()'. There is no boxing conversion from 'Test.S1' to 'System.IComparable'.
                //         new TestRef().M<S1>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<S1>").WithArguments("TestRef.M<T>()", "System.IComparable", "T", "Test.S1").WithLocation(10, 23)
                );
        }

        [Fact]
        public void UnmanagedConstraintWithNoCtorConstraint_IL()
        {
            var ilSource = IsUnmanagedAttributeIL + @"
.class public auto ansi beforefieldinit TestRef
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          M<valuetype (class [mscorlib]System.IComparable, class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
  {
    .param type T
    .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
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
        // OK
        new TestRef().M<int>();

        // Not IComparable
        new TestRef().M<S1>();
    }

    struct S1{}
}";

            var c = CreateCompilation(code, references: new[] { reference });

            c.VerifyDiagnostics(
                // (10,23): error CS0315: The type 'Test.S1' cannot be used as type parameter 'T' in the generic type or method 'TestRef.M<T>()'. There is no boxing conversion from 'Test.S1' to 'System.IComparable'.
                //         new TestRef().M<S1>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M<S1>").WithArguments("TestRef.M<T>()", "System.IComparable", "T", "Test.S1").WithLocation(10, 23)
                );

            var typeParameter = c.GlobalNamespace.GetTypeMember("TestRef").GetMethod("M").TypeParameters.Single();
            Assert.True(typeParameter.HasUnmanagedTypeConstraint);
            Assert.True(typeParameter.HasValueTypeConstraint);
            Assert.False(typeParameter.HasReferenceTypeConstraint);
            Assert.False(typeParameter.HasConstructorConstraint);
        }

        [Fact]
        [WorkItem(25654, "https://github.com/dotnet/roslyn/issues/25654")]
        public void UnmanagedConstraint_PointersTypeInference()
        {
            var code = @"
unsafe class Program
{
    static void M<T>(T* a) where T : unmanaged
    {
        System.Console.WriteLine(typeof(T).FullName);
    }
    static void Main()
    {
        int x = 5;
        M(&x);

        double y = 5.5;
        M(&y);
    }
}";

            CompileAndVerify(code, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: @"
System.Int32
System.Double
")
                .VerifyIL("Program.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (int V_0, //x
                double V_1) //y
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  conv.u
  IL_0005:  call       ""void Program.M<int>(int*)""
  IL_000a:  ldc.r8     5.5
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  conv.u
  IL_0017:  call       ""void Program.M<double>(double*)""
  IL_001c:  ret
}");
        }

        private const string IsUnmanagedAttributeIL = @"
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

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsUnmanagedAttribute
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
