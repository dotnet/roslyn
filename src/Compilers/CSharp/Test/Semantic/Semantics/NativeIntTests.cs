// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class NativeIntTests : CSharpTestBase
    {
        private static readonly SymbolDisplayFormat FormatWithSpecialTypes = SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        [Fact]
        public void LanguageVersion()
        {
            var source =
@"interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,5): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(3, 5),
                // (3,14): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(3, 14),
                // (3,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(3, 22));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        /// <summary>
        /// System.IntPtr and System.UIntPtr definitions from metadata.
        /// </summary>
        [Fact]
        public void TypeDefinitions_FromMetadata()
        {
            var source =
@"interface I
{
    void F1(System.IntPtr x, nint y);
    void F2(System.UIntPtr x, nuint y);
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var method = comp.GetMember<MethodSymbol>("I.F1");
            Assert.Equal("void I.F1(System.IntPtr x, nint y)", method.ToDisplayString(FormatWithSpecialTypes));
            VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: true);

            method = comp.GetMember<MethodSymbol>("I.F2");
            Assert.Equal("void I.F2(System.UIntPtr x, nuint y)", method.ToDisplayString(FormatWithSpecialTypes));
            VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: false);
        }

        /// <summary>
        /// System.IntPtr and System.UIntPtr definitions from source.
        /// </summary>
        [Fact]
        public void TypeDefinitions_FromSource()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct IntPtr { }
    public struct UIntPtr { }
}";
            var sourceB =
@"interface I
{
    void F1(System.IntPtr x, nint y);
    void F2(System.UIntPtr x, nuint y);
}";
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var method = comp.GetMember<MethodSymbol>("I.F1");
                Assert.Equal("void I.F1(System.IntPtr x, nint y)", method.ToDisplayString(FormatWithSpecialTypes));
                VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: true);

                method = comp.GetMember<MethodSymbol>("I.F2");
                Assert.Equal("void I.F2(System.UIntPtr x, nuint y)", method.ToDisplayString(FormatWithSpecialTypes));
                VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: false);
            }
        }

        private static void VerifyTypes(INamedTypeSymbol underlyingType, INamedTypeSymbol nativeIntegerType, bool signed)
        {
            var specialType = signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr;

            Assert.Equal(specialType, underlyingType.SpecialType);
            Assert.Equal(SymbolKind.NamedType, underlyingType.Kind);
            Assert.Equal(TypeKind.Struct, underlyingType.TypeKind);
            Assert.Same(underlyingType, underlyingType.ConstructedFrom);

            Assert.Equal(specialType, nativeIntegerType.SpecialType);
            Assert.Equal(SymbolKind.NamedType, nativeIntegerType.Kind);
            Assert.Equal(TypeKind.Struct, nativeIntegerType.TypeKind);
            Assert.Same(nativeIntegerType, nativeIntegerType.ConstructedFrom);

            Assert.Empty(nativeIntegerType.MemberNames);
            Assert.Empty(nativeIntegerType.GetTypeMembers());
            Assert.Empty(nativeIntegerType.GetMembers());

            // PROTOTYPE: Include certain interfaces defined on the underlying underlyingType.
            Assert.Empty(nativeIntegerType.Interfaces);

            Assert.NotSame(underlyingType, nativeIntegerType);
            Assert.Equal(underlyingType, nativeIntegerType);
            Assert.Equal(nativeIntegerType, underlyingType);
            Assert.Equal(underlyingType.GetHashCode(), nativeIntegerType.GetHashCode());
        }

        private static void VerifyTypes(NamedTypeSymbol underlyingType, NamedTypeSymbol nativeIntegerType, bool signed)
        {
            var specialType = signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr;

            Assert.Equal(specialType, underlyingType.SpecialType);
            Assert.Equal(SymbolKind.NamedType, underlyingType.Kind);
            Assert.Equal(TypeKind.Struct, underlyingType.TypeKind);
            Assert.Same(underlyingType, underlyingType.ConstructedFrom);
            Assert.False(underlyingType.IsNativeInt);

            Assert.Equal(specialType, nativeIntegerType.SpecialType);
            Assert.Equal(SymbolKind.NamedType, nativeIntegerType.Kind);
            Assert.Equal(TypeKind.Struct, nativeIntegerType.TypeKind);
            Assert.Same(nativeIntegerType, nativeIntegerType.ConstructedFrom);
            Assert.True(nativeIntegerType.IsNativeInt);

            Assert.Empty(nativeIntegerType.MemberNames);
            Assert.Empty(nativeIntegerType.GetTypeMembers());
            Assert.Empty(nativeIntegerType.GetMembers());

            // PROTOTYPE: Include certain interfaces defined on the underlying underlyingType.
            Assert.Empty(nativeIntegerType.InterfacesNoUseSiteDiagnostics());

            Assert.Same(underlyingType, underlyingType.AsNativeInt(false));
            Assert.Same(nativeIntegerType, nativeIntegerType.AsNativeInt(true));
            Assert.Equal(nativeIntegerType, underlyingType.AsNativeInt(true));
            Assert.Equal(underlyingType, nativeIntegerType.AsNativeInt(false));
            VerifyEqualButDistinct(underlyingType, underlyingType.AsNativeInt(true));
            VerifyEqualButDistinct(nativeIntegerType, nativeIntegerType.AsNativeInt(false));
            VerifyEqualButDistinct(underlyingType, nativeIntegerType);

            VerifyTypes(underlyingType.GetPublicSymbol(), nativeIntegerType.GetPublicSymbol(), signed);
        }

        private static void VerifyEqualButDistinct(NamedTypeSymbol underlyingType, NamedTypeSymbol nativeIntegerType)
        {
            Assert.NotSame(underlyingType, nativeIntegerType);
            Assert.Equal(underlyingType, nativeIntegerType);
            Assert.Equal(nativeIntegerType, underlyingType);
            Assert.True(underlyingType.Equals(nativeIntegerType, TypeCompareKind.ConsiderEverything));
            Assert.True(nativeIntegerType.Equals(underlyingType, TypeCompareKind.ConsiderEverything));
            Assert.Equal(underlyingType.GetHashCode(), nativeIntegerType.GetHashCode());
        }

        [Fact]
        public void MissingTypes()
        {
            var sourceA =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
}";
            var sourceB =
@"interface I
{
    void F1(System.IntPtr x, nint y);
    void F2(System.UIntPtr x, nuint y);
}";
            var diagnostics = new[]
            {
                // (3,20): error CS0234: The underlyingType or namespace name 'IntPtr' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //     void F1(System.IntPtr x, nint y);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IntPtr").WithArguments("IntPtr", "System").WithLocation(3, 20),
                // (3,30): error CS0518: Predefined underlyingType 'System.IntPtr' is not defined or imported
                //     void F1(System.IntPtr x, nint y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nint").WithArguments("System.IntPtr").WithLocation(3, 30),
                // (4,20): error CS0234: The underlyingType or namespace name 'UIntPtr' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //     void F2(System.UIntPtr x, nuint y);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "UIntPtr").WithArguments("UIntPtr", "System").WithLocation(4, 20),
                // (4,31): error CS0518: Predefined underlyingType 'System.UIntPtr' is not defined or imported
                //     void F2(System.UIntPtr x, nuint y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nuint").WithArguments("System.UIntPtr").WithLocation(4, 31)
            };

            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
            verify(comp);

            comp = CreateEmptyCompilation(sourceA);
            comp.VerifyDiagnostics();
            var ref1 = comp.ToMetadataReference();
            var ref2 = comp.EmitToImageReference();

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref1 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
            verify(comp);

            comp = CreateEmptyCompilation(sourceB, references: new[] { ref2 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(diagnostics);
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var method = comp.GetMember<MethodSymbol>("I.F1");
                Assert.Equal("void I.F1(System.IntPtr x, nint y)", method.ToDisplayString(FormatWithSpecialTypes));
                verifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: true);

                method = comp.GetMember<MethodSymbol>("I.F2");
                Assert.Equal("void I.F2(System.UIntPtr x, nuint y)", method.ToDisplayString(FormatWithSpecialTypes));
                verifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: false);
            }

            static void verifyTypes(NamedTypeSymbol underlyingType, NamedTypeSymbol nativeIntegerType, bool signed)
            {
                Assert.Equal(SpecialType.None, underlyingType.SpecialType);
                Assert.Equal(SymbolKind.ErrorType, underlyingType.Kind);
                Assert.Equal(TypeKind.Error, underlyingType.TypeKind);
                Assert.False(underlyingType.IsNativeInt);

                var specialType = signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr;
                Assert.Equal(specialType, nativeIntegerType.SpecialType);
                Assert.Equal(SymbolKind.ErrorType, nativeIntegerType.Kind);
                Assert.Equal(TypeKind.Error, nativeIntegerType.TypeKind);
                Assert.True(nativeIntegerType.IsNativeInt);
                Assert.Same(nativeIntegerType, nativeIntegerType.AsNativeInt(true));
                VerifyEqualButDistinct(nativeIntegerType, nativeIntegerType.AsNativeInt(false));
            }
        }

        // PROTOTYPE: Test:
        // - @nint
        // - Type.nint, Namespace.nint
        // - BindNonGenericSimpleNamespaceOrTypeOrAliasSymbol has the comment "dynamic not allowed as an attribute underlyingType". Does that apply to "nint"?
        // - BindNonGenericSimpleNamespaceOrTypeOrAliasSymbol checks IsViableType(result)
        // - Use-site diagnostics (basically any use-site diagnostics from IntPtr/UIntPtr)

        [Fact]
        public void ClassName()
        {
            var source =
@"class nint
{
}
interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(6, 22));
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var nodes = tree.GetRoot().DescendantNodes().ToArray();
                var model = comp.GetSemanticModel(tree);
                var underlyingType = model.GetDeclaredSymbol(nodes.OfType<ClassDeclarationSyntax>().Single());
                Assert.Equal("nint", underlyingType.ToTestDisplayString());
                Assert.Equal(SpecialType.None, underlyingType.SpecialType);
                var method = model.GetDeclaredSymbol(nodes.OfType<MethodDeclarationSyntax>().Single());
                Assert.Equal("nint I.Add(nint x, System.UIntPtr y)", method.ToTestDisplayString());
                var underlyingType0 = method.Parameters[0].Type.GetSymbol<NamedTypeSymbol>();
                var underlyingType1 = method.Parameters[1].Type.GetSymbol<NamedTypeSymbol>();
                Assert.Equal(SpecialType.None, underlyingType0.SpecialType);
                Assert.False(underlyingType0.IsNativeInt);
                Assert.Equal(SpecialType.System_UIntPtr, underlyingType1.SpecialType);
                Assert.True(underlyingType1.IsNativeInt);
            }
        }

        [Fact]
        public void AliasName()
        {
            var source =
@"using nint = System.Int16;
interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(4, 22));
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var nodes = tree.GetRoot().DescendantNodes().ToArray();
                var model = comp.GetSemanticModel(tree);
                var method = model.GetDeclaredSymbol(nodes.OfType<MethodDeclarationSyntax>().Single());
                Assert.Equal("System.Int16 I.Add(System.Int16 x, System.UIntPtr y)", method.ToTestDisplayString());
                var underlyingType0 = method.Parameters[0].Type.GetSymbol<NamedTypeSymbol>();
                var underlyingType1 = method.Parameters[1].Type.GetSymbol<NamedTypeSymbol>();
                Assert.Equal(SpecialType.System_Int16, underlyingType0.SpecialType);
                Assert.False(underlyingType0.IsNativeInt);
                Assert.Equal(SpecialType.System_UIntPtr, underlyingType1.SpecialType);
                Assert.True(underlyingType1.IsNativeInt);
            }
        }

        // PROTOTYPE: nint and nuint should be allowed.
        [Fact]
        public void MemberName()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = nint.Equals(0, 0);
        _ = nuint.Equals(0, 0);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,13): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nint.Equals(0, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 13),
                // (6,13): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nuint.Equals(0, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 13));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nint.Equals(0, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 13),
                // (6,13): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nuint.Equals(0, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 13));
        }

        // PROTOTYPE: nint and nuint should be allowed.
        [Fact]
        public void NameOf()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = nameof(nint);
        _ = nameof(nuint);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,20): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nameof(nint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 20),
                // (6,20): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nameof(nuint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 20));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nameof(nint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 20),
                // (6,20): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nameof(nuint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 20));
        }

        /// <summary>
        /// sizeof(IntPtr) and sizeof(nint) require compiling with /unsafe.
        /// </summary>
        [Fact]
        public void SizeOf_01()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = sizeof(System.IntPtr);
        _ = sizeof(System.UIntPtr);
        _ = sizeof(nint);
        _ = sizeof(nuint);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS0233: 'IntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.IntPtr)").WithArguments("System.IntPtr").WithLocation(5, 13),
                // (6,13): error CS0233: 'UIntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.UIntPtr)").WithArguments("System.UIntPtr").WithLocation(6, 13),
                // (7,13): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(nint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nint)").WithArguments("nint").WithLocation(7, 13),
                // (8,13): error CS0233: 'nuint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(nuint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nuint)").WithArguments("nuint").WithLocation(8, 13));
        }

        [Fact]
        public void SizeOf_02()
        {
            var source =
@"using System;
class Program
{
    unsafe static void Main()
    {
        Console.Write(sizeof(System.IntPtr));
        Console.Write(sizeof(System.UIntPtr));
        Console.Write(sizeof(nint));
        Console.Write(sizeof(nuint));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.RegularPreview);
            int size = IntPtr.Size;
            var verifier = CompileAndVerify(comp, expectedOutput: $"{size}{size}{size}{size}");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       45 (0x2d)
  .maxstack  1
  IL_0000:  sizeof     ""System.IntPtr""
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  sizeof     ""System.UIntPtr""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  sizeof     ""System.IntPtr""
  IL_001c:  call       ""void System.Console.Write(int)""
  IL_0021:  sizeof     ""System.UIntPtr""
  IL_0027:  call       ""void System.Console.Write(int)""
  IL_002c:  ret
}");
        }

        [Fact]
        public void SizeOf_03()
        {
            var source =
@"using System.Collections.Generic;
unsafe class Program
{
    static IEnumerable<int> F()
    {
        yield return sizeof(nint);
        yield return sizeof(nuint);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,22): error CS1629: Unsafe code may not appear in iterators
                //         yield return sizeof(nint);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "sizeof(nint)").WithLocation(6, 22),
                // (7,22): error CS1629: Unsafe code may not appear in iterators
                //         yield return sizeof(nuint);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "sizeof(nuint)").WithLocation(7, 22));
        }

        [Fact]
        public void SizeOf_04()
        {
            var source =
@"unsafe class Program
{
    const int A = sizeof(System.IntPtr);
    const int B = sizeof(System.UIntPtr);
    const int C = sizeof(nint);
    const int D = sizeof(nuint);
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,19): error CS0133: The expression being assigned to 'Program.A' must be constant
                //     const int A = sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(System.IntPtr)").WithArguments("Program.A").WithLocation(3, 19),
                // (4,19): error CS0133: The expression being assigned to 'Program.B' must be constant
                //     const int B = sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(System.UIntPtr)").WithArguments("Program.B").WithLocation(4, 19),
                // (5,19): error CS0133: The expression being assigned to 'Program.C' must be constant
                //     const int C = sizeof(nint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nint)").WithArguments("Program.C").WithLocation(5, 19),
                // (6,19): error CS0133: The expression being assigned to 'Program.D' must be constant
                //     const int D = sizeof(nuint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nuint)").WithArguments("Program.D").WithLocation(6, 19));
        }

        /// <summary>
        /// Verify there is exactly one built in operator for { nint, nuint, nint?, nuint? }
        /// for each operator kind.
        /// </summary>
        [Fact]
        public void BinaryOperators_BuiltInOperators()
        {
            var source = "";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            verifyOperators(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verifyOperators(comp);

            static void verifyOperators(CSharpCompilation comp)
            {
                var operatorKinds = new[]
                {
                    BinaryOperatorKind.Addition,
                    BinaryOperatorKind.Subtraction,
                    BinaryOperatorKind.Multiplication,
                    BinaryOperatorKind.Division,
                    BinaryOperatorKind.Remainder,
                    BinaryOperatorKind.LessThan,
                    BinaryOperatorKind.LessThanOrEqual,
                    BinaryOperatorKind.GreaterThan,
                    BinaryOperatorKind.GreaterThanOrEqual,
                    BinaryOperatorKind.LeftShift,
                    BinaryOperatorKind.RightShift,
                    BinaryOperatorKind.Equal,
                    BinaryOperatorKind.NotEqual,
                    BinaryOperatorKind.Or,
                    BinaryOperatorKind.And,
                    BinaryOperatorKind.Xor,
                };

                foreach (var operatorKind in operatorKinds)
                {
                    var builder = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
                    comp.builtInOperators.GetSimpleBuiltInOperators(operatorKind, builder);
                    var operators = builder.ToImmutableAndFree();
                    _ = operators.Single(op => isNativeInt(op.LeftType, signed: true));
                    _ = operators.Single(op => isNativeInt(op.LeftType, signed: false));
                    _ = operators.Single(op => isNullableNativeInt(op.LeftType, signed: true));
                    _ = operators.Single(op => isNullableNativeInt(op.LeftType, signed: false));
                }

                static bool isNativeInt(TypeSymbol underlyingType, bool signed)
                {
                    return underlyingType is NamedTypeSymbol { IsNativeInt: true } &&
                        underlyingType.SpecialType == (signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr);
                }

                static bool isNullableNativeInt(TypeSymbol underlyingType, bool signed)
                {
                    return underlyingType.IsNullableType() && isNativeInt(underlyingType.GetNullableUnderlyingType(), signed);
                }
            }
        }

        [Fact]
        public void Conversions()
        {
            string convNone =
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}";
            static string conv(string conversion) =>
$@"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conversion}
  IL_0002:  ret
}}";
            static string convFromNullableT(string conversion, string sourceType) =>
$@"{{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""{sourceType} {sourceType}?.Value.get""
  IL_0007:  {conversion}
  IL_0008:  ret
}}";
            static string convToNullableT(string conversion, string destType) =>
$@"{{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conversion}
  IL_0002:  newobj     ""{destType}?..ctor({destType})""
  IL_0007:  ret
}}";
            static string convFromToNullableT(string conversion, string sourceType, string destType) =>
$@"{{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init ({sourceType}? V_0,
                {destType}? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{destType}?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""{sourceType} {sourceType}?.GetValueOrDefault()""
  IL_001c:  {conversion}
  IL_001d:  newobj     ""{destType}?..ctor({destType})""
  IL_0022:  ret
}}";
            void conversions(string sourceType, string destType, string expectedImplicitIL, string expectedExplicitIL, string expectedCheckedIL = null)
            {
                Convert(
                    sourceType,
                    destType,
                    expectedImplicitIL,
                    skipTypeChecks: usesIntPtrOrUIntPtr(sourceType) || usesIntPtrOrUIntPtr(destType), // PROTOTYPE: Not distinguishing IntPtr from nint.
                    useExplicitCast: false,
                    useChecked: false,
                    expectedImplicitIL is null ?
                        expectedExplicitIL is null ? ErrorCode.ERR_NoImplicitConv : ErrorCode.ERR_NoImplicitConvCast :
                        0);
                Convert(
                    sourceType,
                    destType,
                    expectedExplicitIL,
                    skipTypeChecks: true,
                    useExplicitCast: true,
                    useChecked: false,
                    expectedExplicitIL is null ? ErrorCode.ERR_NoExplicitConv : 0);
                expectedCheckedIL ??= expectedExplicitIL;
                Convert(
                    sourceType,
                    destType,
                    expectedCheckedIL,
                    skipTypeChecks: true,
                    useExplicitCast: true,
                    useChecked: true,
                    expectedCheckedIL is null ? ErrorCode.ERR_NoExplicitConv : 0);

                static bool usesIntPtrOrUIntPtr(string underlyingType) => underlyingType.Contains("IntPtr");
            }

            conversions(sourceType: "object", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""System.IntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nint", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            conversions(sourceType: "bool", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "byte", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "ushort", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            conversions(sourceType: "uint", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "long", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "ulong", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "nint", destType: "nint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            conversions(sourceType: "float", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "double", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            conversions(sourceType: "decimal", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.i
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.i
  IL_0007:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "bool?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"));
            conversions(sourceType: "byte?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"));
            conversions(sourceType: "ushort?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"));
            conversions(sourceType: "uint?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "uint"));
            conversions(sourceType: "long?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "long"));
            conversions(sourceType: "ulong?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "ulong"));
            conversions(sourceType: "nint?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));
            conversions(sourceType: "float?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "float"));
            conversions(sourceType: "double?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "double"));
            conversions(sourceType: "decimal?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.i
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.i
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.IntPtr System.IntPtr?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "System.UIntPtr?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "object", destType: "nint?", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nint?", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nint?..ctor(nint)""
  IL_000b:  ret
}");
            conversions(sourceType: "bool", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "sbyte", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "byte", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "short", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "ushort", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            conversions(sourceType: "int", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            conversions(sourceType: "uint", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "long", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "ulong", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "nint", destType: "nint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            conversions(sourceType: "float", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "double", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            conversions(sourceType: "decimal", destType: "nint?", expectedImplicitIL: null,
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.i
  IL_0007:  newobj     ""nint?..ctor(nint)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.i
  IL_0007:  newobj     ""nint?..ctor(nint)""
  IL_000c:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}");
            conversions(sourceType: "System.UIntPtr", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "bool?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "char", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nint"));
            conversions(sourceType: "sbyte?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"));
            conversions(sourceType: "byte?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nint"));
            conversions(sourceType: "short?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "short", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nint"));
            conversions(sourceType: "ushort?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nint"));
            conversions(sourceType: "int?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "int", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "int", "nint"));
            conversions(sourceType: "uint?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "uint", "nint"));
            conversions(sourceType: "long?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "long", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "long", "nint"));
            conversions(sourceType: "ulong?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "ulong", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "ulong", "nint"));
            conversions(sourceType: "nint?", destType: "nint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "nuint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            conversions(sourceType: "float?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "float", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "float", "nint"));
            conversions(sourceType: "double?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "double", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "double", "nint"));
            conversions(sourceType: "decimal?", destType: "nint?", null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""long decimal.op_Explicit(decimal)""
  IL_0021:  conv.i
  IL_0022:  newobj     ""nint?..ctor(nint)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""long decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.i
  IL_0022:  newobj     ""nint?..ctor(nint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "System.UIntPtr?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.IntPtr""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.IntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "nint", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nint", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "char", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "nint", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1"));
            conversions(sourceType: "nint", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1"));
            conversions(sourceType: "nint", destType: "short", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2"));
            conversions(sourceType: "nint", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            conversions(sourceType: "nint", destType: "int", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4"));
            conversions(sourceType: "nint", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4"));
            conversions(sourceType: "nint", destType: "long", expectedImplicitIL: conv("conv.i8"), expectedExplicitIL: conv("conv.i8"));
            conversions(sourceType: "nint", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i8"), expectedCheckedIL: conv("conv.ovf.u8")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            conversions(sourceType: "nint", destType: "float", expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            conversions(sourceType: "nint", destType: "double", expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            conversions(sourceType: "nint", destType: "decimal",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  ret
}");
            conversions(sourceType: "nint", destType: "System.IntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nint", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "char"));
            conversions(sourceType: "nint", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1", "sbyte"));
            conversions(sourceType: "nint", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1", "byte"));
            conversions(sourceType: "nint", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2", "short"));
            conversions(sourceType: "nint", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "ushort"));
            conversions(sourceType: "nint", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4", "int"));
            conversions(sourceType: "nint", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4", "uint"));
            conversions(sourceType: "nint", destType: "long?", expectedImplicitIL: convToNullableT("conv.i8", "long"), expectedExplicitIL: convToNullableT("conv.i8", "long"));
            conversions(sourceType: "nint", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i8", "ulong"), expectedCheckedIL: convToNullableT("conv.ovf.u8", "ulong")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            conversions(sourceType: "nint", destType: "float?", expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "nint", destType: "double?", expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "nint", destType: "decimal?",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  call       ""decimal decimal.op_Implicit(long)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}");
            conversions(sourceType: "nint", destType: "System.IntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nint", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint?""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint?""
  IL_0006:  ret
}");
            conversions(sourceType: "nint?", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_000c:  ret
}");
            conversions(sourceType: "nint?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "nint?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1", "nint"));
            conversions(sourceType: "nint?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1", "nint"));
            conversions(sourceType: "nint?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2", "nint"));
            conversions(sourceType: "nint?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            conversions(sourceType: "nint?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4", "nint"));
            conversions(sourceType: "nint?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4", "nint"));
            conversions(sourceType: "nint?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"));
            conversions(sourceType: "nint?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u8", "nint")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            conversions(sourceType: "nint?", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nint"));
            conversions(sourceType: "nint?", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nint"));
            conversions(sourceType: "nint?", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  conv.i8
  IL_0008:  call       ""decimal decimal.op_Implicit(long)""
  IL_000d:  ret
}");
            conversions(sourceType: "nint?", destType: "System.IntPtr", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nint?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nint?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "char"));
            conversions(sourceType: "nint?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1", "nint", "sbyte"));
            conversions(sourceType: "nint?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1", "nint", "byte"));
            conversions(sourceType: "nint?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2", "nint", "short"));
            conversions(sourceType: "nint?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "ushort"));
            conversions(sourceType: "nint?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4", "nint", "int"));
            conversions(sourceType: "nint?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4", "nint", "uint"));
            conversions(sourceType: "nint?", destType: "long?", expectedImplicitIL: convFromToNullableT("conv.i8", "nint", "long"), expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "long"));
            conversions(sourceType: "nint?", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "ulong"), expectedCheckedIL: convFromToNullableT("conv.ovf.u8", "nint", "ulong")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            conversions(sourceType: "nint?", destType: "float?", expectedImplicitIL: convFromToNullableT("conv.r4", "nint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nint", "float"), null);
            conversions(sourceType: "nint?", destType: "double?", expectedImplicitIL: convFromToNullableT("conv.r8", "nint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nint", "double"), null);
            conversions(sourceType: "nint?", destType: "decimal?",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  conv.i8
  IL_001d:  call       ""decimal decimal.op_Implicit(long)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  conv.i8
  IL_001d:  call       ""decimal decimal.op_Implicit(long)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "nint?", destType: "System.IntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nint?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "object", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""System.UIntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nuint", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            conversions(sourceType: "bool", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "sbyte", destType: "nuint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "byte", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "short", destType: "nuint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ushort", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "int", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "uint", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            conversions(sourceType: "long", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "ulong", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u.un"));
            conversions(sourceType: "nint", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "nuint", destType: "nuint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "double", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            conversions(sourceType: "decimal", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.u
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.u.un
  IL_0007:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "nuint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "bool?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            conversions(sourceType: "sbyte?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "sbyte"));
            conversions(sourceType: "byte?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            conversions(sourceType: "short?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "short"));
            conversions(sourceType: "ushort?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            conversions(sourceType: "int?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "int"));
            conversions(sourceType: "uint?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"));
            conversions(sourceType: "long?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "long"));
            conversions(sourceType: "ulong?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.u.un", "ulong"));
            conversions(sourceType: "nint?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));
            conversions(sourceType: "nuint?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "float?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "float"));
            conversions(sourceType: "double?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "double"));
            conversions(sourceType: "decimal?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.u
  IL_000d:  ret
}",
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  conv.ovf.u.un
  IL_000d:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "object", destType: "nuint?", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint?""
  IL_0006:  ret
}");
            conversions(sourceType: "string", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "void*", destType: "nuint?", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nuint?..ctor(nuint)""
  IL_000b:  ret
}");
            conversions(sourceType: "bool", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "sbyte", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.i", "nuint"), expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "byte", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "short", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.i", "nuint"), expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ushort", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "int", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "uint", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            conversions(sourceType: "long", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "ulong", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u.un", "nuint"));
            conversions(sourceType: "nint", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "nuint", destType: "nuint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "float", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "double", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            conversions(sourceType: "decimal", destType: "nuint?", expectedImplicitIL: null,
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.u
  IL_0007:  newobj     ""nuint?..ctor(nuint)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  conv.ovf.u.un
  IL_0007:  newobj     ""nuint?..ctor(nuint)""
  IL_000c:  ret
}");
            conversions(sourceType: "System.IntPtr", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr", destType: "nuint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            conversions(sourceType: "bool?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "char?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "char", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nuint"));
            conversions(sourceType: "sbyte?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "sbyte", "nuint"));
            conversions(sourceType: "byte?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nuint"));
            conversions(sourceType: "short?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "short", "nuint"));
            conversions(sourceType: "ushort?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"));
            conversions(sourceType: "int?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "int", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "int", "nuint"));
            conversions(sourceType: "uint?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "uint", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nuint"));
            conversions(sourceType: "long?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "long", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "long", "nuint"));
            conversions(sourceType: "ulong?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "ulong", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u.un", "ulong", "nuint"));
            conversions(sourceType: "nint?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "nint", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            conversions(sourceType: "nuint?", destType: "nuint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "float?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "float", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "float", "nuint"));
            conversions(sourceType: "double?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "double", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "double", "nuint"));
            conversions(sourceType: "decimal?", destType: "nuint?", null,
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0021:  conv.u
  IL_0022:  newobj     ""nuint?..ctor(nuint)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (decimal? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool decimal?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_001c:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0021:  conv.ovf.u.un
  IL_0022:  newobj     ""nuint?..ctor(nuint)""
  IL_0027:  ret
}");
            conversions(sourceType: "System.IntPtr?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "System.UIntPtr?", destType: "nuint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.UIntPtr""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""System.UIntPtr""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "char", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "nuint", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1.un"));
            conversions(sourceType: "nuint", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1.un"));
            conversions(sourceType: "nuint", destType: "short", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2.un"));
            conversions(sourceType: "nuint", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            conversions(sourceType: "nuint", destType: "int", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4.un"));
            conversions(sourceType: "nuint", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4.un"));
            conversions(sourceType: "nuint", destType: "long", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u8"), expectedCheckedIL: conv("conv.ovf.i8.un")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            conversions(sourceType: "nuint", destType: "ulong", expectedImplicitIL: conv("conv.u8"), expectedExplicitIL: conv("conv.u8"));
            conversions(sourceType: "nuint", destType: "float", expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            conversions(sourceType: "nuint", destType: "double", expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            conversions(sourceType: "nuint", destType: "decimal",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "System.UIntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            conversions(sourceType: "nuint", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "char"));
            conversions(sourceType: "nuint", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1.un", "sbyte"));
            conversions(sourceType: "nuint", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1.un", "byte"));
            conversions(sourceType: "nuint", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2.un", "short"));
            conversions(sourceType: "nuint", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "ushort"));
            conversions(sourceType: "nuint", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4.un", "int"));
            conversions(sourceType: "nuint", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4.un", "uint"));
            conversions(sourceType: "nuint", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u8", "long"), expectedCheckedIL: convToNullableT("conv.ovf.i8.un", "long")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            conversions(sourceType: "nuint", destType: "ulong?", expectedImplicitIL: convToNullableT("conv.u8", "ulong"), expectedExplicitIL: convToNullableT("conv.u8", "ulong"));
            conversions(sourceType: "nuint", destType: "float?", expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            conversions(sourceType: "nuint", destType: "double?", expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            conversions(sourceType: "nuint", destType: "decimal?",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0007:  newobj     ""decimal?..ctor(decimal)""
  IL_000c:  ret
}");
            conversions(sourceType: "nuint", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint", destType: "System.UIntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint?", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint?""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint?""
  IL_0006:  ret
}");
            conversions(sourceType: "nuint?", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  ret
}");
            conversions(sourceType: "nuint?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4.un", "nuint"));
            conversions(sourceType: "nuint?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i8.un", "nuint")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            conversions(sourceType: "nuint?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"));
            conversions(sourceType: "nuint?", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nuint"));
            conversions(sourceType: "nuint?", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nuint"));
            conversions(sourceType: "nuint?", destType: "decimal", expectedImplicitIL: null,
@"{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  conv.u8
  IL_0008:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000d:  ret
}");
            conversions(sourceType: "nuint?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "System.UIntPtr", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            conversions(sourceType: "nuint?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "char"));
            conversions(sourceType: "nuint?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nuint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1.un", "nuint", "sbyte"));
            conversions(sourceType: "nuint?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nuint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1.un", "nuint", "byte"));
            conversions(sourceType: "nuint?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nuint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2.un", "nuint", "short"));
            conversions(sourceType: "nuint?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "ushort"));
            conversions(sourceType: "nuint?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nuint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4.un", "nuint", "int"));
            conversions(sourceType: "nuint?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nuint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4.un", "nuint", "uint"));
            conversions(sourceType: "nuint?", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "long"), expectedCheckedIL: convFromToNullableT("conv.ovf.i8.un", "nuint", "long")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            conversions(sourceType: "nuint?", destType: "ulong?", expectedImplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"), expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"));
            conversions(sourceType: "nuint?", destType: "float?", expectedImplicitIL: convFromToNullableT("conv.r4", "nuint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nuint", "float"), null);
            conversions(sourceType: "nuint?", destType: "double?", expectedImplicitIL: convFromToNullableT("conv.r8", "nuint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nuint", "double"), null);
            conversions(sourceType: "nuint?", destType: "decimal?",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nuint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  conv.u8
  IL_001d:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (nuint? V_0,
                decimal? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""decimal?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  conv.u8
  IL_001d:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0022:  newobj     ""decimal?..ctor(decimal)""
  IL_0027:  ret
}");
            conversions(sourceType: "nuint?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            conversions(sourceType: "nuint?", destType: "System.UIntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
        }

        private void Convert(string sourceType,
            string destType,
            string expectedIL,
            bool skipTypeChecks,
            bool useExplicitCast,
            bool useChecked,
            ErrorCode expectedErrorCode)
        {
            bool useUnsafeContext = useUnsafe(sourceType) || useUnsafe(destType);
            string value = "value";
            if (useExplicitCast)
            {
                value = $"({destType})value";
            }
            var expectedDiagnostics = expectedErrorCode == 0 ?
                Array.Empty<DiagnosticDescription>() :
                new[] { Diagnostic(expectedErrorCode, value).WithArguments(sourceType, destType) };
            if (useChecked)
            {
                value = $"checked({value})";
            }
            string source =
$@"class Program
{{
    static {(useUnsafeContext ? "unsafe " : "")}{destType} Convert({sourceType} value)
    {{
        return {value};
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
            var underlyingTypeInfo = model.GetTypeInfo(expr);

            if (!skipTypeChecks)
            {
                Assert.Equal(sourceType, underlyingTypeInfo.Type.ToString());
                Assert.Equal(destType, underlyingTypeInfo.ConvertedType.ToString());
            }

            if (expectedIL != null)
            {
                var verifier = CompileAndVerify(comp, verify: useUnsafeContext ? Verification.Skipped : Verification.Passes);
                verifier.VerifyIL("Program.Convert", expectedIL);
            }

            static bool useUnsafe(string underlyingType) => underlyingType == "void*";
        }

        // PROTOTYPE:Test pre- and postfix increment and decrement. See UnopEasyOut.s_increment.

        // PROTOTYPE: Test unary operator- with `static IntPtr operator-(IntPtr)` defined on System.IntPtr. (Should be ignored for `nint`.)

        [Fact]
        public void UnaryOperators()
        {
            static string getComplement(uint value)
            {
                object result = (IntPtr.Size == 4) ?
                    (object)~value :
                    (object)~(ulong)value;
                return result.ToString();
            }

            void unaryOp(string op, string opType, string expectedSymbol = null, string operand = null, string expectedResult = null, string expectedIL = "", DiagnosticDescription diagnostic = null)
            {
                operand ??= "default";
                if (expectedSymbol == null && diagnostic == null)
                {
                    diagnostic = Diagnostic(ErrorCode.ERR_BadUnaryOp, $"{op}operand").WithArguments(op, opType);
                }

                UnaryOperator(op, opType, opType, expectedSymbol, operand, expectedResult, expectedIL, diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>());
            }

            unaryOp("+", "nint", "nint nint.op_UnaryPlus(nint value)", "3", "3",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            unaryOp(" + ", "nuint", "nuint nuint.op_UnaryPlus(nuint value)", "3", "3",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            unaryOp("+", "System.IntPtr");
            unaryOp("+", "System.UIntPtr");
            unaryOp("-", "nint", "nint nint.op_UnaryNegation(nint value)", "3", "-3",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  neg
  IL_0002:  ret
}");
            unaryOp("-", "nuint", null, null, null, null, Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-operand").WithArguments("-", "nuint")); // PROTOTYPE: Should report ERR_BadUnaryOp.
            unaryOp("-", "System.IntPtr");
            unaryOp("-", "System.UIntPtr");
            unaryOp("!", "nint");
            unaryOp("!", "nuint");
            unaryOp("!", "System.IntPtr");
            unaryOp("!", "System.UIntPtr");
            unaryOp("~", "nint", "nint nint.op_OnesComplement(nint value)", "3", "-4",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  not
  IL_0002:  ret
}");
            unaryOp("~", "nuint", "nuint nuint.op_OnesComplement(nuint value)", "3", getComplement(3),
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  not
  IL_0002:  ret
}");
            unaryOp("~", "System.IntPtr");
            unaryOp("~", "System.UIntPtr");

            unaryOp("+", "nint?", "nint nint.op_UnaryPlus(nint value)", "3", "3",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nint?..ctor(nint)""
  IL_0021:  ret
}");
            unaryOp("+", "nuint?", "nuint nuint.op_UnaryPlus(nuint value)", "3", "3",
@"{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  newobj     ""nuint?..ctor(nuint)""
  IL_0021:  ret
}");
            unaryOp("+", "System.IntPtr?");
            unaryOp("+", "System.UIntPtr?");
            unaryOp("-", "nint?", "nint nint.op_UnaryNegation(nint value)", "3", "-3",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  neg
  IL_001d:  newobj     ""nint?..ctor(nint)""
  IL_0022:  ret
}");
            unaryOp("-", "nuint?", null, null, null, null, Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-operand").WithArguments("-", "nuint?")); // PROTOTYPE: Should report ERR_BadUnaryOp.
            unaryOp("-", "System.IntPtr?");
            unaryOp("-", "System.UIntPtr?");
            unaryOp("!", "nint?");
            unaryOp("!", "nuint?");
            unaryOp("!", "System.IntPtr?");
            unaryOp("!", "System.UIntPtr?");
            unaryOp("~", "nint?", "nint nint.op_OnesComplement(nint value)", "3", "-4",
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nint nint?.GetValueOrDefault()""
  IL_001c:  not
  IL_001d:  newobj     ""nint?..ctor(nint)""
  IL_0022:  ret
}");
            unaryOp("~", "nuint?", "nuint nuint.op_OnesComplement(nuint value)", "3", getComplement(3),
@"{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001c:  not
  IL_001d:  newobj     ""nuint?..ctor(nuint)""
  IL_0022:  ret
}");
            unaryOp("~", "System.IntPtr?");
            unaryOp("~", "System.UIntPtr?");
        }

        private void UnaryOperator(string op, string opType, string resultType, string expectedSymbol, string operand, string expectedResult, string expectedIL, DiagnosticDescription[] expectedDiagnostics)
        {
            string source =
$@"class Program
{{
    static {resultType} Evaluate({opType} operand)
    {{
        return {op}operand;
    }}
    static void Main()
    {{
        System.Console.WriteLine(Evaluate({operand}));
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

            if (expectedDiagnostics.Length == 0)
            {
                var verifier = CompileAndVerify(comp, expectedOutput: expectedResult);
                verifier.VerifyIL("Program.Evaluate", expectedIL);
            }
        }

        [Fact]
        public void BinaryOperators()
        {
            void binaryOps(string op, string leftType, string rightType, string expectedSymbol1 = null, string expectedSymbol2 = "", DiagnosticDescription[] diagnostics1 = null, DiagnosticDescription[] diagnostics2 = null)
            {
                binaryOp(op, leftType, rightType, expectedSymbol1, diagnostics1);
                binaryOp(op, rightType, leftType, expectedSymbol2 == "" ? expectedSymbol1 : expectedSymbol2, diagnostics2 ?? diagnostics1);
            }

            void binaryOp(string op, string leftType, string rightType, string expectedSymbol, DiagnosticDescription[] diagnostics)
            {
                if (expectedSymbol == null && diagnostics == null)
                {
                    diagnostics = new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, leftType, rightType) };
                }
                BinaryOperator(op, leftType, rightType, expectedSymbol, diagnostics ?? Array.Empty<DiagnosticDescription>());
            }

            var arithmeticOperators = new[]
            {
                ("-", "op_Subtraction"),
                ("*", "op_Multiply"),
                ("/", "op_Division"),
                ("%", "op_Modulus"),
            };
            var additionOperators = new[]
            {
                ("+", "op_Addition"),
            };
            var comparisonOperators = new[]
            {
                ("<", "op_LessThan"),
                ("<=", "op_LessThanOrEqual"),
                (">", "op_GreaterThan"),
                (">=", "op_GreaterThanOrEqual"),
            };
            var shiftOperators = new[]
            {
                ("<<", "op_LeftShift"),
                (">>", "op_RightShift"),
            };
            var equalityOperators = new[]
            {
                ("==", "op_Equality"),
                ("!=", "op_Inequality"),
            };
            var logicalOperators = new[]
            {
                ("&", "op_BitwiseAnd"),
                ("|", "op_BitwiseOr"),
                ("^", "op_ExclusiveOr"),
            };

            foreach ((string symbol, string name) in arithmeticOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                // PROTOTYPE: Test all:
                if (symbol == "*") binaryOps(symbol, "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") });
                binaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint") });
                binaryOps(symbol, "nint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") });
                binaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint") });
                binaryOps(symbol, "nint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                // PROTOTYPE: Test all:
                if (symbol == "*") binaryOps(symbol, "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") });
                binaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint?") });
                binaryOps(symbol, "nint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") });
                binaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint?") });
                binaryOps(symbol, "nint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                // PROTOTYPE: Test all:
                if (symbol == "*") binaryOps(symbol, "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint") });
                binaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") });
                binaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint") });
                binaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint") });
                binaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") });
                binaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint") });
                binaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                // PROTOTYPE: Test all:
                if (symbol == "*") binaryOps(symbol, "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint?") });
                binaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint?") });
                binaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr?"); // PROTOTYPE: Not handled.
            }

            foreach ((string symbol, string name) in comparisonOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint") });
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") });
                binaryOps(symbol, "nint", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint") });
                binaryOps(symbol, "nint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") });
                binaryOps(symbol, "nint", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint") });
                binaryOps(symbol, "nint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint?") });
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") });
                binaryOps(symbol, "nint?", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint?") });
                binaryOps(symbol, "nint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") });
                binaryOps(symbol, "nint?", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint?") });
                binaryOps(symbol, "nint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint") });
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint") });
                binaryOps(symbol, "nuint", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") });
                binaryOps(symbol, "nuint", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint") });
                binaryOps(symbol, "nuint", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint") });
                binaryOps(symbol, "nuint", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") });
                binaryOps(symbol, "nuint", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint") });
                binaryOps(symbol, "nuint", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint?") });
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint?") });
                binaryOps(symbol, "nuint?", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint?") });
                binaryOps(symbol, "nuint?", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr?"); // PROTOTYPE: Not handled.
            }

            foreach ((string symbol, string name) in additionOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nint", "void*", $"void* void*.{name}(long left, void* right)", $"void* void*.{name}(void* left, long right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") });
                binaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint") });
                binaryOps(symbol, "nint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") });
                binaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint") });
                binaryOps(symbol, "nint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments(symbol, "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments(symbol, "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") });
                binaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint?") });
                binaryOps(symbol, "nint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") });
                binaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint?") });
                binaryOps(symbol, "nint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nuint", "void*", $"void* void*.{name}(ulong left, void* right)", $"void* void*.{name}(void* left, ulong right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint") });
                binaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") });
                binaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint") });
                binaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint") });
                binaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") });
                binaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint") });
                binaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string", $"string string.{name}(object left, string right)", $"string string.{name}(string left, object right)");
                binaryOps(symbol, "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments(symbol, "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments(symbol, "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint?") });
                binaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint?") });
                binaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"float float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"double double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"decimal decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr?"); // PROTOTYPE: Not handled.
            }

            foreach ((string symbol, string name) in shiftOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "uint");
                binaryOps(symbol, "nint", "nint");
                binaryOps(symbol, "nint", "nuint");
                binaryOps(symbol, "nint", "long");
                binaryOps(symbol, "nint", "ulong");
                binaryOps(symbol, "nint", "float");
                binaryOps(symbol, "nint", "double");
                binaryOps(symbol, "nint", "decimal");
                binaryOps(symbol, "nint", "System.IntPtr");
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint", "uint?");
                binaryOps(symbol, "nint", "nint?");
                binaryOps(symbol, "nint", "nuint?");
                binaryOps(symbol, "nint", "long?");
                binaryOps(symbol, "nint", "ulong?");
                binaryOps(symbol, "nint", "float?");
                binaryOps(symbol, "nint", "double?");
                binaryOps(symbol, "nint", "decimal?");
                binaryOps(symbol, "nint", "System.IntPtr?");
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "uint");
                binaryOps(symbol, "nint?", "nint");
                binaryOps(symbol, "nint?", "nuint");
                binaryOps(symbol, "nint?", "long");
                binaryOps(symbol, "nint?", "ulong");
                binaryOps(symbol, "nint?", "float");
                binaryOps(symbol, "nint?", "double");
                binaryOps(symbol, "nint?", "decimal");
                binaryOps(symbol, "nint?", "System.IntPtr");
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, int right)", null);
                binaryOps(symbol, "nint?", "uint?");
                binaryOps(symbol, "nint?", "nint?");
                binaryOps(symbol, "nint?", "nuint?");
                binaryOps(symbol, "nint?", "long?");
                binaryOps(symbol, "nint?", "ulong?");
                binaryOps(symbol, "nint?", "float?");
                binaryOps(symbol, "nint?", "double?");
                binaryOps(symbol, "nint?", "decimal?");
                binaryOps(symbol, "nint?", "System.IntPtr?");
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "int", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "uint");
                binaryOps(symbol, "nuint", "nint");
                binaryOps(symbol, "nuint", "nuint");
                binaryOps(symbol, "nuint", "long");
                binaryOps(symbol, "nuint", "ulong");
                binaryOps(symbol, "nuint", "float");
                binaryOps(symbol, "nuint", "double");
                binaryOps(symbol, "nuint", "decimal");
                binaryOps(symbol, "nuint", "System.IntPtr");
                binaryOps(symbol, "nuint", "System.UIntPtr");
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "int?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint", "uint?");
                binaryOps(symbol, "nuint", "nint?");
                binaryOps(symbol, "nuint", "nuint?");
                binaryOps(symbol, "nuint", "long?");
                binaryOps(symbol, "nuint", "ulong?");
                binaryOps(symbol, "nuint", "float?");
                binaryOps(symbol, "nuint", "double?");
                binaryOps(symbol, "nuint", "decimal?");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                binaryOps(symbol, "nuint", "System.UIntPtr?");
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "int", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "uint");
                binaryOps(symbol, "nuint?", "nint");
                binaryOps(symbol, "nuint?", "nuint");
                binaryOps(symbol, "nuint?", "long");
                binaryOps(symbol, "nuint?", "ulong");
                binaryOps(symbol, "nuint?", "float");
                binaryOps(symbol, "nuint?", "double");
                binaryOps(symbol, "nuint?", "decimal");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                binaryOps(symbol, "nuint?", "System.UIntPtr");
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "int?", $"nuint nuint.{name}(nuint left, int right)", null);
                binaryOps(symbol, "nuint?", "uint?");
                binaryOps(symbol, "nuint?", "nint?");
                binaryOps(symbol, "nuint?", "nuint?");
                binaryOps(symbol, "nuint?", "long?");
                binaryOps(symbol, "nuint?", "ulong?");
                binaryOps(symbol, "nuint?", "float?");
                binaryOps(symbol, "nuint?", "double?");
                binaryOps(symbol, "nuint?", "decimal?");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                binaryOps(symbol, "nuint?", "System.UIntPtr?");
            }

            foreach ((string symbol, string name) in equalityOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint") });
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") });
                binaryOps(symbol, "nint", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint") });
                binaryOps(symbol, "nint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") });
                binaryOps(symbol, "nint", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint") });
                binaryOps(symbol, "nint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint?") });
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") });
                binaryOps(symbol, "nint?", "long", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong", "nint?") });
                binaryOps(symbol, "nint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"bool nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") });
                binaryOps(symbol, "nint?", "long?", $"bool long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "ulong?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "ulong?", "nint?") });
                binaryOps(symbol, "nint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                //getArgs(builder, symbol, "nint?", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint") });
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint") });
                binaryOps(symbol, "nuint", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint") });
                binaryOps(symbol, "nuint", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint") });
                binaryOps(symbol, "nuint", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint") });
                binaryOps(symbol, "nuint", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint") });
                binaryOps(symbol, "nuint", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint") });
                binaryOps(symbol, "nuint", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint?") });
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int", "nuint?") });
                binaryOps(symbol, "nuint?", "uint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "int?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "int?", "nuint?") });
                binaryOps(symbol, "nuint?", "uint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "nint?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "nuint?") });
                binaryOps(symbol, "nuint?", "nuint?", $"bool nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?", null, null, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "long?") }, new[] { Diagnostic(ErrorCode.ERR_AmbigBinaryOps, $"x {symbol} y").WithArguments(symbol, "long?", "nuint?") });
                binaryOps(symbol, "nuint?", "ulong?", $"bool ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?", $"bool float.{name}(float left, float right)");
                binaryOps(symbol, "nuint?", "double?", $"bool double.{name}(double left, double right)");
                binaryOps(symbol, "nuint?", "decimal?", $"bool decimal.{name}(decimal left, decimal right)");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr?"); // PROTOTYPE: Not handled.
            }

            foreach ((string symbol, string name) in logicalOperators)
            {
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint", "string");
                binaryOps(symbol, "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nint", "bool");
                binaryOps(symbol, "nint", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint");
                binaryOps(symbol, "nint", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong");
                binaryOps(symbol, "nint", "float");
                binaryOps(symbol, "nint", "double");
                binaryOps(symbol, "nint", "decimal");
                //getArgs(builder, symbol, "nint", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr");
                binaryOps(symbol, "nint", "bool?");
                binaryOps(symbol, "nint", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint", "nuint?");
                binaryOps(symbol, "nint", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint", "ulong?");
                binaryOps(symbol, "nint", "float?");
                binaryOps(symbol, "nint", "double?");
                binaryOps(symbol, "nint", "decimal?");
                //getArgs(builder, symbol, "nint", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint", "System.UIntPtr?");
                binaryOps(symbol, "nint", "object");
                binaryOps(symbol, "nint?", "string");
                binaryOps(symbol, "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nint?", "bool");
                binaryOps(symbol, "nint?", "char", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint");
                binaryOps(symbol, "nint?", "long", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong");
                binaryOps(symbol, "nint?", "float");
                binaryOps(symbol, "nint?", "double");
                binaryOps(symbol, "nint?", "decimal");
                //getArgs(builder, symbol, "nint?", "System.IntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr");
                binaryOps(symbol, "nint?", "bool?");
                binaryOps(symbol, "nint?", "char?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "sbyte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "byte?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "short?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "ushort?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "int?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "uint?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "nint?", $"nint nint.{name}(nint left, nint right)");
                binaryOps(symbol, "nint?", "nuint?");
                binaryOps(symbol, "nint?", "long?", $"long long.{name}(long left, long right)");
                binaryOps(symbol, "nint?", "ulong?");
                binaryOps(symbol, "nint?", "float?");
                binaryOps(symbol, "nint?", "double?");
                binaryOps(symbol, "nint?", "decimal?");
                //getArgs(builder, symbol, "nint?", "System.IntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nint?", "System.UIntPtr?");
                binaryOps(symbol, "nuint", "object");
                binaryOps(symbol, "nuint", "string");
                binaryOps(symbol, "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nuint", "bool");
                binaryOps(symbol, "nuint", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int");
                binaryOps(symbol, "nuint", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint");
                binaryOps(symbol, "nuint", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long");
                binaryOps(symbol, "nuint", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float");
                binaryOps(symbol, "nuint", "double");
                binaryOps(symbol, "nuint", "decimal");
                binaryOps(symbol, "nuint", "System.IntPtr");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint", "bool?");
                binaryOps(symbol, "nuint", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "int?");
                binaryOps(symbol, "nuint", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "nint?");
                binaryOps(symbol, "nuint", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint", "long?");
                binaryOps(symbol, "nuint", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint", "float?");
                binaryOps(symbol, "nuint", "double?");
                binaryOps(symbol, "nuint", "decimal?");
                binaryOps(symbol, "nuint", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint", "System.UIntPtr?"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "object");
                binaryOps(symbol, "nuint?", "string");
                binaryOps(symbol, "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {symbol} y").WithArguments(symbol, "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, $"x {symbol} y") });
                binaryOps(symbol, "nuint?", "bool");
                binaryOps(symbol, "nuint?", "char", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int");
                binaryOps(symbol, "nuint?", "uint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint");
                binaryOps(symbol, "nuint?", "nuint", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long");
                binaryOps(symbol, "nuint?", "ulong", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float");
                binaryOps(symbol, "nuint?", "double");
                binaryOps(symbol, "nuint?", "decimal");
                binaryOps(symbol, "nuint?", "System.IntPtr");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr"); // PROTOTYPE: Not handled.
                binaryOps(symbol, "nuint?", "bool?");
                binaryOps(symbol, "nuint?", "char?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "sbyte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "byte?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "short?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "ushort?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "int?");
                binaryOps(symbol, "nuint?", "uint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "nint?");
                binaryOps(symbol, "nuint?", "nuint?", $"nuint nuint.{name}(nuint left, nuint right)");
                binaryOps(symbol, "nuint?", "long?");
                binaryOps(symbol, "nuint?", "ulong?", $"ulong ulong.{name}(ulong left, ulong right)");
                binaryOps(symbol, "nuint?", "float?");
                binaryOps(symbol, "nuint?", "double?");
                binaryOps(symbol, "nuint?", "decimal?");
                binaryOps(symbol, "nuint?", "System.IntPtr?");
                //getArgs(builder, symbol, "nuint?", "System.UIntPtr?"); // PROTOTYPE: Not handled.
            }
        }

        private void BinaryOperator(string op, string leftType, string rightType, string expectedSymbol, DiagnosticDescription[] expectedDiagnostics)
        {
            bool useUnsafeContext = useUnsafe(leftType) || useUnsafe(rightType);
            string source =
$@"class Program
{{
    static {(useUnsafeContext ? "unsafe " : "")}object Evaluate({leftType} x, {rightType} y)
    {{
        return x {op} y;
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

            if (expectedDiagnostics.Length == 0)
            {
                CompileAndVerify(comp);
            }

            static bool useUnsafe(string underlyingType) => underlyingType == "void*";
        }

        [Fact]
        public void BinaryOperators_NInt()
        {
            var source =
@"using System;
class Program
{
    static nint Add(nint x, nint y) => x + y;
    static nint Subtract(nint x, nint y) => x - y;
    static nint Multiply(nint x, nint y) => x * y;
    static nint Divide(nint x, nint y) => x / y;
    static nint Mod(nint x, nint y) => x % y;
    static bool Equals(nint x, nint y) => x == y;
    static bool NotEquals(nint x, nint y) => x != y;
    static bool LessThan(nint x, nint y) => x < y;
    static bool LessThanOrEqual(nint x, nint y) => x <= y;
    static bool GreaterThan(nint x, nint y) => x > y;
    static bool GreaterThanOrEqual(nint x, nint y) => x >= y;
    static nint And(nint x, nint y) => x & y;
    static nint Or(nint x, nint y) => x | y;
    static nint Xor(nint x, nint y) => x ^ y;
    static nint ShiftLeft(nint x, int y) => x << y;
    static nint ShiftRight(nint x, int y) => x >> y;
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(3, 4));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
        Console.WriteLine(Equals(3, 4));
        Console.WriteLine(NotEquals(3, 4));
        Console.WriteLine(LessThan(3, 4));
        Console.WriteLine(LessThanOrEqual(3, 4));
        Console.WriteLine(GreaterThan(3, 4));
        Console.WriteLine(GreaterThanOrEqual(3, 4));
        Console.WriteLine(And(3, 5));
        Console.WriteLine(Or(3, 5));
        Console.WriteLine(Xor(3, 5));
        Console.WriteLine(ShiftLeft(35, 4));
        Console.WriteLine(ShiftRight(35, 4));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
-1
12
2
1
False
True
True
True
False
False
1
7
6
560
2");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Equals",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.NotEquals",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.LessThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.LessThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.GreaterThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.GreaterThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.And",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  and
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Or",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  or
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Xor",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  xor
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shl
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shr
  IL_0003:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NUInt()
        {
            var source =
@"using System;
class Program
{
    static nuint Add(nuint x, nuint y) => x + y;
    static nuint Subtract(nuint x, nuint y) => x - y;
    static nuint Multiply(nuint x, nuint y) => x * y;
    static nuint Divide(nuint x, nuint y) => x / y;
    static nuint Mod(nuint x, nuint y) => x % y;
    static bool Equals(nuint x, nuint y) => x == y;
    static bool NotEquals(nuint x, nuint y) => x != y;
    static bool LessThan(nuint x, nuint y) => x < y;
    static bool LessThanOrEqual(nuint x, nuint y) => x <= y;
    static bool GreaterThan(nuint x, nuint y) => x > y;
    static bool GreaterThanOrEqual(nuint x, nuint y) => x >= y;
    static nuint And(nuint x, nuint y) => x & y;
    static nuint Or(nuint x, nuint y) => x | y;
    static nuint Xor(nuint x, nuint y) => x ^ y;
    static nuint ShiftLeft(nuint x, int y) => x << y;
    static nuint ShiftRight(nuint x, int y) => x >> y;
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(4, 3));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
        Console.WriteLine(Equals(3, 4));
        Console.WriteLine(NotEquals(3, 4));
        Console.WriteLine(LessThan(3, 4));
        Console.WriteLine(LessThanOrEqual(3, 4));
        Console.WriteLine(GreaterThan(3, 4));
        Console.WriteLine(GreaterThanOrEqual(3, 4));
        Console.WriteLine(And(3, 5));
        Console.WriteLine(Or(3, 5));
        Console.WriteLine(Xor(3, 5));
        Console.WriteLine(ShiftLeft(35, 4));
        Console.WriteLine(ShiftRight(35, 4));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
1
12
2
1
False
True
True
True
False
False
1
7
6
560
2");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Equals",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.NotEquals",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.LessThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt.un
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.LessThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt.un
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.GreaterThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt.un
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.GreaterThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt.un
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.And",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  and
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Or",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  or
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Xor",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  xor
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shl
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shr.un
  IL_0003:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NInt_Checked()
        {
            var source =
@"using System;
class Program
{
    static nint Add(nint x, nint y) => checked(x + y);
    static nint Subtract(nint x, nint y) => checked(x - y);
    static nint Multiply(nint x, nint y) => checked(x * y);
    static nint Divide(nint x, nint y) => checked(x / y);
    static nint Mod(nint x, nint y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(3, 4));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
-1
12
2
1");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NUInt_Checked()
        {
            var source =
@"using System;
class Program
{
    static nuint Add(nuint x, nuint y) => checked(x + y);
    static nuint Subtract(nuint x, nuint y) => checked(x - y);
    static nuint Multiply(nuint x, nuint y) => checked(x * y);
    static nuint Divide(nuint x, nuint y) => checked(x / y);
    static nuint Mod(nuint x, nuint y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(4, 3));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
1
12
2
1");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem.un
  IL_0003:  ret
}");
        }
    }
}
