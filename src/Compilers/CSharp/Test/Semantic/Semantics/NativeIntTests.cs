// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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

            var type = comp.GetTypeByMetadataName("System.IntPtr");
            VerifyType(type, signed: true, isNativeInt: false);
            VerifyType(type.GetPublicSymbol(), signed: true, isNativeInt: false);

            type = comp.GetTypeByMetadataName("System.UIntPtr");
            VerifyType(type, signed: false, isNativeInt: false);
            VerifyType(type.GetPublicSymbol(), signed: false, isNativeInt: false);

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
                var type = comp.GetTypeByMetadataName("System.IntPtr");
                VerifyType(type, signed: true, isNativeInt: false);
                VerifyType(type.GetPublicSymbol(), signed: true, isNativeInt: false);

                type = comp.GetTypeByMetadataName("System.UIntPtr");
                VerifyType(type, signed: false, isNativeInt: false);
                VerifyType(type.GetPublicSymbol(), signed: false, isNativeInt: false);

                var method = comp.GetMember<MethodSymbol>("I.F1");
                Assert.Equal("void I.F1(System.IntPtr x, nint y)", method.ToDisplayString(FormatWithSpecialTypes));
                VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: true);

                method = comp.GetMember<MethodSymbol>("I.F2");
                Assert.Equal("void I.F2(System.UIntPtr x, nuint y)", method.ToDisplayString(FormatWithSpecialTypes));
                VerifyTypes((NamedTypeSymbol)method.Parameters[0].Type, (NamedTypeSymbol)method.Parameters[1].Type, signed: false);
            }
        }

        private static void VerifyType(NamedTypeSymbol type, bool signed, bool isNativeInt)
        {
            var specialType = signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr;

            Assert.Equal(specialType, type.SpecialType);
            Assert.Equal(SymbolKind.NamedType, type.Kind);
            Assert.Equal(TypeKind.Struct, type.TypeKind);
            Assert.Same(type, type.ConstructedFrom);
            Assert.Equal(isNativeInt, type.IsNativeInt);
        }

        private static void VerifyType(INamedTypeSymbol type, bool signed, bool isNativeInt)
        {
            var specialType = signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr;

            Assert.Equal(specialType, type.SpecialType);
            Assert.Equal(SymbolKind.NamedType, type.Kind);
            Assert.Equal(TypeKind.Struct, type.TypeKind);
            Assert.Same(type, type.ConstructedFrom);
        }

        private static void VerifyTypes(INamedTypeSymbol underlyingType, INamedTypeSymbol nativeIntegerType, bool signed)
        {
            VerifyType(underlyingType, signed, isNativeInt: false);
            VerifyType(nativeIntegerType, signed, isNativeInt: false);

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
            VerifyType(underlyingType, signed, isNativeInt: false);
            VerifyType(nativeIntegerType, signed, isNativeInt: true);

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
        // - BindNonGenericSimpleNamespaceOrTypeOrAliasSymbol has the comment "dynamic not allowed as an attribute type". Does that apply to "nint"?
        // - BindNonGenericSimpleNamespaceOrTypeOrAliasSymbol checks IsViableType(result)
        // - Use-site diagnostics (basically any use-site diagnostics from IntPtr/UIntPtr)
        // - Type unification of I<System.IntPtr> and I<nint>

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
                var method = comp.GetMember<MethodSymbol>("I.Add");
                Assert.Equal("System.Int16 I.Add(System.Int16 x, System.UIntPtr y)", method.ToTestDisplayString());
                var underlyingType0 = (NamedTypeSymbol)method.Parameters[0].Type;
                var underlyingType1 = (NamedTypeSymbol)method.Parameters[1].Type;
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
        /// Verify there is the number of built in operators for { nint, nuint, nint?, nuint? }
        /// for each operator kind.
        /// </summary>
        [Fact]
        public void BuiltInOperators()
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
                var unaryOperators = new[]
                {
                    UnaryOperatorKind.PostfixIncrement,
                    UnaryOperatorKind.PostfixDecrement,
                    UnaryOperatorKind.PrefixIncrement,
                    UnaryOperatorKind.PrefixDecrement,
                    UnaryOperatorKind.UnaryPlus,
                    UnaryOperatorKind.UnaryMinus,
                    UnaryOperatorKind.BitwiseComplement,
                };

                var binaryOperators = new[]
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

                foreach (var operatorKind in unaryOperators)
                {
                    var builder = ArrayBuilder<UnaryOperatorSignature>.GetInstance();
                    comp.builtInOperators.GetSimpleBuiltInOperators(operatorKind, builder);
                    var operators = builder.ToImmutableAndFree();
                    int expectedUnsigned = (operatorKind == UnaryOperatorKind.UnaryMinus) ? 0 : 1;
                    VerifyOperators(operators, (op, signed) => isNativeInt(op.OperandType, signed), 1, expectedUnsigned);
                    VerifyOperators(operators, (op, signed) => isNullableNativeInt(op.OperandType, signed), 1, expectedUnsigned);
                }

                foreach (var operatorKind in binaryOperators)
                {
                    var builder = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
                    comp.builtInOperators.GetSimpleBuiltInOperators(operatorKind, builder);
                    var operators = builder.ToImmutableAndFree();
                    VerifyOperators(operators, (op, signed) => isNativeInt(op.LeftType, signed), 1, 1);
                    VerifyOperators(operators, (op, signed) => isNullableNativeInt(op.LeftType, signed), 1, 1);
                }

                static void VerifyOperators<T>(ImmutableArray<T> operators, Func<T, bool, bool> predicate, int expectedSigned, int expectedUnsigned)
                {
                    Assert.Equal(expectedSigned, operators.Count(op => predicate(op, true)));
                    Assert.Equal(expectedUnsigned, operators.Count(op => predicate(op, false)));
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

        [Theory]
        [InlineData("")]
        [InlineData("unchecked")]
        [InlineData("checked")]
        public void ConstantConversions_ToNativeInt(string context)
        {
            var source =
$@"#pragma warning disable 219
class Program
{{
    static void F1()
    {{
        nint i;
        {context}
        {{
            i = sbyte.MaxValue;
            i = byte.MaxValue;
            i = char.MaxValue;
            i = short.MaxValue;
            i = ushort.MaxValue;
            i = int.MaxValue;
            i = uint.MaxValue;
            i = long.MaxValue;
            i = ulong.MaxValue;
            i = float.MaxValue;
            i = double.MaxValue;
            i = (decimal)int.MaxValue;
            i = (nint)int.MaxValue;
            i = (nuint)uint.MaxValue;
        }}
    }}
    static void F2()
    {{
        nuint u;
        {context}
        {{
            u = sbyte.MaxValue;
            u = byte.MaxValue;
            u = char.MaxValue;
            u = short.MaxValue;
            u = ushort.MaxValue;
            u = int.MaxValue;
            u = uint.MaxValue;
            u = long.MaxValue;
            u = ulong.MaxValue;
            u = float.MaxValue;
            u = double.MaxValue;
            u = (decimal)uint.MaxValue;
            u = (nint)int.MaxValue;
            u = (nuint)uint.MaxValue;
        }}
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (15,17): error CS0266: Cannot implicitly convert type 'uint' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = uint.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "uint.MaxValue").WithArguments("uint", "nint").WithLocation(15, 17),
                // (16,17): error CS0266: Cannot implicitly convert type 'long' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = long.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "long.MaxValue").WithArguments("long", "nint").WithLocation(16, 17),
                // (17,17): error CS0266: Cannot implicitly convert type 'ulong' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = ulong.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "ulong.MaxValue").WithArguments("ulong", "nint").WithLocation(17, 17),
                // (18,17): error CS0266: Cannot implicitly convert type 'float' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = float.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "float.MaxValue").WithArguments("float", "nint").WithLocation(18, 17),
                // (19,17): error CS0266: Cannot implicitly convert type 'double' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = double.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "double.MaxValue").WithArguments("double", "nint").WithLocation(19, 17),
                // (20,17): error CS0266: Cannot implicitly convert type 'decimal' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = (decimal)int.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(decimal)int.MaxValue").WithArguments("decimal", "nint").WithLocation(20, 17),
                // (22,17): error CS0266: Cannot implicitly convert type 'nuint' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             i = (nuint)uint.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(nuint)uint.MaxValue").WithArguments("nuint", "nint").WithLocation(22, 17),
                // (37,17): error CS0266: Cannot implicitly convert type 'long' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = long.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "long.MaxValue").WithArguments("long", "nuint").WithLocation(37, 17),
                // (38,17): error CS0266: Cannot implicitly convert type 'ulong' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = ulong.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "ulong.MaxValue").WithArguments("ulong", "nuint").WithLocation(38, 17),
                // (39,17): error CS0266: Cannot implicitly convert type 'float' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = float.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "float.MaxValue").WithArguments("float", "nuint").WithLocation(39, 17),
                // (40,17): error CS0266: Cannot implicitly convert type 'double' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = double.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "double.MaxValue").WithArguments("double", "nuint").WithLocation(40, 17),
                // (41,17): error CS0266: Cannot implicitly convert type 'decimal' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = (decimal)uint.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(decimal)uint.MaxValue").WithArguments("decimal", "nuint").WithLocation(41, 17),
                // (42,17): error CS0266: Cannot implicitly convert type 'nint' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             u = (nint)int.MaxValue;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(nint)int.MaxValue").WithArguments("nint", "nuint").WithLocation(42, 17));
        }

        [Theory]
        [InlineData("")]
        [InlineData("unchecked")]
        [InlineData("checked")]
        public void ConstantConversions_FromNativeInt(string context)
        {
            var source =
$@"#pragma warning disable 219
class Program
{{
    static void F1()
    {{
        const nint n = (nint)int.MaxValue;
        {context}
        {{
            sbyte sb = n;
            byte b = n;
            char c = n;
            short s = n;
            ushort us = n;
            int i = n;
            uint u = n;
            long l = n;
            ulong ul = n;
            float f = n;
            double d = n;
            decimal dec = n;
            nuint nu = n;
        }}
    }}
    static void F2()
    {{
        const nuint nu = (nuint)uint.MaxValue;
        {context}
        {{
            sbyte sb = nu;
            byte b = nu;
            char c = nu;
            short s = nu;
            ushort us = nu;
            int i = nu;
            uint u = nu;
            long l = nu;
            ulong ul = nu;
            float f = nu;
            double d = nu;
            decimal dec = nu;
            nint n = nu;
        }}
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,24): error CS0266: Cannot implicitly convert type 'nint' to 'sbyte'. An explicit conversion exists (are you missing a cast?)
                //             sbyte sb = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "sbyte").WithLocation(9, 24),
                // (10,22): error CS0266: Cannot implicitly convert type 'nint' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //             byte b = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "byte").WithLocation(10, 22),
                // (11,22): error CS0266: Cannot implicitly convert type 'nint' to 'char'. An explicit conversion exists (are you missing a cast?)
                //             char c = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "char").WithLocation(11, 22),
                // (12,23): error CS0266: Cannot implicitly convert type 'nint' to 'short'. An explicit conversion exists (are you missing a cast?)
                //             short s = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "short").WithLocation(12, 23),
                // (13,25): error CS0266: Cannot implicitly convert type 'nint' to 'ushort'. An explicit conversion exists (are you missing a cast?)
                //             ushort us = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "ushort").WithLocation(13, 25),
                // (14,21): error CS0266: Cannot implicitly convert type 'nint' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             int i = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "int").WithLocation(14, 21),
                // (15,22): error CS0266: Cannot implicitly convert type 'nint' to 'uint'. An explicit conversion exists (are you missing a cast?)
                //             uint u = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "uint").WithLocation(15, 22),
                // (17,24): error CS0266: Cannot implicitly convert type 'nint' to 'ulong'. An explicit conversion exists (are you missing a cast?)
                //             ulong ul = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "ulong").WithLocation(17, 24),
                // (21,24): error CS0266: Cannot implicitly convert type 'nint' to 'nuint'. An explicit conversion exists (are you missing a cast?)
                //             nuint nu = n;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "n").WithArguments("nint", "nuint").WithLocation(21, 24),
                // (29,24): error CS0266: Cannot implicitly convert type 'nuint' to 'sbyte'. An explicit conversion exists (are you missing a cast?)
                //             sbyte sb = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "sbyte").WithLocation(29, 24),
                // (30,22): error CS0266: Cannot implicitly convert type 'nuint' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //             byte b = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "byte").WithLocation(30, 22),
                // (31,22): error CS0266: Cannot implicitly convert type 'nuint' to 'char'. An explicit conversion exists (are you missing a cast?)
                //             char c = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "char").WithLocation(31, 22),
                // (32,23): error CS0266: Cannot implicitly convert type 'nuint' to 'short'. An explicit conversion exists (are you missing a cast?)
                //             short s = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "short").WithLocation(32, 23),
                // (33,25): error CS0266: Cannot implicitly convert type 'nuint' to 'ushort'. An explicit conversion exists (are you missing a cast?)
                //             ushort us = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "ushort").WithLocation(33, 25),
                // (34,21): error CS0266: Cannot implicitly convert type 'nuint' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             int i = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "int").WithLocation(34, 21),
                // (35,22): error CS0266: Cannot implicitly convert type 'nuint' to 'uint'. An explicit conversion exists (are you missing a cast?)
                //             uint u = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "uint").WithLocation(35, 22),
                // (36,22): error CS0266: Cannot implicitly convert type 'nuint' to 'long'. An explicit conversion exists (are you missing a cast?)
                //             long l = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "long").WithLocation(36, 22),
                // (41,22): error CS0266: Cannot implicitly convert type 'nuint' to 'nint'. An explicit conversion exists (are you missing a cast?)
                //             nint n = nu;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "nu").WithArguments("nuint", "nint").WithLocation(41, 22));
        }

        [Fact]
        public void Constants_NInt()
        {
            string source =
$@"class Program
{{
    static void Main()
    {{
        F(default);
        F(int.MinValue);
        F({short.MinValue - 1});
        F(short.MinValue);
        F(sbyte.MinValue);
        F(-2);
        F(-1);
        F(0);
        F(1);
        F(2);
        F(3);
        F(4);
        F(5);
        F(6);
        F(7);
        F(8);
        F(9);
        F(sbyte.MaxValue);
        F(byte.MaxValue);
        F(short.MaxValue);
        F(char.MaxValue);
        F(ushort.MaxValue);
        F({ushort.MaxValue + 1});
        F(int.MaxValue);
    }}
    static void F(nint n)
    {{
        System.Console.WriteLine(n);
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
@"0
-2147483648
-32769
-32768
-128
-2
-1
0
1
2
3
4
5
6
7
8
9
127
255
32767
65535
65535
65536
2147483647";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            string expectedIL =
@"{
  // Code size      209 (0xd1)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  call       ""void Program.F(nint)""
  IL_0007:  ldc.i4     0x80000000
  IL_000c:  conv.i
  IL_000d:  call       ""void Program.F(nint)""
  IL_0012:  ldc.i4     0xffff7fff
  IL_0017:  conv.i
  IL_0018:  call       ""void Program.F(nint)""
  IL_001d:  ldc.i4     0xffff8000
  IL_0022:  conv.i
  IL_0023:  call       ""void Program.F(nint)""
  IL_0028:  ldc.i4.s   -128
  IL_002a:  conv.i
  IL_002b:  call       ""void Program.F(nint)""
  IL_0030:  ldc.i4.s   -2
  IL_0032:  conv.i
  IL_0033:  call       ""void Program.F(nint)""
  IL_0038:  ldc.i4.m1
  IL_0039:  conv.i
  IL_003a:  call       ""void Program.F(nint)""
  IL_003f:  ldc.i4.0
  IL_0040:  conv.i
  IL_0041:  call       ""void Program.F(nint)""
  IL_0046:  ldc.i4.1
  IL_0047:  conv.i
  IL_0048:  call       ""void Program.F(nint)""
  IL_004d:  ldc.i4.2
  IL_004e:  conv.i
  IL_004f:  call       ""void Program.F(nint)""
  IL_0054:  ldc.i4.3
  IL_0055:  conv.i
  IL_0056:  call       ""void Program.F(nint)""
  IL_005b:  ldc.i4.4
  IL_005c:  conv.i
  IL_005d:  call       ""void Program.F(nint)""
  IL_0062:  ldc.i4.5
  IL_0063:  conv.i
  IL_0064:  call       ""void Program.F(nint)""
  IL_0069:  ldc.i4.6
  IL_006a:  conv.i
  IL_006b:  call       ""void Program.F(nint)""
  IL_0070:  ldc.i4.7
  IL_0071:  conv.i
  IL_0072:  call       ""void Program.F(nint)""
  IL_0077:  ldc.i4.8
  IL_0078:  conv.i
  IL_0079:  call       ""void Program.F(nint)""
  IL_007e:  ldc.i4.s   9
  IL_0080:  conv.i
  IL_0081:  call       ""void Program.F(nint)""
  IL_0086:  ldc.i4.s   127
  IL_0088:  conv.i
  IL_0089:  call       ""void Program.F(nint)""
  IL_008e:  ldc.i4     0xff
  IL_0093:  conv.i
  IL_0094:  call       ""void Program.F(nint)""
  IL_0099:  ldc.i4     0x7fff
  IL_009e:  conv.i
  IL_009f:  call       ""void Program.F(nint)""
  IL_00a4:  ldc.i4     0xffff
  IL_00a9:  conv.i
  IL_00aa:  call       ""void Program.F(nint)""
  IL_00af:  ldc.i4     0xffff
  IL_00b4:  conv.i
  IL_00b5:  call       ""void Program.F(nint)""
  IL_00ba:  ldc.i4     0x10000
  IL_00bf:  conv.i
  IL_00c0:  call       ""void Program.F(nint)""
  IL_00c5:  ldc.i4     0x7fffffff
  IL_00ca:  conv.i
  IL_00cb:  call       ""void Program.F(nint)""
  IL_00d0:  ret
}";
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void Constants_NUInt()
        {
            string source =
$@"class Program
{{
    static void Main()
    {{
        F(default);
        F(0);
        F(1);
        F(2);
        F(3);
        F(4);
        F(5);
        F(6);
        F(7);
        F(8);
        F(9);
        F(sbyte.MaxValue);
        F(byte.MaxValue);
        F(short.MaxValue);
        F(char.MaxValue);
        F(ushort.MaxValue);
        F(int.MaxValue);
        F({(uint)int.MaxValue + 1});
        F(uint.MaxValue);
    }}
    static void F(nuint n)
    {{
        System.Console.WriteLine(n);
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
@"0
0
1
2
3
4
5
6
7
8
9
127
255
32767
65535
65535
2147483647
2147483648
4294967295";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            string expectedIL =
@"{
  // Code size      160 (0xa0)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  call       ""void Program.F(nuint)""
  IL_0007:  ldc.i4.0
  IL_0008:  conv.i
  IL_0009:  call       ""void Program.F(nuint)""
  IL_000e:  ldc.i4.1
  IL_000f:  conv.i
  IL_0010:  call       ""void Program.F(nuint)""
  IL_0015:  ldc.i4.2
  IL_0016:  conv.i
  IL_0017:  call       ""void Program.F(nuint)""
  IL_001c:  ldc.i4.3
  IL_001d:  conv.i
  IL_001e:  call       ""void Program.F(nuint)""
  IL_0023:  ldc.i4.4
  IL_0024:  conv.i
  IL_0025:  call       ""void Program.F(nuint)""
  IL_002a:  ldc.i4.5
  IL_002b:  conv.i
  IL_002c:  call       ""void Program.F(nuint)""
  IL_0031:  ldc.i4.6
  IL_0032:  conv.i
  IL_0033:  call       ""void Program.F(nuint)""
  IL_0038:  ldc.i4.7
  IL_0039:  conv.i
  IL_003a:  call       ""void Program.F(nuint)""
  IL_003f:  ldc.i4.8
  IL_0040:  conv.i
  IL_0041:  call       ""void Program.F(nuint)""
  IL_0046:  ldc.i4.s   9
  IL_0048:  conv.i
  IL_0049:  call       ""void Program.F(nuint)""
  IL_004e:  ldc.i4.s   127
  IL_0050:  conv.i
  IL_0051:  call       ""void Program.F(nuint)""
  IL_0056:  ldc.i4     0xff
  IL_005b:  conv.i
  IL_005c:  call       ""void Program.F(nuint)""
  IL_0061:  ldc.i4     0x7fff
  IL_0066:  conv.i
  IL_0067:  call       ""void Program.F(nuint)""
  IL_006c:  ldc.i4     0xffff
  IL_0071:  conv.i
  IL_0072:  call       ""void Program.F(nuint)""
  IL_0077:  ldc.i4     0xffff
  IL_007c:  conv.i
  IL_007d:  call       ""void Program.F(nuint)""
  IL_0082:  ldc.i4     0x7fffffff
  IL_0087:  conv.i
  IL_0088:  call       ""void Program.F(nuint)""
  IL_008d:  ldc.i4     0x80000000
  IL_0092:  conv.u
  IL_0093:  call       ""void Program.F(nuint)""
  IL_0098:  ldc.i4.m1
  IL_0099:  conv.u
  IL_009a:  call       ""void Program.F(nuint)""
  IL_009f:  ret
}";
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void Constants_Locals()
        {
            var source =
@"#pragma warning disable 219
class Program
{
    static void Main()
    {
        const System.IntPtr a = default;
        const nint b = default;
        const System.UIntPtr c = default;
        const nuint d = default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,15): error CS0283: The type 'IntPtr' cannot be declared const
                //         const System.IntPtr a = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "System.IntPtr").WithArguments("System.IntPtr").WithLocation(6, 15),
                // (8,15): error CS0283: The type 'UIntPtr' cannot be declared const
                //         const System.UIntPtr c = default;
                Diagnostic(ErrorCode.ERR_BadConstType, "System.UIntPtr").WithArguments("System.UIntPtr").WithLocation(8, 15));
        }

        [Fact]
        public void Constants_Fields()
        {
            var source =
@"class Program
{
    const System.IntPtr A = default(System.IntPtr);
    const nint B = default(nint);
    const System.UIntPtr C = default(System.UIntPtr);
    const nuint D = default(nuint);
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,5): error CS0283: The type 'IntPtr' cannot be declared const
                //     const System.IntPtr A = default(System.IntPtr);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("System.IntPtr").WithLocation(3, 5),
                // (3,29): error CS0133: The expression being assigned to 'Program.A' must be constant
                //     const System.IntPtr A = default(System.IntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(System.IntPtr)").WithArguments("Program.A").WithLocation(3, 29),
                // (5,5): error CS0283: The type 'UIntPtr' cannot be declared const
                //     const System.UIntPtr C = default(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_BadConstType, "const").WithArguments("System.UIntPtr").WithLocation(5, 5),
                // (5,30): error CS0133: The expression being assigned to 'Program.C' must be constant
                //     const System.UIntPtr C = default(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(System.UIntPtr)").WithArguments("Program.C").WithLocation(5, 30));
        }

        [Fact]
        public void Constants_FromMetadata()
        {
            var source0 =
@"public class Constants
{
    public const nint NIntMin = int.MinValue;
    public const nint NIntMax = int.MaxValue;
    public const nuint NUIntMin = uint.MinValue;
    public const nuint NUIntMax = uint.MaxValue;
}";
            var comp = CreateCompilation(source0, parseOptions: TestOptions.RegularPreview);
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"using System;
class Program
{
    static void Main()
    {
        const nint nintMin = Constants.NIntMin;
        const nint nintMax = Constants.NIntMax;
        const nuint nuintMin = Constants.NUIntMin;
        const nuint nuintMax = Constants.NUIntMax;
        Console.WriteLine(nintMin);
        Console.WriteLine(nintMax);
        Console.WriteLine(nuintMin);
        Console.WriteLine(nuintMax);
    }
}";
            comp = CreateCompilation(source1, references: new[] { ref0 }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, expectedOutput:
@"-2147483648
2147483647
0
4294967295");
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
                convert(
                    sourceType,
                    destType,
                    expectedImplicitIL,
                    skipTypeChecks: usesIntPtrOrUIntPtr(sourceType) || usesIntPtrOrUIntPtr(destType), // PROTOTYPE: Not distinguishing IntPtr from nint.
                    useExplicitCast: false,
                    useChecked: false,
                    expectedImplicitIL is null ?
                        expectedExplicitIL is null ? ErrorCode.ERR_NoImplicitConv : ErrorCode.ERR_NoImplicitConvCast :
                        0);
                convert(
                    sourceType,
                    destType,
                    expectedExplicitIL,
                    skipTypeChecks: true,
                    useExplicitCast: true,
                    useChecked: false,
                    expectedExplicitIL is null ? ErrorCode.ERR_NoExplicitConv : 0);
                expectedCheckedIL ??= expectedExplicitIL;
                convert(
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

            void convert(string sourceType,
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
                var typeInfo = model.GetTypeInfo(expr);

                if (!skipTypeChecks)
                {
                    Assert.Equal(sourceType, typeInfo.Type.ToString());
                    Assert.Equal(destType, typeInfo.ConvertedType.ToString());
                }

                if (expectedIL != null)
                {
                    var verifier = CompileAndVerify(comp, verify: useUnsafeContext ? Verification.Skipped : Verification.Passes);
                    verifier.VerifyIL("Program.Convert", expectedIL);
                }

                static bool useUnsafe(string type) => type == "void*";
            }
        }

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

                unaryOperator(op, opType, opType, expectedSymbol, operand, expectedResult, expectedIL, diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>());
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

            void unaryOperator(string op, string opType, string resultType, string expectedSymbol, string operand, string expectedResult, string expectedIL, DiagnosticDescription[] expectedDiagnostics)
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
        }

        [Fact]
        public void IncrementOperators()
        {
            void incrementOps(string op, string opType, string expectedSymbol = null, bool useChecked = false, string values = null, string expectedResult = null, string expectedIL = "", string expectedLiftedIL = "", DiagnosticDescription diagnostic = null)
            {
                incrementOperator(op, opType, isPrefix: true, expectedSymbol, useChecked, values, expectedResult, expectedIL, getDiagnostics(opType, isPrefix: true, diagnostic));
                incrementOperator(op, opType, isPrefix: false, expectedSymbol, useChecked, values, expectedResult, expectedIL, getDiagnostics(opType, isPrefix: false, diagnostic));
                opType += "?";
                incrementOperator(op, opType, isPrefix: true, expectedSymbol, useChecked, values, expectedResult, expectedLiftedIL, getDiagnostics(opType, isPrefix: true, diagnostic));
                incrementOperator(op, opType, isPrefix: false, expectedSymbol, useChecked, values, expectedResult, expectedLiftedIL, getDiagnostics(opType, isPrefix: false, diagnostic));

                DiagnosticDescription[] getDiagnostics(string opType, bool isPrefix, DiagnosticDescription diagnostic)
                {
                    if (expectedSymbol == null && diagnostic == null)
                    {
                        diagnostic = Diagnostic(ErrorCode.ERR_BadUnaryOp, isPrefix ? op + "operand" : "operand" + op).WithArguments(op, opType);
                    }
                    return diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>();
                }
            }

            incrementOps("++", "nint", "nint nint.op_Increment(nint value)", useChecked: false,
                values: $"{int.MinValue}, -1, 0, {int.MaxValue - 1}, {int.MaxValue}",
                expectedResult: $"-2147483647, 0, 1, 2147483647, {(IntPtr.Size == 4 ? "-2147483648" : "2147483648")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("++", "nuint", "nuint nuint.op_Increment(nuint value)", useChecked: false,
                values: $"0, {int.MaxValue}, {uint.MaxValue - 1}, {uint.MaxValue}",
                expectedResult: $"1, 2147483648, 4294967295, {(IntPtr.Size == 4 ? "0" : "4294967296")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("++", "System.IntPtr");
            incrementOps("++", "System.UIntPtr");
            incrementOps("--", "nint", "nint nint.op_Decrement(nint value)", useChecked: false,
                values: $"{int.MinValue}, {int.MinValue + 1}, 0, 1, {int.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? "2147483647" : "-2147483649")}, -2147483648, -1, 0, 2147483646",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "nuint", "nuint nuint.op_Decrement(nuint value)", useChecked: false,
                values: $"0, 1, {uint.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? uint.MaxValue.ToString() : ulong.MaxValue.ToString())}, 0, 4294967294",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "System.IntPtr");
            incrementOps("--", "System.UIntPtr");

            incrementOps("++", "nint", "nint nint.op_Increment(nint value)", useChecked: true,
                values: $"{int.MinValue}, -1, 0, {int.MaxValue - 1}, {int.MaxValue}",
                expectedResult: $"-2147483647, 0, 1, 2147483647, {(IntPtr.Size == 4 ? "System.OverflowException" : "2147483648")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add.ovf
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("++", "nuint", "nuint nuint.op_Increment(nuint value)", useChecked: true,
                values: $"0, {int.MaxValue}, {uint.MaxValue - 1}, {uint.MaxValue}",
                expectedResult: $"1, 2147483648, 4294967295, {(IntPtr.Size == 4 ? "System.OverflowException" : "4294967296")}",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf.un
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  add.ovf.un
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "nint", "nint nint.op_Decrement(nint value)", useChecked: true,
                values: $"{int.MinValue}, {int.MinValue + 1}, 0, 1, {int.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? "System.OverflowException" : "-2147483649")}, -2147483648, -1, 0, 2147483646",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub.ovf
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nint nint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub.ovf
  IL_001f:  newobj     ""nint?..ctor(nint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");
            incrementOps("--", "nuint", "nuint nuint.op_Decrement(nuint value)", useChecked: true,
                values: $"0, 1, {uint.MaxValue}",
                expectedResult: $"System.OverflowException, 0, 4294967294",
@"{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub.ovf.un
  IL_0003:  starg.s    V_0
  IL_0005:  ldarg.0
  IL_0006:  ret
}",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool nuint?.HasValue.get""
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""nuint?""
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0024
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_001d:  ldc.i4.1
  IL_001e:  sub.ovf.un
  IL_001f:  newobj     ""nuint?..ctor(nuint)""
  IL_0024:  starg.s    V_0
  IL_0026:  ldarg.0
  IL_0027:  ret
}");

            void incrementOperator(string op, string opType, bool isPrefix, string expectedSymbol, bool useChecked, string values, string expectedResult, string expectedIL, DiagnosticDescription[] expectedDiagnostics)
            {
                var source =
$@"using System;
class Program
{{
    static {opType} Evaluate({opType} operand)
    {{
        {(useChecked ? "checked" : "unchecked")}
        {{
            {(isPrefix ? op + "operand" : "operand" + op)};
            return operand;
        }}
    }}
    static void EvaluateAndReport({opType} operand)
    {{
        object result;
        try
        {{
            result = Evaluate(operand);
        }}
        catch (Exception e)
        {{
            result = e.GetType();
        }}
        Console.Write(result);
    }}
    static void Main()
    {{
        bool separator = false;
        foreach (var value in new {opType}[] {{ {values} }})
        {{
            if (separator) Console.Write("", "");
            separator = true;
            EvaluateAndReport(value);
        }}
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var kind = (op == "++") ?
                    isPrefix ? SyntaxKind.PreIncrementExpression : SyntaxKind.PostIncrementExpression :
                    isPrefix ? SyntaxKind.PreDecrementExpression : SyntaxKind.PostDecrementExpression;
                var expr = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == kind);
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    var verifier = CompileAndVerify(comp, expectedOutput: expectedResult);
                    verifier.VerifyIL("Program.Evaluate", expectedIL);
                }
            }
        }

        [Fact]
        public void IncrementOperators_RefOperand()
        {
            void incrementOps(string op, string opType, string expectedSymbol = null, string values = null, string expectedResult = null, string expectedIL = "", string expectedLiftedIL = "", DiagnosticDescription diagnostic = null)
            {
                incrementOperator(op, opType, expectedSymbol, values, expectedResult, expectedIL, getDiagnostics(opType, diagnostic));
                opType += "?";
                incrementOperator(op, opType, expectedSymbol, values, expectedResult, expectedLiftedIL, getDiagnostics(opType, diagnostic));

                DiagnosticDescription[] getDiagnostics(string opType, DiagnosticDescription diagnostic)
                {
                    if (expectedSymbol == null && diagnostic == null)
                    {
                        diagnostic = Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "operand").WithArguments(op, opType);
                    }
                    return diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>();
                }
            }

            incrementOps("++", "nint", "nint nint.op_Increment(nint value)",
                values: $"{int.MinValue}, -1, 0, {int.MaxValue - 1}, {int.MaxValue}",
                expectedResult: $"-2147483647, 0, 1, 2147483647, {(IntPtr.Size == 4 ? "-2147483648" : "2147483648")}",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nint nint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  newobj     ""nint?..ctor(nint)""
  IL_002a:  stobj      ""nint?""
  IL_002f:  ret
}");
            incrementOps("++", "nuint", "nuint nuint.op_Increment(nuint value)",
                values: $"0, {int.MaxValue}, {uint.MaxValue - 1}, {uint.MaxValue}",
                expectedResult: $"1, 2147483648, 4294967295, {(IntPtr.Size == 4 ? "0" : "4294967296")}",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nuint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nuint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nuint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  newobj     ""nuint?..ctor(nuint)""
  IL_002a:  stobj      ""nuint?""
  IL_002f:  ret
}");
            incrementOps("--", "nint", "nint nint.op_Decrement(nint value)",
                values: $"{int.MinValue}, {int.MinValue + 1}, 0, 1, {int.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? "2147483647" : "-2147483649")}, -2147483648, -1, 0, 2147483646",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nint nint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  sub
  IL_0025:  newobj     ""nint?..ctor(nint)""
  IL_002a:  stobj      ""nint?""
  IL_002f:  ret
}");
            incrementOps("--", "nuint", "nuint nuint.op_Decrement(nuint value)",
                values: $"0, 1, {uint.MaxValue}",
                expectedResult: $"{(IntPtr.Size == 4 ? uint.MaxValue.ToString() : ulong.MaxValue.ToString())}, 0, 4294967294",
@"{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.i
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  stind.i
  IL_0006:  ret
}",
@"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""nuint?""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""bool nuint?.HasValue.get""
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    ""nuint?""
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0023:  ldc.i4.1
  IL_0024:  sub
  IL_0025:  newobj     ""nuint?..ctor(nuint)""
  IL_002a:  stobj      ""nuint?""
  IL_002f:  ret
}");

            void incrementOperator(string op, string opType, string expectedSymbol, string values, string expectedResult, string expectedIL, DiagnosticDescription[] expectedDiagnostics)
            {
                string source =
    $@"using System;
class Program
{{
    static void Evaluate(ref {opType} operand)
    {{
        {op}operand;
    }}
    static void EvaluateAndReport({opType} operand)
    {{
        object result;
        try
        {{
            Evaluate(ref operand);
            result = operand;
        }}
        catch (Exception e)
        {{
            result = e.GetType();
        }}
        Console.Write(result);
    }}
    static void Main()
    {{
        bool separator = false;
        foreach (var value in new {opType}[] {{ {values} }})
        {{
            if (separator) Console.Write("", "");
            separator = true;
            EvaluateAndReport(value);
        }}
    }}
}}";
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
                comp.VerifyDiagnostics(expectedDiagnostics);

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var kind = (op == "++") ? SyntaxKind.PreIncrementExpression : SyntaxKind.PreDecrementExpression;
                var expr = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == kind);
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

                if (expectedDiagnostics.Length == 0)
                {
                    var verifier = CompileAndVerify(comp, expectedOutput: expectedResult);
                    verifier.VerifyIL("Program.Evaluate", expectedIL);
                }
            }
        }

        [Fact]
        public void UnaryOperators_UserDefinedConversions_NInt()
        {
            string source =
@"using System;
class MyInt
{
    private readonly nint _i;
    internal MyInt(nint i) => _i = i;
    public static implicit operator nint(MyInt i) => i._i;
    public static implicit operator MyInt(nint i) => new MyInt(i);
    public override string ToString() => _i.ToString();
}
class Program
{
    static void Main()
    {
        // ++i;
        Evaluate(int.MinValue, PrefixIncrement);
        Evaluate(-1, PrefixIncrement);
        Evaluate(0, PrefixIncrement);
        // i++;
        Evaluate(int.MinValue, PostfixIncrement);
        Evaluate(-1, PostfixIncrement);
        Evaluate(0, PostfixIncrement);
        // --i;
        Evaluate(int.MaxValue, PrefixDecrement);
        Evaluate(1, PrefixDecrement);
        Evaluate(0, PrefixDecrement);
        // i--;
        Evaluate(int.MaxValue, PostfixDecrement);
        Evaluate(1, PostfixDecrement);
        Evaluate(0, PostfixDecrement);
        // +i;
        Evaluate(int.MinValue, Plus);
        Evaluate(0, Plus);
        Evaluate(int.MaxValue, Plus);
        // -i;
        Evaluate(int.MinValue, Minus);
        Evaluate(0, Minus);
        Evaluate(int.MaxValue, Minus);
        // ~i;
        Evaluate(int.MinValue, Complement);
        Evaluate(0, Complement);
        Evaluate(int.MaxValue, Complement);
    }
    static void Evaluate(nint i, Func<MyInt, MyInt> f)
    {
        MyInt m = f(new MyInt(i));
        Console.WriteLine(m);
    }
    static MyInt PrefixIncrement(MyInt i) => ++i;
    static MyInt PostfixIncrement(MyInt i) { i++; return i; }
    static MyInt PrefixDecrement(MyInt i) => --i;
    static MyInt PostfixDecrement(MyInt i) { i--; return i; }
    static MyInt Plus(MyInt i) => +i;
    static MyInt Minus(MyInt i) => -i;
    static MyInt Complement(MyInt i) => ~i;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
@"-2147483647
0
1
-2147483647
0
1
2147483646
0
-1
2147483646
0
-1
-2147483648
0
2147483647
2147483648
0
-2147483647
2147483647
-1
-2147483648";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            verifier.VerifyIL("Program.PrefixIncrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nint)""
  IL_000d:  dup
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.PostfixIncrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nint)""
  IL_000d:  starg.s    V_0
  IL_000f:  ldarg.0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.PrefixDecrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nint)""
  IL_000d:  dup
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.PostfixDecrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nint)""
  IL_000d:  starg.s    V_0
  IL_000f:  ldarg.0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.Plus",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint MyInt.op_Implicit(MyInt)""
  IL_0006:  call       ""MyInt MyInt.op_Implicit(nint)""
  IL_000b:  ret
}");
            verifier.VerifyIL("Program.Minus",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint MyInt.op_Implicit(MyInt)""
  IL_0006:  neg
  IL_0007:  call       ""MyInt MyInt.op_Implicit(nint)""
  IL_000c:  ret
}");
            verifier.VerifyIL("Program.Complement",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint MyInt.op_Implicit(MyInt)""
  IL_0006:  not
  IL_0007:  call       ""MyInt MyInt.op_Implicit(nint)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void UnaryOperators_UserDefinedConversions_NUInt()
        {
            string source =
@"using System;
class MyInt
{
    private readonly nuint _i;
    internal MyInt(nuint i) => _i = i;
    public static implicit operator nuint(MyInt i) => i._i;
    public static implicit operator MyInt(nuint i) => new MyInt(i);
    public override string ToString() => _i.ToString();
}
class Program
{
    static void Main()
    {
        // ++i;
        Evaluate(0, PrefixIncrement);
        Evaluate(uint.MaxValue - 1, PrefixIncrement);
        // i++;
        Evaluate(0, PostfixIncrement);
        Evaluate(uint.MaxValue - 1, PostfixIncrement);
        // --i;
        Evaluate(1, PrefixDecrement);
        Evaluate(uint.MaxValue, PrefixDecrement);
        // i--;
        Evaluate(1, PostfixDecrement);
        Evaluate(uint.MaxValue, PostfixDecrement);
        // +i;
        Evaluate(0, Plus);
        Evaluate(uint.MaxValue, Plus);
        // ~i;
        Evaluate(0, Complement);
        Evaluate(uint.MaxValue, Complement);
    }
    static void Evaluate(nuint i, Func<MyInt, MyInt> f)
    {
        MyInt m = f(new MyInt(i));
        Console.WriteLine(m);
    }
    static MyInt PrefixIncrement(MyInt i) => ++i;
    static MyInt PostfixIncrement(MyInt i) { i++; return i; }
    static MyInt PrefixDecrement(MyInt i) => --i;
    static MyInt PostfixDecrement(MyInt i) { i--; return i; }
    static MyInt Plus(MyInt i) => +i;
    static MyInt Complement(MyInt i) => ~i;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
@"1
4294967295
1
4294967295
0
4294967294
0
4294967294
0
4294967295
18446744073709551615
18446744069414584320";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            verifier.VerifyIL("Program.PrefixIncrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nuint)""
  IL_000d:  dup
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.PostfixIncrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nuint)""
  IL_000d:  starg.s    V_0
  IL_000f:  ldarg.0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.PrefixDecrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nuint)""
  IL_000d:  dup
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.PostfixDecrement",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint MyInt.op_Implicit(MyInt)""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  call       ""MyInt MyInt.op_Implicit(nuint)""
  IL_000d:  starg.s    V_0
  IL_000f:  ldarg.0
  IL_0010:  ret
}");
            verifier.VerifyIL("Program.Plus",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint MyInt.op_Implicit(MyInt)""
  IL_0006:  call       ""MyInt MyInt.op_Implicit(nuint)""
  IL_000b:  ret
}");
            verifier.VerifyIL("Program.Complement",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint MyInt.op_Implicit(MyInt)""
  IL_0006:  not
  IL_0007:  call       ""MyInt MyInt.op_Implicit(nuint)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void UnaryOperators_UserDefinedConversions_LiftedNInt()
        {
            string source =
@"using System;
class MyInt
{
    private readonly nint? _i;
    internal MyInt(nint? i) => _i = i;
    public static implicit operator nint?(MyInt i) => i._i;
    public static implicit operator MyInt(nint? i) => new MyInt(i);
    public override string ToString() => _i.ToString();
}
class Program
{
    static void Main()
    {
        // ++i;
        Evaluate(int.MinValue, PrefixIncrement);
        Evaluate(-1, PrefixIncrement);
        Evaluate(0, PrefixIncrement);
        // i++;
        Evaluate(int.MinValue, PostfixIncrement);
        Evaluate(-1, PostfixIncrement);
        Evaluate(0, PostfixIncrement);
        // --i;
        Evaluate(int.MaxValue, PrefixDecrement);
        Evaluate(1, PrefixDecrement);
        Evaluate(0, PrefixDecrement);
        // i--;
        Evaluate(int.MaxValue, PostfixDecrement);
        Evaluate(1, PostfixDecrement);
        Evaluate(0, PostfixDecrement);
        // +i;
        Evaluate(int.MinValue, Plus);
        Evaluate(0, Plus);
        Evaluate(int.MaxValue, Plus);
        // -i;
        Evaluate(int.MinValue, Minus);
        Evaluate(0, Minus);
        Evaluate(int.MaxValue, Minus);
        // ~i;
        Evaluate(int.MinValue, Complement);
        Evaluate(0, Complement);
        Evaluate(int.MaxValue, Complement);
    }
    static void Evaluate(nint i, Func<MyInt, MyInt> f)
    {
        MyInt m = f(new MyInt(i));
        Console.WriteLine(m);
    }
    static MyInt PrefixIncrement(MyInt i) => ++i;
    static MyInt PostfixIncrement(MyInt i) { i++; return i; }
    static MyInt PrefixDecrement(MyInt i) => --i;
    static MyInt PostfixDecrement(MyInt i) { i--; return i; }
    static MyInt Plus(MyInt i) => +i;
    static MyInt Minus(MyInt i) => -i;
    static MyInt Complement(MyInt i) => ~i;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
$@"-2147483647
0
1
-2147483647
0
1
2147483646
0
-1
2147483646
0
-1
-2147483648
0
2147483647
{(IntPtr.Size == 4 ? "-2147483648" : "2147483648")}
0
-2147483647
2147483647
-1
-2147483648";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            verifier.VerifyIL("Program.PrefixIncrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  add
  IL_0024:  newobj     ""nint?..ctor(nint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nint?)""
  IL_002e:  dup
  IL_002f:  starg.s    V_0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.PostfixIncrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  add
  IL_0024:  newobj     ""nint?..ctor(nint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nint?)""
  IL_002e:  starg.s    V_0
  IL_0030:  ldarg.0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.PrefixDecrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  sub
  IL_0024:  newobj     ""nint?..ctor(nint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nint?)""
  IL_002e:  dup
  IL_002f:  starg.s    V_0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.PostfixDecrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  sub
  IL_0024:  newobj     ""nint?..ctor(nint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nint?)""
  IL_002e:  starg.s    V_0
  IL_0030:  ldarg.0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.Plus",
@"{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0027
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  newobj     ""nint?..ctor(nint)""
  IL_0027:  call       ""MyInt MyInt.op_Implicit(nint?)""
  IL_002c:  ret
}");
            verifier.VerifyIL("Program.Minus",
@"{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0028
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  neg
  IL_0023:  newobj     ""nint?..ctor(nint)""
  IL_0028:  call       ""MyInt MyInt.op_Implicit(nint?)""
  IL_002d:  ret
}");
            verifier.VerifyIL("Program.Complement",
@"{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (nint? V_0,
                nint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0028
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nint nint?.GetValueOrDefault()""
  IL_0022:  not
  IL_0023:  newobj     ""nint?..ctor(nint)""
  IL_0028:  call       ""MyInt MyInt.op_Implicit(nint?)""
  IL_002d:  ret
}");
        }

        [Fact]
        public void UnaryOperators_UserDefinedConversions_LiftedNUInt()
        {
            string source =
@"using System;
class MyInt
{
    private readonly nuint? _i;
    internal MyInt(nuint? i) => _i = i;
    public static implicit operator nuint?(MyInt i) => i._i;
    public static implicit operator MyInt(nuint? i) => new MyInt(i);
    public override string ToString() => _i.ToString();
}
class Program
{
    static void Main()
    {
        // ++i;
        Evaluate(0, PrefixIncrement);
        Evaluate(uint.MaxValue - 1, PrefixIncrement);
        // i++;
        Evaluate(0, PostfixIncrement);
        Evaluate(uint.MaxValue - 1, PostfixIncrement);
        // --i;
        Evaluate(1, PrefixDecrement);
        Evaluate(uint.MaxValue, PrefixDecrement);
        // i--;
        Evaluate(1, PostfixDecrement);
        Evaluate(uint.MaxValue, PostfixDecrement);
        // +i;
        Evaluate(0, Plus);
        Evaluate(uint.MaxValue, Plus);
        // ~i;
        Evaluate(0, Complement);
        Evaluate(uint.MaxValue, Complement);
    }
    static void Evaluate(nuint i, Func<MyInt, MyInt> f)
    {
        MyInt m = f(new MyInt(i));
        Console.WriteLine(m);
    }
    static MyInt PrefixIncrement(MyInt i) => ++i;
    static MyInt PostfixIncrement(MyInt i) { i++; return i; }
    static MyInt PrefixDecrement(MyInt i) => --i;
    static MyInt PostfixDecrement(MyInt i) { i--; return i; }
    static MyInt Plus(MyInt i) => +i;
    static MyInt Complement(MyInt i) => ~i;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            string expectedOutput =
$@"1
4294967295
1
4294967295
0
4294967294
0
4294967294
0
4294967295
{(IntPtr.Size == 4 ? "4294967295" : "18446744073709551615")}
{(IntPtr.Size == 4 ? "0" : "18446744069414584320")}";
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
            verifier.VerifyIL("Program.PrefixIncrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nuint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nuint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  add
  IL_0024:  newobj     ""nuint?..ctor(nuint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nuint?)""
  IL_002e:  dup
  IL_002f:  starg.s    V_0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.PostfixIncrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nuint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nuint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  add
  IL_0024:  newobj     ""nuint?..ctor(nuint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nuint?)""
  IL_002e:  starg.s    V_0
  IL_0030:  ldarg.0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.PrefixDecrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nuint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nuint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  sub
  IL_0024:  newobj     ""nuint?..ctor(nuint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nuint?)""
  IL_002e:  dup
  IL_002f:  starg.s    V_0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.PostfixDecrement",
@"{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nuint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nuint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0029
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  sub
  IL_0024:  newobj     ""nuint?..ctor(nuint)""
  IL_0029:  call       ""MyInt MyInt.op_Implicit(nuint?)""
  IL_002e:  starg.s    V_0
  IL_0030:  ldarg.0
  IL_0031:  ret
}");
            verifier.VerifyIL("Program.Plus",
@"{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nuint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nuint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0027
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0022:  newobj     ""nuint?..ctor(nuint)""
  IL_0027:  call       ""MyInt MyInt.op_Implicit(nuint?)""
  IL_002c:  ret
}");
            verifier.VerifyIL("Program.Complement",
@"{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (nuint? V_0,
                nuint? V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""nuint? MyInt.op_Implicit(MyInt)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool nuint?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""nuint?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_0028
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""nuint nuint?.GetValueOrDefault()""
  IL_0022:  not
  IL_0023:  newobj     ""nuint?..ctor(nuint)""
  IL_0028:  call       ""MyInt MyInt.op_Implicit(nuint?)""
  IL_002d:  ret
}");
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
                binaryOperator(op, leftType, rightType, expectedSymbol, diagnostics ?? Array.Empty<DiagnosticDescription>());
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

            void binaryOperator(string op, string leftType, string rightType, string expectedSymbol, DiagnosticDescription[] expectedDiagnostics)
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

                static bool useUnsafe(string type) => type == "void*";
            }
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
