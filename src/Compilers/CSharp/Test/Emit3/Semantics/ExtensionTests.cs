// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public class ExtensionTests : CompilingTestBase
{
    private static string ExpectedOutput(string output)
    {
        return ExecutionConditionUtil.IsMonoOrCoreClr ? output : null;
    }

    private static void VerifyTypeIL(CompilationVerifier compilation, string typeName, string expected)
    {
        // .Net Core has different assemblies for the same standard library types as .Net Framework, meaning that that the emitted output will be different to the expected if we run them .Net Core
        // Since we do not expect there to be any meaningful differences between output for .Net Core and .Net Framework, we will skip these tests on .Net Framework
        if (ExecutionConditionUtil.IsCoreClr)
        {
            compilation.VerifyTypeIL(typeName, expected);
        }
    }

    private static void AssertExtensionDeclaration(INamedTypeSymbol symbol)
    {
        // Verify things that are common for all extension types
        Assert.Equal(TypeKind.Extension, symbol.TypeKind);
        Assert.True(symbol.IsExtension);
        Assert.Null(symbol.BaseType);
        Assert.Empty(symbol.Interfaces);
        Assert.Empty(symbol.AllInterfaces);
        Assert.True(symbol.IsReferenceType);
        Assert.False(symbol.IsValueType);
        Assert.False(symbol.IsAnonymousType);
        Assert.False(symbol.IsTupleType);
        Assert.False(symbol.IsNativeIntegerType);
        Assert.Equal(SpecialType.None, symbol.SpecialType);
        Assert.False(symbol.IsRefLikeType);
        Assert.False(symbol.IsUnmanagedType);
        Assert.False(symbol.IsReadOnly);
        Assert.False(symbol.IsRecord);
        Assert.Equal(CodeAnalysis.NullableAnnotation.None, symbol.NullableAnnotation);
        Assert.Throws<NotSupportedException>(() => { symbol.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated); });

        Assert.False(symbol.IsScriptClass);
        Assert.False(symbol.IsImplicitClass);
        Assert.False(symbol.IsComImport);
        Assert.False(symbol.IsFileLocal);
        Assert.Null(symbol.DelegateInvokeMethod);
        Assert.Null(symbol.EnumUnderlyingType);
        Assert.Null(symbol.AssociatedSymbol);
        Assert.False(symbol.MightContainExtensionMethods);
        Assert.Null(symbol.TupleUnderlyingType);
        Assert.True(symbol.TupleElements.IsDefault);
        Assert.False(symbol.IsSerializable);
        Assert.Null(symbol.NativeIntegerUnderlyingType);

        Assert.Equal(SymbolKind.NamedType, symbol.Kind);
        Assert.Equal("", symbol.Name);
        Assert.Equal(SpecialType.None, symbol.SpecialType);
        Assert.True(symbol.IsDefinition);
        Assert.False(symbol.IsStatic);
        Assert.False(symbol.IsVirtual);
        Assert.False(symbol.IsOverride);
        Assert.False(symbol.IsAbstract);
        Assert.True(symbol.IsSealed);
        Assert.False(symbol.IsExtern);
        Assert.False(symbol.IsImplicitlyDeclared);
        Assert.False(symbol.CanBeReferencedByName);
        Assert.Equal(Accessibility.Public, symbol.DeclaredAccessibility);

        var namedTypeSymbol = symbol.GetSymbol<NamedTypeSymbol>();
        Assert.False(namedTypeSymbol.HasSpecialName);
        Assert.False(namedTypeSymbol.IsImplicitlyDeclared);
    }

    [Fact]
    public void EmptyExtension()
    {
        var src = """
public static class Extensions
{
    extension(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [netstandard]System.Object
    {
    } // end of class <>E__0
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);

        Assert.Empty(symbol.MemberNames);
        Assert.Empty(symbol.InstanceConstructors);
        Assert.Empty(symbol.StaticConstructors);
        Assert.Empty(symbol.Constructors);

        Assert.Equal(0, symbol.Arity);
        Assert.False(symbol.IsGenericType);
        Assert.False(symbol.IsUnboundGenericType);
        Assert.Empty(symbol.TypeParameters);
        Assert.Empty(symbol.TypeArguments);
        Assert.Same(symbol, symbol.OriginalDefinition);
        Assert.Same(symbol, symbol.ConstructedFrom);
        Assert.Equal("Extensions", symbol.ContainingSymbol.Name);
        Assert.Equal("Extensions", symbol.ContainingType.Name);
        Assert.Equal("<>E__0", symbol.MetadataName);

        var member = symbol.ContainingType.GetMembers().Single();
        Assert.Equal("Extensions.<>E__0", member.ToTestDisplayString());

        var format = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        Assert.Equal("Extensions.extension", symbol.ToDisplayString(format)); // PROTOTYPE display string should include the receiver parameter

        format = new SymbolDisplayFormat(kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);
        Assert.Equal("extension", symbol.ToDisplayString(format)); // PROTOTYPE display string should include the receiver parameter

        format = new SymbolDisplayFormat();
        Assert.Equal("extension", symbol.ToDisplayString(format)); // PROTOTYPE display string should include the receiver parameter

        format = new SymbolDisplayFormat(compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseMetadataMemberNames);
        Assert.Equal("<>E__0", symbol.ToDisplayString(format));
    }

    [Fact]
    public void TypeParameters_01()
    {
        // Unconstrained type parameter
        var src = """
public static class Extensions
{
    extension<T>(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<T>
        extends [netstandard]System.Object
    {
    } // end of class <>E__0`1
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);

        Assert.Equal(1, symbol.Arity);
        Assert.True(symbol.IsGenericType);
        Assert.False(symbol.IsUnboundGenericType);
        Assert.Equal(["T"], symbol.TypeParameters.ToTestDisplayStrings());
        Assert.Equal(["T"], symbol.TypeArguments.ToTestDisplayStrings());
        Assert.Same(symbol, symbol.OriginalDefinition);
        Assert.Same(symbol, symbol.ConstructedFrom);
        Assert.Equal("Extensions", symbol.ContainingSymbol.Name);
        Assert.Equal("Extensions", symbol.ContainingType.Name);
        Assert.Equal("<>E__0`1", symbol.MetadataName);

        var member = symbol.ContainingType.GetMembers().Single();
        Assert.Equal("Extensions.<>E__0<T>", member.ToTestDisplayString());

        var constructed = symbol.Construct(comp.GetSpecialType(SpecialType.System_Int32));
        Assert.True(constructed.IsExtension);
        Assert.Equal("Extensions.<>E__0<System.Int32>", constructed.ToTestDisplayString());
        Assert.Equal("<>E__0`1", constructed.MetadataName);
        Assert.NotSame(symbol, constructed);
        Assert.Same(symbol, constructed.OriginalDefinition);
        Assert.Same(symbol, constructed.ConstructedFrom);

        var unbound = symbol.ConstructUnboundGenericType();
        Assert.Equal("Extensions.<>E__0<>", unbound.ToTestDisplayString());
        Assert.True(unbound.IsUnboundGenericType);
        Assert.NotSame(symbol, unbound);
        Assert.Same(symbol, unbound.OriginalDefinition);
        Assert.Same(symbol, unbound.ConstructedFrom);
    }

    [Fact]
    public void TypeParameters_02()
    {
        // Constrained type parameter
        var src = """
public static class Extensions
{
    extension<T>(object) where T : struct { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<valuetype .ctor ([netstandard]System.ValueType) T>
        extends [netstandard]System.Object
    {
    } // end of class <>E__0`1
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);

        Assert.Equal(1, symbol.Arity);
        Assert.True(symbol.IsGenericType);
        Assert.Equal(["T"], symbol.TypeParameters.ToTestDisplayStrings());
        Assert.Equal(["T"], symbol.TypeArguments.ToTestDisplayStrings());
        Assert.True(symbol.TypeParameters.Single().IsValueType);
        Assert.False(symbol.TypeParameters.Single().IsReferenceType);
        Assert.Empty(symbol.TypeParameters.Single().ConstraintTypes);

        var format = new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints);
        Assert.Equal("extension<T> where T : struct", symbol.ToDisplayString(format)); // PROTOTYPE display string should include the receiver parameter
    }

    [Fact]
    public void TypeParameters_03()
    {
        // Constraint on undefined type parameter
        var src = """
public static class Extensions
{
    extension(object) where T : struct { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,23): error CS0080: Constraints are not allowed on non-generic declarations
            //     extension(object) where T : struct { }
            Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(3, 23));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(0, symbol.Arity);
        Assert.False(symbol.IsGenericType);
        Assert.Empty(symbol.TypeParameters.ToTestDisplayStrings());
        Assert.Empty(symbol.TypeArguments.ToTestDisplayStrings());
    }

    [Fact]
    public void TypeParameters_04()
    {
        // Type parameter variance
        var src = """
public static class Extensions
{
    extension<out T>(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            //     extension<out T>(object) { }
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(1, symbol.Arity);
        Assert.True(symbol.IsGenericType);
        Assert.Equal(["out T"], symbol.TypeParameters.ToTestDisplayStrings());
        Assert.Equal(["out T"], symbol.TypeArguments.ToTestDisplayStrings());
    }

    [Fact]
    public void TypeParameters_05()
    {
        // Duplicate type parameter
        var src = """
public static class Extensions
{
    extension<T, T>(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,18): error CS0692: Duplicate type parameter 'T'
            //     extension<T, T>(object) { }
            Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "T").WithArguments("T").WithLocation(3, 18));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(2, symbol.Arity);
        Assert.Equal(["T", "T"], symbol.TypeParameters.ToTestDisplayStrings());
        Assert.Equal(["T", "T"], symbol.TypeArguments.ToTestDisplayStrings());
    }

    [Fact]
    public void TypeParameters_06()
    {
        // Type parameter same as outer type parameter
        var src = """
public static class Extensions<T>
{
    extension<T>(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension<T>(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5),
            // (3,15): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'Extensions<T>'
            //     extension<T>(object) { }
            Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "Extensions<T>").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(1, symbol.Arity);
        Assert.Equal(["T"], symbol.TypeParameters.ToTestDisplayStrings());
        Assert.Equal(["T"], symbol.TypeArguments.ToTestDisplayStrings());

        var container = symbol.ContainingType;
        var substitutedExtension = (INamedTypeSymbol)container.Construct(comp.GetSpecialType(SpecialType.System_Int32)).GetMembers().Single();
        Assert.Equal("Extensions<System.Int32>.<>E__0<T>", substitutedExtension.ToTestDisplayString());
        Assert.True(substitutedExtension.IsExtension);
    }

    [Fact]
    public void TypeParameters_07()
    {
        // Reserved type name for type parameter
        var src = $$"""
public static class Extensions
{
    extension<record>(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): warning CS8860: Types and aliases should not be named 'record'.
            //     extension<record>(object) { }
            Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["record"], symbol.TypeParameters.ToTestDisplayStrings());
    }

    [Fact]
    public void TypeParameters_08()
    {
        // Reserved type name for type parameter
        var src = $$"""
public static class Extensions
{
    extension<file>(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9056: Types and aliases cannot be named 'file'.
            //     extension<file>(object) { }
            Diagnostic(ErrorCode.ERR_FileTypeNameDisallowed, "file").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["file"], symbol.TypeParameters.ToTestDisplayStrings());
    }

    [Fact]
    public void TypeParameters_09()
    {
        // Member name same as type parameter
        var src = $$"""
public static class Extensions
{
    extension<T>(object)
    {
        void T() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,14): error CS0102: The type 'Extensions.extension<T>' already contains a definition for 'T'
            //         void T() { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "T").WithArguments("Extensions.extension<T>", "T").WithLocation(5, 14));
    }

    [Fact]
    public void TypeParameters_10()
    {
        var src = $$"""
#nullable enable
public static class Extensions
{
    extension<T>(object) where T : notnull
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<T>
        extends [netstandard]System.Object
    {
        .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
    } // end of class <>E__0`1
} // end of class Extensions
""");
    }

    [Fact]
    public void BadContainer_Generic()
    {
        var src = """
object.M();

public static class Extensions<T>
{
    extension(object) { public static void M() { } }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(1, 8),
            // (5,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(object) { public static void M() { } }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 5));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);
        Assert.True(symbol.IsGenericType);
        var member = symbol.ContainingType.GetMembers().Single();
        Assert.Equal("Extensions<T>.<>E__0", member.ToTestDisplayString());
    }

    [Fact]
    public void BadContainer_TopLevel()
    {
        var src = """
object.M();

extension(object) { public static void M() { } }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(1, 8),
            // (3,1): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            // extension(object) { public static void M() { } }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 1));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);
        Assert.Null(symbol.ContainingType);
        Assert.Equal("<>E__0", symbol.ToTestDisplayString());
    }

    [Fact]
    public void BadContainer_Nested()
    {
        var src = """
object.M();

public static class Extensions
{
    static void Method()
    {
        object.M();
    }

    public static class Extensions2
    {
        extension(object) { public static void M() { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(1, 8),
            // (7,16): error CS0117: 'object' does not contain a definition for 'M'
            //         object.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(7, 16),
            // (12,9): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //         extension(object) { public static void M() { } }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(12, 9));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var nestedExtension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();

        var nestedExtensionSymbol = model.GetDeclaredSymbol(nestedExtension);
        AssertExtensionDeclaration(nestedExtensionSymbol);
        Assert.Equal("Extensions.Extensions2", nestedExtensionSymbol.ContainingType.ToTestDisplayString());
        var member = nestedExtensionSymbol.ContainingType.GetMembers().Single();
        Assert.Equal("Extensions.Extensions2.<>E__0", member.ToTestDisplayString());
    }

    [Fact]
    public void BadContainer_NestedInExtension()
    {
        var src = """
string.M();

public static class Extensions
{
    static void Method()
    {
        string.M();
    }

    extension(object)
    {
        static void Method()
        {
            string.M();
        }

        extension(string) { public static void M() { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'string' does not contain a definition for 'M'
            // string.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("string", "M").WithLocation(1, 8),
            // (7,16): error CS0117: 'string' does not contain a definition for 'M'
            //         string.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("string", "M").WithLocation(7, 16),
            // (14,20): error CS0117: 'string' does not contain a definition for 'M'
            //             string.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("string", "M").WithLocation(14, 20),
            // (17,9): error CS9501: Extension declarations can include only methods or properties
            //         extension(string) { public static void M() { } }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "extension").WithLocation(17, 9));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var nestedExtension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();

        var nestedExtensionSymbol = model.GetDeclaredSymbol(nestedExtension);
        AssertExtensionDeclaration(nestedExtensionSymbol);
        Assert.Equal("Extensions.<>E__0", nestedExtensionSymbol.ContainingType.ToTestDisplayString());
        Assert.Equal(["void Extensions.<>E__0.Method()", "Extensions.<>E__0.<>E__0"], nestedExtensionSymbol.ContainingType.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void BadContainer_TypeKind()
    {
        var src = """
object.M();

public static struct Extensions
{
    extension(object) { public static void M() { } }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(1, 8),
            // (3,22): error CS0106: The modifier 'static' is not valid for this item
            // public static struct Extensions
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Extensions").WithArguments("static").WithLocation(3, 22),
            // (5,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(object) { public static void M() { } }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 5));
    }

    [Fact]
    public void BadContainer_NotStatic()
    {
        var src = """
object.M();

public class Extensions
{
    extension(object) { public static void M() { } }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(1, 8),
            // (5,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(object) { public static void M() { } }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 5));
    }

    [Theory, CombinatorialData]
    public void ExtensionIndex_InPartial(bool reverseOrder)
    {
        var src1 = """
public static partial class Extensions
{
    extension(object) { }
}
""";
        var src2 = """
public static partial class Extensions
{
}
""";

        var src = reverseOrder ? new[] { src2, src1 } : new[] { src1, src2 };
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [netstandard]System.Object
    {
    } // end of class <>E__0
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[reverseOrder ? 1 : 0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);
        Assert.Equal("<>E__0", symbol.MetadataName);
    }

    [Fact]
    public void ExtensionIndex_InPartial_TwoExtension()
    {
        var src1 = """
public static partial class Extensions
{
    extension<T>(object) { }
}
""";
        var src2 = """
public static partial class Extensions
{
    extension<T1, T2>(string) { }
}
""";

        var comp = CreateCompilation([src1, src2]);
        comp.VerifyEmitDiagnostics();

        var tree1 = comp.SyntaxTrees[0];
        var model1 = comp.GetSemanticModel(tree1);
        var extension1 = tree1.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol1 = model1.GetDeclaredSymbol(extension1);
        var sourceExtension1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__0`1", symbol1.MetadataName);
        Assert.Equal("Extensions.<>E__0<T>", sourceExtension1.ToTestDisplayString());

        var tree2 = comp.SyntaxTrees[1];
        var extension2 = tree2.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var model2 = comp.GetSemanticModel(tree2);
        var symbol2 = model2.GetDeclaredSymbol(extension2);
        var sourceExtension2 = symbol2.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__1`2", symbol2.MetadataName);
        Assert.Equal("Extensions.<>E__1<T1, T2>", sourceExtension2.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionIndex_InPartial_TwoExtensions()
    {
        var src = """
public static partial class Extensions
{
    extension<T>(object) { }
}
public static partial class Extensions
{
    extension<T1, T2>(string) { }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(extension1);
        Assert.Equal("<>E__0`1", symbol1.MetadataName);
        Assert.Equal("Extensions.<>E__0<T>", symbol1.ToTestDisplayString());

        var extension2 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();
        var symbol2 = model.GetDeclaredSymbol(extension2);
        Assert.Equal("<>E__1`2", symbol2.MetadataName);
        Assert.Equal("Extensions.<>E__1<T1, T2>", symbol2.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionIndex_TwoExtensions_SameSignatures_01()
    {
        var src = """
public static class Extensions
{
    extension<T>(object) { }
    extension<T>(object) { }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(extension1);
        var sourceExtension1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__0`1", symbol1.MetadataName);
        Assert.Equal("Extensions.<>E__0<T>", symbol1.ToTestDisplayString());

        var extension2 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();
        var symbol2 = model.GetDeclaredSymbol(extension2);
        var sourceExtension2 = symbol2.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__1`1", symbol2.MetadataName);
        Assert.Equal("Extensions.<>E__1<T>", symbol2.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionIndex_TwoExtensions_SameSignatures_02()
    {
        var src = """
public static class Extensions
{
    extension<T>(object) { }
    class C { }
    extension<T>(object) { }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(extension1);
        var sourceExtension1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__0`1", symbol1.MetadataName);
        Assert.Equal("Extensions.<>E__0<T>", symbol1.ToTestDisplayString());

        var extension2 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();
        var symbol2 = model.GetDeclaredSymbol(extension2);
        var sourceExtension2 = symbol2.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__2`1", symbol2.MetadataName);
        Assert.Equal("Extensions.<>E__2<T>", symbol2.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionIndex_TwoExtensions_SameSignatures_03()
    {
        var src = """
extension<T>(object) { }
class C { }
extension<T>(object) { }
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            // extension<T>(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(1, 1),
            // (3,1): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            // extension<T>(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 1));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(extension1);
        var sourceExtension1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__0`1", symbol1.MetadataName);
        Assert.Equal("<>E__0<T>", symbol1.ToTestDisplayString());

        var extension2 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();
        var symbol2 = model.GetDeclaredSymbol(extension2);
        var sourceExtension2 = symbol2.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__2`1", symbol2.MetadataName);
        Assert.Equal("<>E__2<T>", symbol2.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionIndex_TwoExtensions_DifferentSignatures_01()
    {
        var src = """
public static class Extensions
{
    extension<T>(object) { }
    extension<T1, T2>(string) { }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(extension1);
        var sourceExtension1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__0`1", symbol1.MetadataName);
        Assert.Equal("Extensions.<>E__0<T>", symbol1.ToTestDisplayString());

        var extension2 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();
        var symbol2 = model.GetDeclaredSymbol(extension2);
        var sourceExtension2 = symbol2.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__1`2", symbol2.MetadataName);
        Assert.Equal("Extensions.<>E__1<T1, T2>", symbol2.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionIndex_TwoExtensions_DifferentSignatures_02()
    {
        var src = """
public static class Extensions
{
    extension<T>(object) { }
    extension<T1>(object) where T1 : struct { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(extension1);
        var sourceExtension1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__0`1", symbol1.MetadataName);
        Assert.Equal("Extensions.<>E__0<T>", symbol1.ToTestDisplayString());

        var extension2 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();
        var symbol2 = model.GetDeclaredSymbol(extension2);
        var sourceExtension2 = symbol2.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__1`1", symbol2.MetadataName);
        Assert.Equal("Extensions.<>E__1<T1>", symbol2.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionIndex_ElevenExtensions()
    {
        var src = """
public static class Extensions
{
    extension<T1>(object o1) { }
    extension<T2>(object o2) { }
    extension<T3>(object o3) { }
    extension<T4>(object o4) { }
    extension<T5>(object o5) { }
    extension<T6>(object o6) { }
    extension<T7>(object o7) { }
    extension<T8>(object o8) { }
    extension<T9>(object o9) { }
    extension<T10>(object o10) { }
    extension<T10>(object o11) { }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();
        var symbol = model.GetDeclaredSymbol(extension);
        var sourceExtension = symbol.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("<>E__10`1", symbol.MetadataName);
        Assert.Equal("Extensions.<>E__10<T10>", symbol.ToTestDisplayString());
    }

    [Fact]
    public void Member_InstanceMethod()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [netstandard]System.Object
    {
        // Methods
        .method private hidebysig
            instance void M () cil managed
        {
            // Method begins at RVA 0x2067
            // Code size 1 (0x1)
            .maxstack 8
            IL_0000: ret
        } // end of method '<>E__0'::M
    } // end of class <>E__0
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["M"], symbol.MemberNames);
        Assert.Empty(symbol.ContainingType.MemberNames);
        Assert.Equal("void Extensions.<>E__0.M()", symbol.GetMember("M").ToTestDisplayString());
    }

    [Fact]
    public void Member_ExtensionMethod()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        void M(this int i) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,14): error CS1109: Extension methods must be defined in a top level static class;  is a nested class
            //         void M(this int i) { }
            Diagnostic(ErrorCode.ERR_ExtensionMethodsDecl, "M").WithArguments("").WithLocation(5, 14));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(method);
        Assert.Equal("void Extensions.<>E__0.M(this System.Int32 i)", symbol.ToTestDisplayString());
        Assert.True(symbol.IsExtensionMethod);
    }

    [Fact]
    public void Member_StaticMethod()
    {
        var src = """
public static class Extensions
{
    extension(object)
    {
        static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [netstandard]System.Object
    {
        // Methods
        .method private hidebysig static
            void M () cil managed
        {
            // Method begins at RVA 0x2067
            // Code size 1 (0x1)
            .maxstack 8
            IL_0000: ret
        } // end of method '<>E__0'::M
    } // end of class <>E__0
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["M"], symbol.MemberNames);
        Assert.Equal("void Extensions.<>E__0.M()", symbol.GetMember("M").ToTestDisplayString());
    }

    [Fact]
    public void Member_InstanceMethod_ExplicitInterfaceImplementation()
    {
        var src = """
public interface I
{
    void M();
}

public static class Extensions
{
    extension(object o)
    {
        void I.M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (10,16): error CS0541: 'Extensions.extension.M()': explicit interface declaration can only be declared in a class, record, struct or interface
            //         void I.M() { }
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M").WithArguments("Extensions.extension.M()").WithLocation(10, 16));
    }

    [Fact]
    public void Member_InstanceMethod_ShadowingTypeParameter()
    {
        var src = """
public static class Extensions
{
    extension<T>(object o)
    {
        void M<T>() { }
        void M2(int T) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,16): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'Extensions.extension<T>'
            //         void M<T>() { }
            Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "Extensions.extension<T>").WithLocation(5, 16));
    }

    [Fact]
    public void Member_InstanceProperty()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        int Property { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [netstandard]System.Object
    {
        // Methods
        .method private hidebysig specialname
            instance int32 get_Property () cil managed
        {
            // Method begins at RVA 0x2067
            // Code size 3 (0x3)
            .maxstack 8
            IL_0000: ldc.i4.s 42
            IL_0002: ret
        } // end of method '<>E__0'::get_Property
        .method private hidebysig specialname
            instance void set_Property (
                int32 'value'
            ) cil managed
        {
            // Method begins at RVA 0x206b
            // Code size 1 (0x1)
            .maxstack 8
            IL_0000: ret
        } // end of method '<>E__0'::set_Property
        // Properties
        .property instance int32 Property()
        {
            .get instance int32 Extensions/'<>E__0'::get_Property()
            .set instance void Extensions/'<>E__0'::set_Property(int32)
        }
    } // end of class <>E__0
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["Property"], symbol.MemberNames);
        Assert.Equal("System.Int32 Extensions.<>E__0.Property { get; set; }", symbol.GetMember("Property").ToTestDisplayString());

        AssertEx.Equal([
            "System.Int32 Extensions.<>E__0.Property { get; set; }",
            "System.Int32 Extensions.<>E__0.Property.get",
            "void Extensions.<>E__0.Property.set"],
            symbol.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Member_InstanceProperty_Auto()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        int Property { get; set; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,13): error CS9501: Extension declarations can include only methods or properties
            //         int Property { get; set; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "Property").WithLocation(5, 13));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["Property"], symbol.MemberNames);
        Assert.Equal("System.Int32 Extensions.<>E__0.Property { get; set; }", symbol.GetMember("Property").ToTestDisplayString());

        AssertEx.Equal([
            "System.Int32 Extensions.<>E__0.<Property>k__BackingField",
            "System.Int32 Extensions.<>E__0.Property { get; set; }",
            "System.Int32 Extensions.<>E__0.Property.get",
            "void Extensions.<>E__0.Property.set"],
            symbol.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Member_InstanceProperty_ExplicitInterfaceImplementation()
    {
        var src = """
public interface I
{
    int Property { get; set; }
}
public static class Extensions
{
    extension(object o)
    {
        int I.Property { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (9,15): error CS0541: 'Extensions.extension.Property': explicit interface declaration can only be declared in a class, record, struct or interface
            //         int I.Property { get => 42; set { } }
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "Property").WithArguments("Extensions.extension.Property").WithLocation(9, 15));
    }

    [Fact]
    public void Member_StaticProperty()
    {
        var src = """
public static class Extensions
{
    extension(object)
    {
        static int Property { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [netstandard]System.Object
    {
        // Methods
        .method private hidebysig specialname static
            int32 get_Property () cil managed
        {
            // Method begins at RVA 0x2067
            // Code size 3 (0x3)
            .maxstack 8
            IL_0000: ldc.i4.s 42
            IL_0002: ret
        } // end of method '<>E__0'::get_Property
        .method private hidebysig specialname static
            void set_Property (
                int32 'value'
            ) cil managed
        {
            // Method begins at RVA 0x206b
            // Code size 1 (0x1)
            .maxstack 8
            IL_0000: ret
        } // end of method '<>E__0'::set_Property
        // Properties
        .property int32 Property()
        {
            .get int32 Extensions/'<>E__0'::get_Property()
            .set void Extensions/'<>E__0'::set_Property(int32)
        }
    } // end of class <>E__0
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["Property"], symbol.MemberNames);
        Assert.Equal("System.Int32 Extensions.<>E__0.Property { get; set; }", symbol.GetMember("Property").ToTestDisplayString());
    }

    [Fact]
    public void Member_StaticProperty_Auto()
    {
        var src = """
public static class Extensions
{
    extension(object)
    {
        static int Property { get; set; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,20): error CS9501: Extension declarations can include only methods or properties
            //         static int Property { get; set; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "Property").WithLocation(5, 20));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["Property"], symbol.MemberNames);
        AssertEx.Equal([
            "System.Int32 Extensions.<>E__0.<Property>k__BackingField",
            "System.Int32 Extensions.<>E__0.Property { get; set; }",
            "System.Int32 Extensions.<>E__0.Property.get",
            "void Extensions.<>E__0.Property.set"],
            symbol.GetMembers().ToTestDisplayStrings());

        Assert.Equal("System.Int32 Extensions.<>E__0.Property { get; set; }", symbol.GetMember("Property").ToTestDisplayString());
    }

    [Fact]
    public void Member_InstanceIndexer()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        int this[int i] { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        VerifyTypeIL(verifier, "Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [netstandard]System.Object
    {
        .custom instance void [netstandard]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
            01 00 04 49 74 65 6d 00 00
        )
        // Methods
        .method private hidebysig specialname 
            instance int32 get_Item (
                int32 i
            ) cil managed 
        {
            // Method begins at RVA 0x2067
            // Code size 3 (0x3)
            .maxstack 8
            IL_0000: ldc.i4.s 42
            IL_0002: ret
        } // end of method '<>E__0'::get_Item
        .method private hidebysig specialname 
            instance void set_Item (
                int32 i,
                int32 'value'
            ) cil managed 
        {
            // Method begins at RVA 0x206b
            // Code size 1 (0x1)
            .maxstack 8
            IL_0000: ret
        } // end of method '<>E__0'::set_Item
        // Properties
        .property instance int32 Item(
            int32 i
        )
        {
            .get instance int32 Extensions/'<>E__0'::get_Item(int32)
            .set instance void Extensions/'<>E__0'::set_Item(int32, int32)
        }
    } // end of class <>E__0
} // end of class Extensions
""");

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Equal(["this[]"], symbol.MemberNames);
        Assert.Equal("System.Int32 Extensions.<>E__0.this[System.Int32 i] { get; set; }", symbol.GetMember("this[]").ToTestDisplayString());

        AssertEx.Equal([
            "System.Int32 Extensions.<>E__0.this[System.Int32 i] { get; set; }",
            "System.Int32 Extensions.<>E__0.this[System.Int32 i].get",
            "void Extensions.<>E__0.this[System.Int32 i].set"],
            symbol.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Member_StaticIndexer()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        static int this[int i] { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,20): error CS0106: The modifier 'static' is not valid for this item
            //         static int this[int i] { get => 42; set { } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(5, 20));
    }

    [Fact]
    public void Member_Type()
    {
        var src = """
public static class Extensions
{
    extension(object)
    {
        class Nested { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,15): error CS9501: Extension declarations can include only methods or properties
            //         class Nested { }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "Nested").WithLocation(5, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Empty(symbol.MemberNames);
        Assert.Equal(["Extensions.<>E__0.Nested"], symbol.GetMembers().ToTestDisplayStrings());
        Assert.Equal(["Extensions.<>E__0.Nested"], symbol.GetTypeMembers().ToTestDisplayStrings());
        Assert.Equal("Extensions.<>E__0.Nested", symbol.GetTypeMember("Nested").ToTestDisplayString());
    }

    [Fact]
    public void Member_Constructor()
    {
        var src = """
public static class Extensions
{
    extension(object) { Extensions() { } }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,25): error CS1520: Method must have a return type
            //     extension(object) { Extensions() { } }
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "Extensions").WithLocation(3, 25),
            // (3,25): error CS9501: Extension declarations can include only methods or properties
            //     extension(object) { Extensions() { } }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "Extensions").WithLocation(3, 25));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);

        Assert.Equal([".ctor"], symbol.MemberNames);
        Assert.Equal(["Extensions.<>E__0..ctor()"], symbol.InstanceConstructors.ToTestDisplayStrings());
        Assert.Empty(symbol.StaticConstructors);
        Assert.Equal(["Extensions.<>E__0..ctor()"], symbol.Constructors.ToTestDisplayStrings());
    }

    [Fact]
    public void Member_Finalizer()
    {
        var src = """
public static class Extensions
{
    extension(object) { ~Extensions() { } }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,26): error CS9501: Extension declarations can include only methods or properties
            //     extension(object) { ~Extensions() { } }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "Extensions").WithLocation(3, 26));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);

        Assert.Equal(["Finalize"], symbol.MemberNames);
        Assert.Equal(["void Extensions.<>E__0.Finalize()"], symbol.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Member_Field()
    {
        var src = """
_ = new object().field;

public static class Extensions
{
    extension(object o) { int field = 0; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,18): error CS1061: 'object' does not contain a definition for 'field' and no accessible extension method 'field' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // _ = new object().field;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "field").WithArguments("object", "field").WithLocation(1, 18),
            // (5,31): error CS9501: Extension declarations can include only methods or properties
            //     extension(object o) { int field = 0; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "field").WithLocation(5, 31),
            // (5,31): warning CS0169: The field 'Extensions.extension.field' is never used
            //     extension(object o) { int field = 0; }
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("Extensions.extension.field").WithLocation(5, 31));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);

        Assert.Equal(["field"], symbol.MemberNames);
        Assert.Equal(["System.Int32 Extensions.<>E__0.field"], symbol.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Member_Const()
    {
        var src = """
public static class Extensions
{
    extension(object) { const int i = 0; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,35): error CS9501: Extension declarations can include only methods or properties
            //     extension(object) { const int i = 0; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "i").WithLocation(3, 35));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        AssertExtensionDeclaration(symbol);

        Assert.Equal(["i"], symbol.MemberNames);
        Assert.Equal(["System.Int32 Extensions.<>E__0.i"], symbol.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void Member_InstanceEvent_ExplicitInterfaceImplementation()
    {
        var src = """
public interface I
{
    event System.Action E;
}
public static class Extensions
{
    extension(object o)
    {
        event System.Action I.E { add { } remove { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (9,31): error CS0541: 'Extensions.extension.E': explicit interface declaration can only be declared in a class, record, struct or interface
            //         event System.Action I.E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "E").WithArguments("Extensions.extension.E").WithLocation(9, 31),
            // (9,31): error CS9501: Extension declarations can include only methods or properties
            //         event System.Action I.E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "E").WithLocation(9, 31),
            // (9,35): error CS9501: Extension declarations can include only methods or properties
            //         event System.Action I.E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "add").WithLocation(9, 35),
            // (9,43): error CS9501: Extension declarations can include only methods or properties
            //         event System.Action I.E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "remove").WithLocation(9, 43));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void IsExtension_MiscTypeKinds(string typeKind)
    {
        var src = $$"""
{{typeKind}} C { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(type);
        Assert.False(symbol.IsExtension);
    }

    [Fact]
    public void IsExtension_Delegate()
    {
        var src = $$"""
delegate void C();
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        Assert.False(symbol.IsExtension);
    }

    [Fact]
    public void IsExtension_Enum()
    {
        var src = $$"""
enum E { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        Assert.False(symbol.IsExtension);
    }

    [Fact]
    public void Attributes_01()
    {
        var src = """
public static class Extensions
{
    [System.Obsolete]
    extension(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,6): error CS0592: Attribute 'System.Obsolete' is not valid on this declaration type. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
            //     [System.Obsolete]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Obsolete").WithArguments("System.Obsolete", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(3, 6));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        AssertEx.SetEqual(["System.ObsoleteAttribute"], symbol.GetAttributes().Select(a => a.ToString()));
    }

    [Fact]
    public void Attributes_02()
    {
        var src = """
public static class Extensions
{
    [My(nameof(o)), My(nameof(Extensions))]
    extension(object o) { }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string s) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0103: The name 'o' does not exist in the current context
            //     [My(nameof(o)), My(nameof(Extensions))]
            Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(3, 16),
            // (3,21): error CS0592: Attribute 'My' is not valid on this declaration type. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
            //     [My(nameof(o)), My(nameof(Extensions))]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "My").WithArguments("My", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter").WithLocation(3, 21));
    }

    [Fact]
    public void ReceiverParameter()
    {
        var src = """
public static class Extensions
{
    extension(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("System.Object", internalSymbol.ExtensionParameter.ToTestDisplayString());
    }

    [Fact]
    public void ReceiverParameter_WithIdentifier()
    {
        var src = """
public static class Extensions
{
    extension(object o) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("System.Object o", internalSymbol.ExtensionParameter.ToTestDisplayString());
    }

    [Fact]
    public void ReceiverParameter_Multiple()
    {
        var src = """
public static class Extensions
{
    extension(int i, int j, C c) { }
}
class C { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,22): error CS9504: An extension container can have only one receiver parameter
            //     extension(int i, int j, C c) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "int j").WithLocation(3, 22),
            // (3,29): error CS9504: An extension container can have only one receiver parameter
            //     extension(int i, int j, C c) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "C c").WithLocation(3, 29));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        var parameter = internalSymbol.ExtensionParameter;

        var parameter1 = parameter;
        Assert.Equal("System.Int32 i", parameter1.ToTestDisplayString());
        Assert.True(parameter1.Equals(parameter1));

        var parameterSyntaxes = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().ToArray();
        // PROTOTYPE semantic model is undone
        Assert.Null(model.GetDeclaredSymbol(parameterSyntaxes[0]));
        //Assert.Same(parameter1, model.GetDeclaredSymbol(parameterSyntaxes[0]));

        Assert.Equal("System.Int32", model.GetTypeInfo(parameterSyntaxes[1].Type).Type.ToTestDisplayString());
        Assert.Equal("C", model.GetTypeInfo(parameterSyntaxes[2].Type).Type.ToTestDisplayString());
    }

    [Fact]
    public void ReceiverParameter_Multiple_MissingType()
    {
        var src = """
public static class Extensions
{
    extension(int i, Type) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,22): error CS9504: An extension container can have only one receiver parameter
            //     extension(int i, Type) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "Type").WithLocation(3, 22));
    }

    [Fact]
    public void ReceiverParameter_TypeParameter_Found()
    {
        var src = """
public static class Extensions
{
    extension<T>(T) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        var extensionParameter = internalSymbol.ExtensionParameter;
        Assert.Equal("T", extensionParameter.ToTestDisplayString());
        Assert.Same(extensionParameter.Type, internalSymbol.TypeParameters[0]);
    }

    [Fact]
    public void ReceiverParameter_TypeParameter_Found_FromContainingType()
    {
        var src = """
public static class Extensions<T>
{
    extension(T) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(T) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        var extensionParameter = internalSymbol.ExtensionParameter;
        Assert.Equal("T", extensionParameter.ToTestDisplayString());
        Assert.Same(extensionParameter.Type, internalSymbol.ContainingType.TypeParameters[0]);
    }

    [Fact]
    public void ReceiverParameter_TypeParameter_Missing()
    {
        var src = """
public static class Extensions
{
    extension(T)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)
            //     extension(T)
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "T").WithArguments("T").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        var parameter = internalSymbol.ExtensionParameter;
        Assert.Equal("T", parameter.ToTestDisplayString());
        Assert.True(parameter.Type.IsErrorType());
    }

    [Fact]
    public void ReceiverParameter_TypeParameter_Unreferenced_01()
    {
        var src = """
int.M();

public static class Extensions
{
    extension<T>(int) 
    {
        public static void M() { }
    }
}
""";
        // PROTOTYPE report a declaration error for unreferenced type parameter
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void ReceiverParameter_TypeParameter_Unreferenced_02()
    {
        var src = """
int.M();

public static class Extensions
{
    extension<T1, T2>(T1) 
    {
        public static void M() { }
    }
}
""";
        // PROTOTYPE report a declaration error for unreferenced type parameter
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void ReceiverParameter_TypeParameter_Unreferenced_03()
    {
        var src = """
int.M();

public static class Extensions
{
    extension<T1, T2>(T1) where T1 : class
    {
        public static void M() { }
    }
}
""";
        // PROTOTYPE report a declaration error for unreferenced type parameter
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void ReceiverParameter_TypeParameter_Missing_Local()
    {
        var src = """
public static class Extensions
{
    extension(T)
    {
        void T() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)
            //     extension(T)
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "T").WithArguments("T").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_Params()
    {
        var src = """
public static class Extensions
{
    extension(params int[] i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1670: params is not valid in this context
            //     extension(params int[] i) { }
            Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(type1);
        var internalSymbol1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        var parameter = internalSymbol1.ExtensionParameter;
        Assert.Equal("System.Int32[] i", parameter.ToTestDisplayString());
        Assert.False(parameter.IsParams);
    }

    [Fact]
    public void ReceiverParameter_Params_BadType()
    {
        var src = """
public static class Extensions
{
    extension(params int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1670: params is not valid in this context
            //     extension(params int i) { }
            Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_Params_NotLast()
    {
        var src = """
public static class Extensions
{
    extension(params int[] i, int j) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1670: params is not valid in this context
            //     extension(params int[] i, int j) { }
            Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(3, 15),
            // (3,31): error CS9504: An extension container can have only one receiver parameter
            //     extension(params int[] i, int j) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "int j").WithLocation(3, 31));
    }

    [Fact]
    public void ReceiverParameter_ParameterTypeViolatesConstraint()
    {
        var src = """
public static class Extensions
{
    extension<T>(C<T>) { }
    extension<T2>(C<T2>) where T2 : struct { }
}

public class C<T> where T : struct { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,18): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C<T>'
            //     extension<T>(C<T>) { }
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C<T>").WithArguments("C<T>", "T", "T").WithLocation(3, 18));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(type1);
        var internalSymbol1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        var parameter = internalSymbol1.ExtensionParameter;
        Assert.Equal("C<T>", parameter.ToTestDisplayString());
    }

    [Fact]
    public void ReceiverParameter_DefaultValue()
    {
        var src = """
public static class Extensions
{
    extension(int i = 0) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9503: The receiver parameter of an extension cannot have a default value
            //     extension(int i = 0) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "int i = 0").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        var parameter = internalSymbol.ExtensionParameter;
        Assert.True(parameter.HasExplicitDefaultValue); // PROTOTYPE consider not recognizing the default value entirely
    }

    [Fact]
    public void ReceiverParameter_DefaultValue_BeforeAnotherParameter()
    {
        var src = """
public static class Extensions
{
    extension(int i = 0, object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9503: The receiver parameter of an extension cannot have a default value
            //     extension(int i = 0, object) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "int i = 0").WithLocation(3, 15),
            // (3,26): error CS9504: An extension container can have only one receiver parameter
            //     extension(int i = 0, object) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "object").WithLocation(3, 26));
    }

    [Fact]
    public void ReceiverParameter_DefaultValue_BadValue()
    {
        var src = """
public static class Extensions
{
    extension(int i = null) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9503: The receiver parameter of an extension cannot have a default value
            //     extension(int i = null) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "int i = null").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_DefaultValue_RefReadonly()
    {
        var src = """
public static class Extensions
{
    extension(ref readonly int x = 2) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9503: The receiver parameter of an extension cannot have a default value
            //     extension(ref readonly int x = 2) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "ref readonly int x = 2").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_Attributes_01()
    {
        var src = """
public static class Extensions
{
    extension([System.Runtime.InteropServices.DefaultParameterValue(1)] int o = 2) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9503: The receiver parameter of an extension cannot have a default value
            //     extension([System.Runtime.InteropServices.DefaultParameterValue(1)] int o = 2) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "[System.Runtime.InteropServices.DefaultParameterValue(1)] int o = 2").WithLocation(3, 15),
            // (3,16): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
            //     extension([System.Runtime.InteropServices.DefaultParameterValue(1)] int o = 2) { }
            Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "System.Runtime.InteropServices.DefaultParameterValue").WithLocation(3, 16));
    }

    [Fact]
    public void ReceiverParameter_Attributes_02()
    {
        var src = """
public static class Extensions
{
    extension([System.Runtime.InteropServices.Optional, System.Runtime.InteropServices.DefaultParameterValue(1)] ref readonly int i) { }
    extension([System.Runtime.InteropServices.Optional, System.Runtime.InteropServices.DefaultParameterValue(2)] ref readonly int) { }
}
""";
        var comp = CreateCompilation(src);
        // Note: we use "" name in the diagnostic for the second parameter
        // Note: these attributes are allowed on the receiver parameter of an extension method
        comp.VerifyEmitDiagnostics(
            // (3,57): warning CS9200: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     extension([System.Runtime.InteropServices.Optional, System.Runtime.InteropServices.DefaultParameterValue(1)] ref readonly int i) { }
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "System.Runtime.InteropServices.DefaultParameterValue(1)").WithArguments("i").WithLocation(3, 57),
            // (4,57): warning CS9200: A default value is specified for 'ref readonly' parameter '', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
            //     extension([System.Runtime.InteropServices.Optional, System.Runtime.InteropServices.DefaultParameterValue(2)] ref readonly int) { }
            Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "System.Runtime.InteropServices.DefaultParameterValue(2)").WithArguments("").WithLocation(4, 57));
    }

    [Fact]
    public void ReceiverParameter_Attributes_03()
    {
        var src = """
public static class Extensions
{
    extension([System.Runtime.CompilerServices.ParamCollectionAttribute] int[] xs) { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0674: Do not use 'System.ParamArrayAttribute'/'System.Runtime.CompilerServices.ParamCollectionAttribute'. Use the 'params' keyword instead.
            //     extension([System.Runtime.CompilerServices.ParamCollectionAttribute] int[] xs) { }
            Diagnostic(ErrorCode.ERR_ExplicitParamArrayOrCollection, "System.Runtime.CompilerServices.ParamCollectionAttribute").WithLocation(3, 16));
    }

    [Fact]
    public void ReceiverParameter_Attributes_04()
    {
        var src = """
public static class Extensions
{
    extension([System.Runtime.CompilerServices.CallerLineNumber] int x = 2) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9503: The receiver parameter of an extension cannot have a default value
            //     extension([System.Runtime.CompilerServices.CallerLineNumber] int x = 2) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "[System.Runtime.CompilerServices.CallerLineNumber] int x = 2").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_Attributes_54()
    {
        var src = """
public static class Extensions
{
    extension([System.Runtime.CompilerServices.CallerLineNumber] int x) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS4020: The CallerLineNumberAttribute may only be applied to parameters with default values
            //     extension([System.Runtime.CompilerServices.CallerLineNumber] int x) { }
            Diagnostic(ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue, "System.Runtime.CompilerServices.CallerLineNumber").WithLocation(3, 16));
    }

    [Fact]
    public void ReceiverParameter_Attributes_06()
    {
        var src = """
public static class Extensions
{
    extension([My] int x) { }
}
public class MyAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var parameter = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
        var parameterSymbol = model.GetDeclaredSymbol(parameter);
        // PROTOTYPE semantic model is undone
        Assert.Null(parameterSymbol);
        //AssertEx.SetEqual(["MyAttribute"], parameterSymbol.GetAttributes().Select(a => a.ToString()));

        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var extensionSymbol = model.GetDeclaredSymbol(type);
        var parameterSymbol2 = extensionSymbol.GetSymbol<SourceNamedTypeSymbol>().ExtensionParameter;
        AssertEx.SetEqual(["MyAttribute"], parameterSymbol2.GetAttributes().Select(a => a.ToString()));
    }

    [Fact]
    public void ReceiverParameter_This_01()
    {
        var src = """
public static class Extensions
{
    extension(this int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS0027: Keyword 'this' is not available in the current context
            //     extension(this int i) { }
            Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_This_02()
    {
        var src = """
public static class Extensions
{
    extension(int i, this int j) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,22): error CS9504: An extension container can have only one receiver parameter
            //     extension(int i, this int j) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "this int j").WithLocation(3, 22));
    }

    [Fact]
    public void ReceiverParameter_This_03()
    {
        var src = """
public static class Extensions
{
    extension(this this int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,20): error CS1107: A parameter can only have one 'this' modifier
            //     extension(this this int i) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "this").WithArguments("this").WithLocation(3, 20),
            // (3,20): error CS0027: Keyword 'this' is not available in the current context
            //     extension(this this int i) { }
            Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(3, 20));
    }

    [Fact]
    public void ReceiverParameter_Ref_01()
    {
        var src = """
public static class Extensions
{
    extension(ref int i) { }
    static void M(this ref int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_Ref_02()
    {
        var src = """
public static class Extensions
{
    extension(ref ref int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,19): error CS1107: A parameter can only have one 'ref' modifier
            //     extension(ref ref int i) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(3, 19));
    }

    [Fact]
    public void ReceiverParameter_Out_01()
    {
        var src = """
public static class Extensions
{
    extension(out int i) { }
    static void M(this out int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS8328:  The parameter modifier 'out' cannot be used with 'extension'
            //     extension(out int i) { }
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "extension").WithLocation(3, 15),
            // (4,17): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
            //     static void M(this out int i) { }
            Diagnostic(ErrorCode.ERR_ParamUnassigned, "M").WithArguments("i").WithLocation(4, 17),
            // (4,24): error CS8328:  The parameter modifier 'out' cannot be used with 'this'
            //     static void M(this out int i) { }
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this").WithLocation(4, 24));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type1 = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().First();
        var symbol1 = model.GetDeclaredSymbol(type1);
        var internalSymbol1 = symbol1.GetSymbol<SourceNamedTypeSymbol>();
        var parameter = internalSymbol1.ExtensionParameter;
        Assert.Equal("out System.Int32 i", parameter.ToTestDisplayString());
        Assert.Equal(RefKind.Out, parameter.RefKind);
    }

    [Fact]
    public void ReceiverParameter_Out_02()
    {
        var src = """
public static class Extensions
{
    extension(int i, out int j) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,22): error CS9504: An extension container can have only one receiver parameter
            //     extension(int i, out int j) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "out int j").WithLocation(3, 22));
    }

    [Fact]
    public void ReceiverParameter_Out_03()
    {
        var src = """
public static class Extensions
{
    extension(out out int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS8328:  The parameter modifier 'out' cannot be used with 'extension'
            //     extension(out out int i) { }
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "extension").WithLocation(3, 15),
            // (3,19): error CS8328:  The parameter modifier 'out' cannot be used with 'extension'
            //     extension(out out int i) { }
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "extension").WithLocation(3, 19));
    }

    [Fact]
    public void ReceiverParameter_In_01()
    {
        var src = """
public static class Extensions
{
    extension(in int i) { }
    static void M(this in int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_In_02()
    {
        var src = """
public static class Extensions
{
    extension(in in int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,18): error CS1107: A parameter can only have one 'in' modifier
            //     extension(in in int i) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "in").WithArguments("in").WithLocation(3, 18));
    }

    [Fact]
    public void ReceiverParameter_RefReadonly()
    {
        var src = """
public static class Extensions
{
    extension(ref readonly int i) { }
    static void M(this ref readonly int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_ReadonlyRef()
    {
        var src = """
public static class Extensions
{
    extension(readonly ref int i) { }
    static void M(this readonly ref int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     extension(readonly ref int i) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 15),
            // (4,24): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     static void M(this readonly ref int i) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(4, 24));
    }

    [Fact]
    public void ReceiverParameter_ArgList_01()
    {
        var src = """
_ = object.P;

public static class Extensions
{
    extension(__arglist) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,12): error CS0117: 'object' does not contain a definition for 'P'
            // _ = object.P;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "P").WithArguments("object", "P").WithLocation(1, 12),
            // (5,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist) { }
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(5, 15));
    }

    [Fact]
    public void ReceiverParameter_ArgList_02()
    {
        var src = """
public static class Extensions
{
    extension(__arglist, int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist, int i) { }
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
            // (3,26): error CS9504: An extension container can have only one receiver parameter
            //     extension(__arglist, int i) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "int i").WithLocation(3, 26));
    }

    [Fact]
    public void ReceiverParameter_StaticType_01()
    {
        var src = """
public static class Extensions
{
    extension(Extensions) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_StaticType_02()
    {
        var src = """
public static class Extensions
{
    extension(object o, Extensions e) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,25): error CS9504: An extension container can have only one receiver parameter
            //     extension(object o, Extensions e) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "Extensions e").WithLocation(3, 25));
    }

    [Fact]
    public void ReceiverParameter_StaticType_03()
    {
        var src = """
public static class Extensions
{
    extension(Extensions e) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS0721: 'Extensions': static types cannot be used as parameters
            //     extension(Extensions) { }
            Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "Extensions").WithArguments("Extensions").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_StaticType_04()
    {
        var src = """
extension(C) { }
extension(C c) { }
static class C { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            // extension(C) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(1, 1),
            // (2,1): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            // extension(C c) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(2, 1),
            // (2,11): error CS0721: 'C': static types cannot be used as parameters
            // extension(C c) { }
            Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(2, 11));
    }

    [Fact]
    public void ReceiverParameter_InstanceType_01()
    {
        var src = """
public static class Extensions
{
    extension(C c) { }
    extension(C) { }
}
class C { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_ShadowingTypeParameter()
    {
        var src = """
public static class Extensions<T>
{
    extension(object T) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(object T) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));
    }

    [Fact]
    public void ReceiverParameter_Ref()
    {
        var src = """
public static class Extensions
{
    extension(ref int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_Scoped_01()
    {
        var src = """
public static class Extensions
{
    extension(scoped int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
            //     extension(scoped int i) { }
            Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped int i").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_Scoped_02()
    {
        var src = """
public static class Extensions
{
    extension(int i, scoped int j) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,22): error CS9504: An extension container can have only one receiver parameter
            //     extension(int i, scoped int j) { }
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "scoped int j").WithLocation(3, 22));
    }

    [Fact]
    public void ReceiverParameter_Scoped_03()
    {
        var src = """
public static class Extensions
{
    extension(scoped System.Span<int> i) { }
    public static void M(this scoped System.Span<int> i) { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_Nullable_01()
    {
        var src = """
public static class Extensions
{
    extension(string?) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,21): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            //     extension(string?) { }
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(3, 21));
    }

    [Fact]
    public void ReceiverParameter_Nullable_02()
    {
        var src = """
#nullable enable
public static class Extensions
{
    extension(string?) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_RestrictedType()
    {
        var src = """
public static class Extensions
{
    extension(ref System.ArgIterator) { }
    extension(ref System.Span<int>) { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1601: Cannot make reference to variable of type 'ArgIterator'
            //     extension(ref System.ArgIterator) { }
            Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(3, 15));
    }

    [Fact]
    public void ReceiverParameter_Empty()
    {
        var src = """
public static class Extensions
{
    extension() { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1031: Type expected
            //     extension() { }
            Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(3, 15));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var type = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();
        var symbol = model.GetDeclaredSymbol(type);
        var internalSymbol = symbol.GetSymbol<SourceNamedTypeSymbol>();
        Assert.Equal("?", internalSymbol.ExtensionParameter.ToTestDisplayString());
    }

    [Fact]
    public void Skeleton()
    {
        var src = """
public static class Extensions
{
    extension(object)
    {
        void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify(comp);
        // PROTOTYPE metadata is undone
        verifier.VerifyIL("Extensions.<>E__0.M()", """
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "ran"
  IL_0005:  call       "void System.Console.Write(string)"
  IL_000a:  ret
}
""");
    }

    [Fact]
    public void InstanceMethodInvocation_Simple()
    {
        var src = """
new object().M();

public static class Extensions
{
    extension(object o)
    {
        public void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new object().M()");
        Assert.Equal("void Extensions.<>E__0.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Inaccessible()
    {
        var src = """
new object().M();

public static class Extensions
{
    extension(object o)
    {
        void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS1061: 'object' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // new object().M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("object", "M").WithLocation(1, 14));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new object().M()");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        // PROTOTYPE semantic model is undone
        Assert.Equal([], model.GetSymbolInfo(invocation).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void InstanceMethodInvocation_GenericReceiverParameter()
    {
        var src = """
new object().M();

public static class Extensions
{
    extension<T>(T t)
    {
        public void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new object().M()");
        Assert.Equal("void Extensions.<>E__0<System.Object>.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void StaticMethodInvocation_GenericReceiverParameter_Constrained()
    {
        var src = """
object.M();
int.M();

public static class Extensions
{
    extension<T>(T) where T : struct
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("object", "M").WithLocation(1, 8));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "object.M()");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);

        invocation = GetSyntax<InvocationExpressionSyntax>(tree, "int.M()");
        Assert.Equal("void Extensions.<>E__0<System.Int32>.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ReceiverParameter_TypeWithUseSiteError()
    {
        var lib1_cs = "public class MissingBase { }";
        var comp1 = CreateCompilation(lib1_cs, assemblyName: "missing");
        comp1.VerifyDiagnostics();

        var lib2_cs = "public class UseSiteError : MissingBase { }";
        var comp2 = CreateCompilation(lib2_cs, [comp1.EmitToImageReference()]);
        comp2.VerifyDiagnostics();

        var src = """
class C<T> { }
static class Extensions
{
    extension(UseSiteError) { }
    extension(C<UseSiteError>) { }
}

class C1 
{
    void M(UseSiteError x) { }
    void M(C<UseSiteError> x) { }
}
""";
        var comp = CreateCompilation(src, [comp2.EmitToImageReference()]);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ReceiverParameter_SuppressConstraintChecksInitially()
    {
        var text = @"
public class C1<T> where T : struct { }

public static class Extensions
{
    extension<T>(C1<T>) { }
}
";
        var comp = CreateCompilation(text);
        comp.VerifyEmitDiagnostics(
            // (6,18): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C1<T>'
            //     extension<T>(C1<T>) { }
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C1<T>").WithArguments("C1<T>", "T", "T").WithLocation(6, 18));
    }

    [Fact]
    public void ReceiverParameter_SuppressConstraintChecksInitially_PointerAsTypeArgument()
    {
        var text = @"
public class C<T> { }

unsafe static class Extensions
{
    extension(C<int*>) { }
}
";
        var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (6,15): error CS0306: The type 'int*' may not be used as a type argument
            //     extension(C<int*>) { }
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "C<int*>").WithArguments("int*").WithLocation(6, 15));
    }

    [Fact]
    public void InstanceMethodInvocation_VariousScopes_Errors()
    {
        var cSrc = """
class C
{
    public static void Main()
    {
        new object().Method();
        _ = new object().Property;
    }
}
""";

        var eSrc = """
static class Extensions
{
    extension(object o)
    {
        public void Method() => throw null;
        public int Property => throw null;
    }
}
""";

        var src1 = $$"""
namespace N
{
    {{cSrc}}
    namespace N2
    {
        {{eSrc}}
    }
}
""";

        verify(src1,
            // (7,22): error CS1061: 'object' does not contain a definition for 'Method' and no accessible extension method 'Method' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            //         new object().Method();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Method").WithArguments("object", "Method").WithLocation(7, 22),
            // (8,26): error CS1061: 'object' does not contain a definition for 'Property' and no accessible extension method 'Property' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            //         _ = new object().Property;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Property").WithArguments("object", "Property").WithLocation(8, 26));

        var src2 = $$"""
file {{eSrc}}
""";

        verify(new[] { cSrc, src2 },
            // 0.cs(5,22): error CS1061: 'object' does not contain a definition for 'Method' and no accessible extension method 'Method' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            //         new object().Method();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Method").WithArguments("object", "Method").WithLocation(5, 22),
            // 0.cs(6,26): error CS1061: 'object' does not contain a definition for 'Property' and no accessible extension method 'Property' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            //         _ = new object().Property;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Property").WithArguments("object", "Property").WithLocation(6, 26));

        static void verify(CSharpTestSource src, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(expected);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().Method");
            Assert.Null(model.GetSymbolInfo(method).Symbol);
            Assert.Empty(model.GetMemberGroup(method));

            var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().Property");
            Assert.Null(model.GetSymbolInfo(property).Symbol);
            Assert.Empty(model.GetMemberGroup(property));
        }
    }

    [Fact]
    public void InstanceMethodInvocation_FromUsingNamespace()
    {
        var cSrc = """
class C
{
    public static void Main()
    {
        new object().Method();
    }
}
""";

        var eSrc = """
namespace N2
{
    static class E
    {
        extension(object o)
        {
            public void Method() => throw null;
        }
    }
}
""";

        var src1 = $$"""
using N2;
{{cSrc}}

{{eSrc}}
""";
        verify(src1, "N2.E.<>E__0");

        var src2 = $$"""
using N2;
using N2; // 1, 2
{{cSrc}}

{{eSrc}}
""";

        var comp = CreateCompilation(src2, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics(
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using N2; // 1, 2
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1),
            // (2,7): warning CS0105: The using directive for 'N2' appeared previously in this namespace
            // using N2; // 1, 2
            Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(2, 7)
            );

        var src3 = $$"""
namespace N3
{
    using N2;

    namespace N4
    {
        {{cSrc}}
    }

    {{eSrc}}
}
""";
        verify(src3, "N3.N2.E.<>E__0");

        void verify(string src, string extensionName)
        {
            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            // PROTOTYPE metadata is undone
            //CompileAndVerify(comp, expectedOutput: "").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var invocation = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().Method");
            Assert.Equal($$"""void {{extensionName}}.Method()""", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
            // PROTOTYPE semantic model is undone
            Assert.Empty(model.GetMemberGroup(invocation));
        }
    }

    [Fact]
    public void InstanceMethodInvocation_UsingNamespaceNecessity()
    {
        var src = """
using N;

class C
{
    public static void Main()
    {
        new object().Method();
    }
}

""";
        var eSrc = """
namespace N
{
    public static class E
    {
        extension(object o)
        {
            public void Method() { System.Console.Write("method"); }
        }
    }
}
""";

        var comp = CreateCompilation([src, eSrc], options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "method");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var invocation = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().Method");
        Assert.Equal("void N.E.<>E__0.Method()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        // PROTOTYPE semantic model is undone
        Assert.Empty(model.GetMemberGroup(invocation));

        src = """
using N;

class C
{
    public static void Main() { }
}

namespace N
{
    public static class Extensions
    {
        extension(object o)
        {
            public void Method() { }
        }
    }
}
""";

        comp = CreateCompilation([src, eSrc]);
        comp.VerifyEmitDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1));
    }

    [Theory, CombinatorialData]
    public void InstanceMethodInvocation_Ambiguity(bool e1BeforeE2)
    {
        var e1 = """
static class E1
{
    extension(object o)
    {
        public void Method() => throw null;
    }
}
""";

        var e2 = """
static class E2
{
    extension(object o)
    {
        public void Method() => throw null;
    }
}
""";

        var src = $$"""
new object().Method();

{{(e1BeforeE2 ? e1 : e2)}}
{{(e1BeforeE2 ? e2 : e1)}}
""";
        var comp = CreateCompilation(src);
        if (!e1BeforeE2)
        {
            comp.VerifyEmitDiagnostics(
                // (1,14): error CS0121: The call is ambiguous between the following methods or properties: 'E2.extension.Method()' and 'E1.extension.Method()'
                // new object().Method();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("E2.extension.Method()", "E1.extension.Method()").WithLocation(1, 14));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (1,14): error CS0121: The call is ambiguous between the following methods or properties: 'E1.extension.Method()' and 'E2.extension.Method()'
                // new object().Method();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("E1.extension.Method()", "E2.extension.Method()").WithLocation(1, 14));
        }
    }

    [Fact]
    public void InstanceMethodInvocation_Overloads()
    {
        var src = """
new object().Method(42);
new object().Method("hello");

static class E1
{
    extension(object o)
    {
        public void Method(int i) { System.Console.Write($"E1.Method({i}) "); }
    }
}

static class E2
{
    extension(object o)
    {
        public void Method(string s) { System.Console.Write($"E2.Method({s}) "); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E1.Method(42) E2.Method(hello)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var invocation1 = GetSyntax<InvocationExpressionSyntax>(tree, "new object().Method(42)");
        Assert.Equal("void E1.<>E__0.Method(System.Int32 i)", model.GetSymbolInfo(invocation1).Symbol.ToTestDisplayString());
        // PROTOTYPE semantic model is undone
        Assert.Empty(model.GetMemberGroup(invocation1));

        var invocation2 = GetSyntax<InvocationExpressionSyntax>(tree, """new object().Method("hello")""");
        Assert.Equal("void E2.<>E__0.Method(System.String s)", model.GetSymbolInfo(invocation2).Symbol.ToTestDisplayString());
        // PROTOTYPE semantic model is undone
        Assert.Empty(model.GetMemberGroup(invocation2));
    }

    [Fact]
    public void InstanceMethodInvocation_Overloads_DifferentScopes_NestedNamespace()
    {
        var src = """
namespace N1
{
    static class E1
    {
        extension(object o)
        {
            public void Method(int i) { System.Console.Write($"E1.Method({i}) "); }
        }
    }

    namespace N2
    {
        static class E2
        {
            extension(object o)
            {
                public void Method(string s) { System.Console.Write($"E2.Method({s}) "); }
            }
        }

        class C
        {
            public static void Main()
            {
                new object().Method(42);
                new object().Method("hello");
            }
        }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E1.Method(42) E2.Method(hello)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var invocation1 = GetSyntax<InvocationExpressionSyntax>(tree, "new object().Method(42)");
        Assert.Equal("void N1.E1.<>E__0.Method(System.Int32 i)", model.GetSymbolInfo(invocation1).Symbol.ToTestDisplayString());
        // PROTOTYPE semantic model is undone
        Assert.Empty(model.GetMemberGroup(invocation1));

        var invocation2 = GetSyntax<InvocationExpressionSyntax>(tree, """new object().Method("hello")""");
        Assert.Equal("void N1.N2.E2.<>E__0.Method(System.String s)", model.GetSymbolInfo(invocation2).Symbol.ToTestDisplayString());
        // PROTOTYPE semantic model is undone
        Assert.Empty(model.GetMemberGroup(invocation2));
    }

    [Fact]
    public void InstanceMethodInvocation_NamespaceVsUsing_FromNamespace()
    {
        var src = """
using N2;

new object().Method(42);
new object().Method("hello");
new object().Method(default);

static class E1
{
    extension(object o)
    {
        public void Method(int i) { System.Console.Write("E1.Method "); }
    }
}

namespace N2
{
    static class E2
    {
        extension(object o)
        {
            public void Method(string s) { System.Console.Write("E2.Method "); }
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E1.Method E2.Method E1.Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var invocation1 = GetSyntax<InvocationExpressionSyntax>(tree, "new object().Method(42)");
        Assert.Equal("void E1.<>E__0.Method(System.Int32 i)", model.GetSymbolInfo(invocation1).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(invocation1)); // PROTOTYPE semantic model is undone

        var invocation2 = GetSyntax<InvocationExpressionSyntax>(tree, """new object().Method("hello")""");
        Assert.Equal("void N2.E2.<>E__0.Method(System.String s)", model.GetSymbolInfo(invocation2).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(invocation2)); // PROTOTYPE semantic model is undone

        var invocation3 = GetSyntax<InvocationExpressionSyntax>(tree, "new object().Method(default)");
        Assert.Equal("void E1.<>E__0.Method(System.Int32 i)", model.GetSymbolInfo(invocation3).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(invocation3)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_DerivedDerivedType()
    {
        var src = """
new Derived().M();

class Base { }
class Derived : Base { }

static class E
{
    extension(object o)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new Derived().M()");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(invocation)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_ImplementedInterface()
    {
        var src = """
new C().M();

interface I { }
class C : I { }

static class E
{
    extension(I i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new C().M()");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(invocation)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_IndirectlyImplementedInterface()
    {
        var src = """
new C().M();

interface I { }
interface Indirect : I { }
class C : Indirect { }

static class E
{
    extension(I i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var staticType = GetSyntax<InvocationExpressionSyntax>(tree, "new C().M()");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(staticType).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(staticType)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_TypeParameterImplementedInterface()
    {
        var src = """
class C
{
    void M<T>(T t) where T : I
    {
        t.M();
    }
}

interface I { }

static class E
{
    extension(I i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "t.M()");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void StaticMethodInvocation_MatchingExtendedType_TypeParameterImplementedInterface()
    {
        var src = """
class C
{
    void M<T>() where T : I
    {
        T.M();
    }
}

interface I { }

static class E
{
    extension(I)
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,9): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         T.M();
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(5, 9));
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_TypeParameterWithBaseClass()
    {
        var src = $$"""
class C<T> { }

class D
{
    void M<T>(T t) where T : C<T>
    {
        t.M2();
    }
}

static class E
{
    extension<T>(C<T> c)
    {
        public void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "t.M2()");
        Assert.Equal("void E.<>E__0<T>.M2()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_ConstrainedTypeParameter()
    {
        var src = $$"""
class D
{
    void M<T>(T t) where T : class
    {
        t.M2();
    }
}

static class E1
{
    extension<T>(T t) where T : struct
    {
        public void M2() { }
    }
}

static class E2
{
    extension<T>(T t) where T : class
    {
        public void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "t.M2()");
        Assert.Equal("void E2.<>E__0<T>.M2()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_BaseType()
    {
        var src = """
new object().M();

static class E
{
    extension(string s)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS1061: 'object' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // new object().M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("object", "M").WithLocation(1, 14));
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_GenericType()
    {
        var src = """
new C<int>().M();

class C<T> { }

static class E
{
    extension<T>(C<T> c)
    {
        public void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new C<int>().M()");
        Assert.Equal("void E.<>E__0<System.Int32>.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_GenericType_GenericMember()
    {
        var src = """
new C<int>().M<string>();

class C<T> { }

static class E
{
    extension<T>(C<T> c)
    {
        public void M<U>() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new C<int>().M<string>()");
        Assert.Equal("void E.<>E__0<System.Int32>.M<System.String>()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_GenericType_GenericMember_OmittedTypeArgument()
    {
        var src = """
new C<int>().M<,>();

class C<T> { }

static class E
{
    extension<T>(C<T> c)
    {
        public void M<U, V>() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS8389: Omitting the type argument is not allowed in the current context
            // new C<int>().M<,>();
            Diagnostic(ErrorCode.ERR_OmittedTypeArgument, "new C<int>().M<,>").WithLocation(1, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new C<int>().M<,>()");
        Assert.Equal("void E.<>E__0<System.Int32>.M<?, ?>()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_GenericType_GenericMember_BrokenConstraint()
    {
        var src = """
new C<int>().M<string>();

class C<T> { }

static class E
{
    extension<T>(C<T> c)
    {
        public void M<U>() where U : struct => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'E.extension<int>.M<U>()'
            // new C<int>().M<string>();
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "M<string>").WithArguments("E.extension<int>.M<U>()", "U", "string").WithLocation(1, 14));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new C<int>().M<string>()");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_BrokenConstraint()
    {
        var source = """
new object().Method();

static class E
{
    extension<T>(T t) where T : struct
    {
        public void Method() { }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS1061: 'object' does not contain a definition for 'Method' and no accessible extension method 'Method' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // new object().Method();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Method").WithArguments("object", "Method").WithLocation(1, 14));
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_BrokenConstraint_Nullability()
    {
        var source = """
#nullable enable
bool b = true;
var o = b ? null : new object();
o.Method();

static class E
{
    extension<T>(T t) where T : notnull
    {
        public void Method() { }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // o.Method();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(4, 1));
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "o.Method");
        // PROTOTYPE Nullability is undone
        Assert.Equal("void E.extension<System.Object>.Method()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void ReceiverParameter_AliasType()
    {
        var source = """
using Alias = C;

new Alias().M();

class C { }

static class E
{
    extension(Alias a)
    {
        public void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new Alias().M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_DynamicArgument()
    {
        // No extension members in dynamic invocation
        var src = """
dynamic d = null;
new object().M(d);

static class E
{
    extension(object o)
    {
        public void Method(object o) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,14): error CS1061: 'object' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // new object().M(d);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("object", "M").WithLocation(2, 14));
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_DynamicDifference_Nested()
    {
        var src = """
new C<dynamic>().M();

class C<T> { }

static class E
{
    extension(C<object> c)
    {
        public void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<dynamic>().M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_DynamicDifference_InBase()
    {
        var src = """
new D().M();

class C<T> { }
class D : C<dynamic> { }

static class E
{
    extension(C<object> c)
    {
        public void M()
        {
            System.Console.Write("M");
        }
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new D().M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_DynamicDifference_InInterface()
    {
        var src = """
new D().M();

interface I<T> { }
class D : I<dynamic> { }

static class E
{
    extension(I<object> i)
    {
        public void M() { System.Console.Write("M"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,11): error CS1966: 'D': cannot implement a dynamic interface 'I<dynamic>'
            // class D : I<dynamic> { }
            Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<dynamic>").WithArguments("D", "I<dynamic>").WithLocation(4, 11));
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_TupleNamesDifference()
    {
        var src = """
new C<(int a, int b)>().M();
new C<(int, int)>().M();
new C<(int other, int)>().M();

class C<T> { }

static class E
{
    extension(C<(int a, int b)> c)
    {
        public void M() { System.Console.Write("M"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();

        src = """
new C<(int a, int b)>().M();
new C<(int, int)>().M();
new C<(int other, int)>().M();

class C<T> { }

static class E
{
    public static void M(this C<(int a, int b)> c) { System.Console.Write("M"); }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_TupleNamesDifference_InBase()
    {
        var src = """
new D1().M();
new D2().M();
new D3().M();

class C<T> { }
class D1 : C<(int a, int b)> { }
class D2 : C<(int, int)> { }
class D3 : C<(int other, int)> { }

static class E
{
    extension(C<(int a, int b)> c)
    {
        public void M() { System.Console.Write("M"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [Fact]
    public void InstanceMethodInvocation_MatchingExtendedType_TupleNamesDifference_InInterface()
    {
        var src = """
new D1().M();
new D2().M();
new D3().M();

class I<T> { }
class D1 : I<(int a, int b)> { }
class D2 : I<(int, int)> { }
class D3 : I<(int other, int)> { }

static class E
{
    extension(I<(int a, int b)> i)
    {
        public void M() { System.Console.Write("M"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "MMM").VerifyDiagnostics();
    }

    [Fact]
    public void InstanceMethodInvocation_Nameof()
    {
        var src = """
object o = null;
System.Console.Write($"{nameof(o.M)} ");

static class E
{
    extension(object)
    {
        public void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "M StaticType").VerifyDiagnostics();
    }

    [Fact]
    public void InstanceMethodInvocation_Nameof_Overloads()
    {
        var src = """
object o = null;
System.Console.Write($"{nameof(o.M)} ");

static class E
{
    extension(object o)
    {
        public void M() { }
        public void M(int i) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();
    }

    [Fact]
    public void InstanceMethodInvocation_Nameof_SimpleName()
    {
        var src = """
class C
{
    void M()
    {
        _ = nameof(Method);
    }
}

static class E
{
    extension(object o)
    {
        public void Method() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,20): error CS0103: The name 'Method' does not exist in the current context
            //         _ = nameof(Method);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Method").WithArguments("Method").WithLocation(5, 20));
    }

    [Fact]
    public void InstanceMethodInvocation_Null_Method()
    {
        var src = """
#nullable enable

object? o = null;
o.Method();

static class E
{
    extension(object o)
    {
        public void Method() { System.Console.Write("Method "); }
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE nullability is undone
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // o.Method();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(4, 1));

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "Method");
    }

    [Fact]
    public void InstanceMethodInvocation_ColorColor_Method()
    {
        var src = """
class C
{
    static void M(C C)
    {
        C.Method();
    }
}

static class E
{
    extension(C c)
    {
        public void Method() { System.Console.Write("Method "); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "Method");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Method");
        Assert.Equal("void E.<>E__0.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_ColorColor_Static_Method()
    {
        var src = """
C.M(null);

class C
{
    public static void M(C C)
    {
        C.Method();
    }
}

static class E
{
    extension(C c)
    {
        public void Method() { System.Console.Write("Method"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var method = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Method");
        Assert.Equal("void E.<>E__0.Method()", model.GetSymbolInfo(method).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(method)); // PROTOTYPE semantic model is undone
    }

    [Fact(Skip = "PROTOTYPE: crash when binding foreach")]
    public void InstanceMethodInvocation_PatternBased_ForEach_NoMethod()
    {
        var src = """
foreach (var x in new C())
{
    System.Console.Write(x);
    break;
}

class C { }
class D { }

static class E
{
    extension(C c)
    {
        public D GetEnumerator() => new D();
    }

    extension(D d)
    {
        public bool MoveNext() => true;
        public int Current => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,19): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
            // foreach (var x in new C())
            Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(1, 19)
            );
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).MoveNextMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).CurrentProperty);
    }

    [Fact]
    public void InstanceMethodInvocation_NameOf_SingleParameter()
    {
        var src = """
class C
{
    public static void Main()
    {
        string x = "";
        System.Console.Write(nameof(x));
    }
}


static class E
{
    extension(C c)
    {
        public string nameof(string s) => throw null;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "x").VerifyDiagnostics();
    }

    [Fact]
    public void InstanceMethodInvocation_Simple_ExpressionTree()
    {
        // PROTOTYPE decide whether to allow expression tree scenarios. Verify shape of the tree if we decide to allow
        var source = """
using System.Linq.Expressions;
Expression<System.Action> x = () => new C().M(42);

class C
{
    public void M() => throw null;
}

static class E
{
    extension(C c)
    {
        public void M(int i) { System.Console.Write("E.M"); }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_NextScope()
    {
        // If overload resolution on extension type methods yields no applicable candidates,
        // we look in the next scope.
        var source = """
using N;

new C().M(42);

class C
{
    public void M() => throw null;
}

static class E1
{
    extension(C c)
    {
        public void M(string s) => throw null;
    }
}

namespace N
{
    static class E2
    {
        extension(C c)
        {
            public void M(int i) { System.Console.Write($"E2.M({i})"); }
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E2.M(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void N.E2.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_NewExtensionPriority()
    {
        // The method from the extension declaration comes before the extension method
        var source = """
new C().M(42);

class C
{
    public void M() => throw null;
}

static class E1
{
    extension(C c)
    {
        public void M(int i) { System.Console.Write($"E1.M({i})"); }
    }
}

static class E2
{
    public static void M(this C c, int i) => throw null;
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E1.M(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E1.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M()", "void C.M(System.Int32 i)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_NewExtensionPriority_02()
    {
        // The method from the extension declaration comes before the extension method
        var source = """
new C().M(42);

class C
{
    public void M() => throw null;
}

static class E
{
    extension(C c)
    {
        public void M(int i) { System.Console.Write($"E1.M({i})"); }
    }
    public static void M(this C c, int i) => throw null;
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E1.M(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M()", "void C.M(System.Int32 i)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_FallbackToExtensionMethod()
    {
        // The extension method is picked up if extension declaration candidates were not applicable
        var source = """
new C().M(42);

class C
{
    public static void M() => throw null;
}

static class E1
{
    extension(C c)
    {
        public void M(string s) => throw null;
        public void M(char c) => throw null;
    }
}

static class E2
{
    public static void M(this C c, int i) { System.Console.Write($"E2.M({i})"); }
}
""";
        var comp = CreateCompilation(source);

        CompileAndVerify(comp, expectedOutput: "E2.M(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void C.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M()", "void C.M(System.Int32 i)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_SimpleName()
    {
        // Extension invocation comes into play on an invocation on a member access but not an invocation on a simple name
        var source = """
class C
{
    public void M() => throw null;

    void M2()
    {
        M(42); // 1
    }
}

static class E
{
    extension(C c)
    {
        public void M(int i) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // 0.cs(7,9): error CS1501: No overload for method 'M' takes 1 arguments
            //         M(42); // 1
            Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "1").WithLocation(7, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "M(42)");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        Assert.Empty(model.GetMemberGroup(invocation));
    }

    [Fact]
    public void InstanceMethodInvocation_ArgumentName()
    {
        // Instance method with incompatible parameter name is skipped in favor of extension declaration method
        var source = """
new C().M(b: 42);

class C
{
    public void M(int a) => throw null;
}

static class E1
{
    extension(C c)
    {
        public void M(int b) { System.Console.Write($"E1.M({b})"); }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E1.M(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E1.<>E__0.M(System.Int32 b)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M(System.Int32 a)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_ArgumentName_02()
    {
        // Extension declaration method with incompatible parameter name is skipped in favor of extension method
        var source = """
new C().M(c: 42);

public class C
{
    public static void M(int a) => throw null;
}

static class E1
{
    extension(C c)
    {
        public void M(int b) => throw null;
    }
}

public static class E2
{
    public static void M(this C self, int c)
    {
        System.Console.Write($"E2.M({c})");
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E2.M(42)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void C.M(System.Int32 c)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        Assert.Equal(["void C.M(System.Int32 a)", "void C.M(System.Int32 c)"],
            model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_AmbiguityWithExtensionOnBaseType_PreferMoreSpecific()
    {
        var source = """
System.Console.Write(new C().M(42));

class Base { }

class C : Base { }

static class E1
{
    extension(Base b)
    {
        public int M(int i) => throw null;
    }
}

static class E2
{
    extension(C c)
    {
        public int M(int i) => i;
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,30): error CS0121: The call is ambiguous between the following methods or properties: 'E1.extension.M(int)' and 'E2.extension.M(int)'
            // System.Console.Write(new C().M(42));
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("E1.extension.M(int)", "E2.extension.M(int)").WithLocation(1, 30));

        source = """
System.Console.Write(new C().M(42));

public class Base { }

public class C : Base { }

public static class E1
{
    public static int M(this Base b, int i) => throw null;
}

public static class E2
{
    public static int M(this C c, int i) => i;
}
""";
        comp = CreateCompilation(source);
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void InstanceMethodInvocation_TypeArguments()
    {
        var source = """
new C().M<object>(42);

class C { }

static class E
{
    extension(C c)
    {
        public void M(int i) => throw null;
        public void M<T>(int i)
        {
            System.Console.Write("ran");
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object>");
        Assert.Equal("void E.<>E__0.M<System.Object>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_TypeArguments_WrongNumber()
    {
        var source = """
new C().M<object, object>(42);

class C { }

static class E
{
    extension(C c)
    {
        public void M(int i) => throw null;
        public void M<T>(int i) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // 0.cs(1,9): error CS1061: 'C' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // new C().M<object, object>(42);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M<object, object>").WithArguments("C", "M").WithLocation(1, 9)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object, object>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void InstanceMethodInvocation_TypeArguments_Omitted()
    {
        var source = """
new C().M<>(42);

class C { }

static class E
{
    extension(C c)
    {
        public void M<T>(int i) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS8389: Omitting the type argument is not allowed in the current context
            // new C().M<>(42);
            Diagnostic(ErrorCode.ERR_OmittedTypeArgument, "new C().M<>").WithLocation(1, 1)
            );

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<>");
        Assert.Equal("void E.<>E__0.M<?>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstanceMethodInvocation_TypeArguments_Inferred()
    {
        // No type arguments passed, but the extension declaration method is found and the type parameter inferred
        var source = """
new C().M(42);

class C { }

static class E
{
    extension(C c)
    {
        public void M<T>(T t)
        {
            System.Console.Write($"M({t})");
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "M(42)");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.<>E__0.M<System.Int32>(System.Int32 t)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void StaticMethodInvocation_InstanceExtensionMethod()
    {
        // The extension method is not static, but the receiver is a type
        var source = """
C.M();

class C { }

static class E
{
    extension(C c)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0120: An object reference is required for the non-static field, method, or property 'E.extension.M()'
            // C.M();
            Diagnostic(ErrorCode.ERR_ObjectRequired, "C.M").WithArguments("E.extension.M()").WithLocation(1, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));

        source = """
C.Method();

public class C { }

public static class E
{
    public static void Method(this C c) { }
}
""";
        comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // 0.cs(1,1): error CS0120: An object reference is required for the non-static field, method, or property 'E.Method(C)'
            // C.Method();
            Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Method").WithArguments("E.Method(C)").WithLocation(1, 1));
    }

    [Fact]
    public void InstanceMethodInvocation_StaticExtensionMethod()
    {
        // The extension method is static but the receiver is a value
        var source = """
new C().M();

class C { }

static class E
{
    extension(C c)
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0176: Member 'E.extension.M()' cannot be accessed with an instance reference; qualify it with a type name instead
            // new C().M();
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "new C().M").WithArguments("E.extension.M()").WithLocation(1, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void InstanceMethodInvocation_GenericType()
    {
        var src = """
new C<int>().StaticType<string>();

class C<T> { }

static class E
{
    extension<T>(C<T> c)
    {
        public static class StaticType<U> { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS1061: 'C<int>' does not contain a definition for 'StaticType' and no accessible extension method 'StaticType' accepting a first argument of type 'C<int>' could be found (are you missing a using directive or an assembly reference?)
            // new C<int>().StaticType<string>();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "StaticType<string>").WithArguments("C<int>", "StaticType").WithLocation(1, 14),
            // (9,29): error CS9501: Extension declarations can include only methods or properties
            //         public static class StaticType<U> { }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "StaticType").WithLocation(9, 29));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<int>().StaticType<string>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess).CandidateReason);
    }

    [ConditionalFact(typeof(NoUsedAssembliesValidation))] // PROTOTYPE metadata is undone
    public void InstanceMethodInvocation_RefOmittedComCall()
    {
        // For COM import type, omitting the ref is allowed
        string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""1234C65D-1234-447A-B786-64682CBEF136"")]
class C { }

static class E
{
    extension(C c)
    {
        public void M(ref short p) { }
        public void M(sbyte p) { }
        public void I(ref int p) { }
    }
}

class X
{
    public static void Goo()
    {
        short x = 123;
        C c = new C();
        c.M(x);
        c.I(123);
    }
}
";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionDeclarationMethods()
    {
        // See ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_Method
        var source = """
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color { }

static class E
{
    extension(Color c)
    {
        public void M1(S1 x, int y = 0) { System.Console.WriteLine("instance"); }

        public static void M1<T>(T x) where T : unmanaged { System.Console.WriteLine("static"); }
    }
}
""";
        // PROTOTYPE missing ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
        comp.VerifyEmitDiagnostics(
            //// (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
            ////         Color.M1(this);
            //Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
            );

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.M1");
        Assert.Equal("void E.<>E__0.M1(S1 x, [System.Int32 y = 0])", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionDeclarationMembersVsExtensionMethod()
    {
        var source = """
public struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

public class Color { }

public static class E1
{
    public static void M1(this Color c, S1 x, int y = 0) { System.Console.WriteLine("instance"); }
}

static class E2
{
    extension(Color c)
    {
        public static void M1<T>(T x) where T : unmanaged { System.Console.WriteLine("static"); }
    }
}
""";
        // PROTOTYPE missing ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
        comp.VerifyEmitDiagnostics(
            //// (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
            ////         Color.M1(this);
            //Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
            );

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.M1");
        Assert.Equal("void Color.M1(S1 x, [System.Int32 y = 0])", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_NotOnBase()
    {
        // Unlike `this`, `base` is not an expression in itself.
        // "Extension invocation" and "extension member lookup" do not apply to `base_access` syntax.
        var src = """
class Base { }

class Derived : Base
{
    void Main()
    {
        M(); // 1
        base.M(); // 2
    }
}

static class E
{
    extension(Base b)
    {
        public void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (7,9): error CS0103: The name 'M' does not exist in the current context
            //         M(); // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 9),
            // (8,14): error CS0117: 'Base' does not contain a definition for 'M'
            //         base.M(); // 2
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("Base", "M").WithLocation(8, 14));
    }

    [Fact]
    public void LookupKind_Invocation()
    {
        // Non-invocable extension member in inner scope is skipped in favor of invocable one from outer scope
        var src = """
using N;

new object().Member();

static class E
{
    extension(object o)
    {
        public int Member => 0;
    }
}

namespace N
{
    static class E2
    {
        extension(object o)
        {
            public void Member() { System.Console.Write("ran "); }
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran") .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().Member");
        Assert.Equal("void N.E2.<>E__0.Member()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_NotUnique()
    {
        var src = """
new C<object, dynamic>().M();
new C<dynamic, object>().M();

new C<object, dynamic>().M2();
new C<dynamic, object>().M2();

class C<T, U> { }

static class E
{
    extension<T>(C<T, T> c)
    {
        public string M() => "hi";
    }
        public static string M2<T>(this C<T, T> c) => "hi";
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,26): error CS1061: 'C<object, dynamic>' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'C<object, dynamic>' could be found (are you missing a using directive or an assembly reference?)
            // new C<object, dynamic>().M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("C<object, dynamic>", "M").WithLocation(1, 26),
            // (2,26): error CS1061: 'C<dynamic, object>' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'C<dynamic, object>' could be found (are you missing a using directive or an assembly reference?)
            // new C<dynamic, object>().M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("C<dynamic, object>", "M").WithLocation(2, 26),
            // (4,26): error CS1061: 'C<object, dynamic>' does not contain a definition for 'M2' and no accessible extension method 'M2' accepting a first argument of type 'C<object, dynamic>' could be found (are you missing a using directive or an assembly reference?)
            // new C<object, dynamic>().M2();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M2").WithArguments("C<object, dynamic>", "M2").WithLocation(4, 26),
            // (5,26): error CS1061: 'C<dynamic, object>' does not contain a definition for 'M2' and no accessible extension method 'M2' accepting a first argument of type 'C<dynamic, object>' could be found (are you missing a using directive or an assembly reference?)
            // new C<dynamic, object>().M2();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M2").WithArguments("C<dynamic, object>", "M2").WithLocation(5, 26));
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_NestedTuples()
    {
        var src = """
var s = new C<(string, string)>.Nested<(int, int)>().M();
System.Console.Write(s);

class C<T>
{
    internal class Nested<U> { }
}

static class E
{
    extension<T1, T2>(C<(T1, T1)>.Nested<(T2, T2)> cn)
    {
        public string M() => "hi";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<(string, string)>.Nested<(int, int)>().M");
        Assert.Equal("System.String E.<>E__0<System.String, System.Int32>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_PointerArray()
    {
        var src = """
unsafe
{
    string s = new C<long*[]>.Nested<int*[]>().M();
    System.Console.Write(s);
}

unsafe class C<T>
{
    internal class Nested<U> { }
}

unsafe static class E
{
    extension<T1, T2>(C<T1*[]>.Nested<T2*[]> cn)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        public string M() => "hi";
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<long*[]>.Nested<int*[]>().M");
        Assert.Equal("System.String E.<>E__0<System.Int64, System.Int32>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_Pointer()
    {
        // A type parameter cannot unify with a pointer type
        var src = """
unsafe
{
    string s = new C<long*[]>.Nested<int*[]>().M();
    System.Console.Write(s);
}

unsafe class C<T>
{
    internal class Nested<U> { }
}

static class E
{
    extension<T1, T2>(C<T1[]>.Nested<T2[]> cn)
    {
        public string M() => "hi";
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugExe);
        comp.VerifyEmitDiagnostics(
            // (3,48): error CS1061: 'C<long*[]>.Nested<int*[]>' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'C<long*[]>.Nested<int*[]>' could be found (are you missing a using directive or an assembly reference?)
            //     string s = new C<long*[]>.Nested<int*[]>().M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("C<long*[]>.Nested<int*[]>", "M").WithLocation(3, 48));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<long*[]>.Nested<int*[]>().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_FunctionPointer()
    {
        var src = """
unsafe
{
    string s = new C<delegate*<int>[]>.Nested<delegate*<long>[]>().M();
    System.Console.Write(s);
}

unsafe class C<T>
{
    internal class Nested<U> { }
}

unsafe static class E
{
    extension<T1, T2>(C<delegate*<T1>[]>.Nested<delegate*<T2>[]> cn)
    {
        public string M() => "hi";
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugExe);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<delegate*<int>[]>.Nested<delegate*<long>[]>().M");
        Assert.Equal("System.String E.<>E__0<System.Int32, System.Int64>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_ForInterface()
    {
        var src = """
string s = new C<int>().M();
System.Console.Write(s);

class C<T> : I<T> { }
interface I<T> { }

static class E
{
    extension<T>(I<T> i)
    {
        public string M() => "hi";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "hi") .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<int>().M");
        Assert.Equal("System.String E.<>E__0<System.Int32>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_ForBaseInterface()
    {
        var src = """
string s = new C<int>().M();
System.Console.Write(s);

class C<T> : I<T> { }
interface I<T> : I2<T> { }
interface I2<T> { }

static class E
{
    extension<T>(I2<T> i)
    {
        public string M() => "hi";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "hi").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<int>().M");
        Assert.Equal("System.String E.<>E__0<System.Int32>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Generic_ForBase()
    {
        var src = """
string s = new C<int, string>().M();
System.Console.Write(s);

class Base<T, U> { }
class C<T, U> : Base<U, T> { }

static class E
{
    extension<T, U>(Base<T, U> b)
    {
        public string M() => "hi";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "hi") .VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C<int, string>().M");
        Assert.Equal("System.String E.<>E__0<System.String, System.Int32>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethodInvocation_Obsolete()
    {
        var src = """
new object().Method();

static class E
{
    extension(object o)
    {
        [System.Obsolete("Method is obsolete", true)]
        public void Method() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0619: 'E.extension.Method()' is obsolete: 'Method is obsolete'
            // new object().Method();
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new object().Method()").WithArguments("E.extension.Method()", "Method is obsolete").WithLocation(1, 1));
    }

    [Fact]
    public void InstanceMethodInvocation_BrokenConstraintMethodOuterExtension()
    {
        var src = """
static class E2
{
    extension(object o)
    {
        public void M<T>() => throw null;
    }
}

namespace Inner
{
    class C
    {
        public static void Main()
        {
            new C().M<object>();
        }
    }

    static class E1
    {
        extension(C c)
        {
            public string M<T>() where T : struct => throw null;
        }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M<object>");
        Assert.Equal("void E2.<>E__0.M<System.Object>()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Theory, CombinatorialData]
    public void InstanceMethodInvocation_MultipleExtensions(bool e1BeforeE2)
    {
        var e1 = """
static class E1
{
    extension(object o)
    {
        public string M() => throw null;
    }
}
""";

        var e2 = """
static class E2
{
    extension(object o)
    {
        public string M() => throw null;
    }
}
""";

        var src = $$"""
new object().M();
{{(e1BeforeE2 ? e1 : e2)}}
{{(e1BeforeE2 ? e2 : e1)}}
""";
        var comp = CreateCompilation(src);
        if (!e1BeforeE2)
        {
            comp.VerifyEmitDiagnostics(
                // (1,14): error CS0121: The call is ambiguous between the following methods or properties: 'E2.extension.M()' and 'E1.extension.M()'
                // new object().M();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("E2.extension.M()", "E1.extension.M()").WithLocation(1, 14));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (1,14): error CS0121: The call is ambiguous between the following methods or properties: 'E1.extension.M()' and 'E2.extension.M()'
                // new object().M();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("E1.extension.M()", "E2.extension.M()").WithLocation(1, 14));
        }

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    public class ThreePermutationGenerator : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = [
            [0, 1, 2],
            [0, 2, 1],
            [1, 0, 2],
            [1, 2, 0],
            [2, 0, 1],
            [2, 1, 0]];

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory, ClassData(typeof(ThreePermutationGenerator))]
    public void InstanceMethodInvocation_InterfaceAppearsTwice(int first, int second, int third)
    {
        string[] segments = [
            """
            static class E1
            {
                extension<T>(I1<T> i)
                {
                    public string M() => null;
                }
            }
            """,
            """
            static class E2
            {
                extension(I2 i) { }
            }
            """,
            """
            static class E3
            {
                extension(C c) { }
            }
            """];

        var src = $$"""
System.Console.Write(new C().M());

interface I1<T> { }
interface I2 : I1<string> { }

class C : I1<int>, I2 { }

{{segments[first]}}

{{segments[second]}}

{{segments[third]}}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,30): error CS1061: 'C' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // System.Console.Write(new C().M());
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("C", "M").WithLocation(1, 30));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void InstanceMethodInvocation_MultipleStageInference()
    {
        var src = """
public class C
{
    public void M(I<string> i, out object o)
    {
        i.M(out o); // infers E.M<object>
        i.M2(out o); // 1
    }
}

public static class E
{
   public static void M<T>(this I<T> i, out T t) { t = default; }
}

static class E2
{
    extension<T>(I<T> i)
    {
       public void M2(out T t) { t = default; }
    }
}

public interface I<out T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,18): error CS1503: Argument 1: cannot convert from 'out object' to 'out string'
            //         i.M2(out o); // 1
            Diagnostic(ErrorCode.ERR_BadArgType, "o").WithArguments("1", "out object", "out string").WithLocation(6, 18));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "i.M");
        Assert.Equal("void I<System.Object>.M<System.Object>(out System.Object t)", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetSymbolInfo(memberAccess1).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(["void I<System.String>.M<System.String>(out System.String t)"], model.GetMemberGroup(memberAccess1).ToTestDisplayStrings());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "i.M2");
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Equal(["void E2.<>E__0<System.String>.M2(out System.String t)"], model.GetSymbolInfo(memberAccess2).CandidateSymbols.ToTestDisplayStrings());
        Assert.Empty(model.GetMemberGroup(memberAccess2)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_01()
    {
        var src = """
using System.Collections.Generic;

IEnumerable<string> i = null;
i.M();

static class E
{
    extension(IEnumerable<object> o)
    {
        public void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "i.M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone

        src = """
using System.Collections.Generic;

IEnumerable<object> i = null;
i.M();

static class E
{
    extension(IEnumerable<string> o)
    {
        public void M() { }
    }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,3): error CS1061: 'IEnumerable<object>' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'IEnumerable<object>' could be found (are you missing a using directive or an assembly reference?)
            // i.M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("System.Collections.Generic.IEnumerable<object>", "M").WithLocation(4, 3));
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_02()
    {
        var src = """
string.M();

static class E
{
    extension(object)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "string.M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_03()
    {
        var src = """
int.M();

static class E
{
    extension(object)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "int.M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_04()
    {
        var src = """
int.M();
42.M2();

static class E
{
    extension(int?)
    {
        public static void M() { }
    }
    public static void M2(this int? i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5),
            // (2,1): error CS1929: 'int' does not contain a definition for 'M2' and the best extension method overload 'E.M2(int?)' requires a receiver of type 'int?'
            // 42.M2();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "42").WithArguments("int", "M2", "E.M2(int?)", "int?").WithLocation(2, 1));
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_05()
    {
        var src = """
MyEnum.Zero.M();

enum MyEnum { Zero }

static class E
{
    extension(System.Enum e)
    {
        public void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_06()
    {
        var src = """
dynamic d = new C();
d.M();
d.M2();

static class E
{
    extension(object o)
    {
        public void M() => throw null;
    }

    public static void M2(this object o) => throw null;
}

class C
{
    public void M() { System.Console.Write("ran "); }
    public void M2() { System.Console.Write("ran2"); }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("ran ran2"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_07()
    {
        var src = """
object o = null;
o.M();
o.M2();

static class E
{
    extension(dynamic d)
    {
        public void M() { }
    }

    public static void M2(this dynamic d) { }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE validate extension parameter
        comp.VerifyEmitDiagnostics(
            // (12,32): error CS1103: The first parameter of an extension method cannot be of type 'dynamic'
            //     public static void M2(this dynamic d) { }
            Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic").WithLocation(12, 32));
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_08()
    {
        var src = """
(int a, int b) t = default;
t.M();
t.M2();

static class E
{
    extension((int c, int d) t)
    {
        public void M() { }
    }

    public static void M2(this (int c, int d) t) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_09()
    {
        var src = """
int[] i = default;
i.M();
i.M2();

static class E
{
    extension(System.ReadOnlySpan<int> ros)
    {
        public void M() { }
    }

    public static void M2(this System.ReadOnlySpan<int> ros) { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_10()
    {
        var missingSrc = """
public class Missing { }
""";
        var missingRef = CreateCompilation(missingSrc, assemblyName: "missing").EmitToImageReference();

        var derivedSrc = """
public class Derived : Missing { }
""";
        var derivedRef = CreateCompilation(derivedSrc, references: [missingRef]).EmitToImageReference();

        var src = """
new Derived().M();
new Derived().M2();

class Other { }

static class E
{
    extension(Other o)
    {
        public void M() { }
    }

    public static void M2(this Other o) { }
}
""";
        var comp = CreateCompilation(src, references: [derivedRef]);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new Derived().M();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new Derived().M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1),
            // (1,15): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new Derived().M();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 15),
            // (1,15): error CS1061: 'Derived' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'Derived' could be found (are you missing a using directive or an assembly reference?)
            // new Derived().M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("Derived", "M").WithLocation(1, 15),
            // (2,1): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new Derived().M2();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "new Derived().M2").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 1),
            // (2,1): error CS1929: 'Derived' does not contain a definition for 'M2' and the best extension method overload 'E.M2(Other)' requires a receiver of type 'Other'
            // new Derived().M2();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new Derived()").WithArguments("Derived", "M2", "E.M2(Other)", "Other").WithLocation(2, 1),
            // (2,15): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new Derived().M2();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "M2").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 15));
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_11()
    {
        var missingSrc = """
public class Missing { }
""";
        var missingRef = CreateCompilation(missingSrc, assemblyName: "missing").EmitToImageReference();

        var derivedSrc = """
public class Derived : Missing { }
""";
        var derivedRef = CreateCompilation(derivedSrc, references: [missingRef]).EmitToImageReference();

        var src = """
new Derived().M();
new Derived().M2();

static class E
{
    extension(Derived d)
    {
        public void M() { }
    }

    public static void M2(this Derived d) { }
}
""";
        var comp = CreateCompilation(src, references: [derivedRef]);
        comp.VerifyEmitDiagnostics(
            // (1,15): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new Derived().M();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 15),
            // (2,15): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new Derived().M2();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "M2").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 15));
    }

    [Fact]
    public void GetCompatibleExtension_Conversion_12()
    {
        var missingSrc = """
public class Missing { }
""";
        var missingRef = CreateCompilation(missingSrc, assemblyName: "missing").EmitToImageReference();

        var derivedSrc = """
public class I<T> { }
public class Derived : I<Missing> { }
""";
        var derivedRef = CreateCompilation(derivedSrc, references: [missingRef]).EmitToImageReference();

        var src = """
new Derived().M();
new Derived().M2();

static class E
{
    extension(I<object> i)
    {
        public void M() { }
    }

    public static void M2(this I<object> i) { }
}
""";
        var comp = CreateCompilation(src, references: [derivedRef]);
        comp.VerifyEmitDiagnostics(
            // (1,15): error CS1061: 'Derived' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'Derived' could be found (are you missing a using directive or an assembly reference?)
            // new Derived().M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("Derived", "M").WithLocation(1, 15),
            // (2,1): error CS1929: 'Derived' does not contain a definition for 'M2' and the best extension method overload 'E.M2(I<object>)' requires a receiver of type 'I<object>'
            // new Derived().M2();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new Derived()").WithArguments("Derived", "M2", "E.M2(I<object>)", "I<object>").WithLocation(2, 1));
    }

    [Fact]
    public void GetCompatibleExtension_TypeInference_01()
    {
        var src = """
I<object, string>.M();

interface I<out T1, out T2> { }

static class E
{
    extension<T>(I<T, T>)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "I<object, string>.M");
        Assert.Equal("void E.<>E__0<System.Object>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void GetCompatibleExtension_TypeInference_02()
    {
        var src = """
I<object, string>.M();

interface I<in T1, in T2> { }

static class E
{
    extension<T>(I<T, T>)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "I<object, string>.M");
        Assert.Equal("void E.<>E__0<System.String>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void GetCompatibleExtension_TypeInference_03()
    {
        var src = """
I<object, string>.M();

interface I<T1, T2> { }

static class E
{
    extension<T>(I<T, T>)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,19): error CS0117: 'I<object, string>' does not contain a definition for 'M'
            // I<object, string>.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("I<object, string>", "M").WithLocation(1, 19));
    }

    [Fact]
    public void GetCompatibleExtension_Constraint_UseSiteInfo_01()
    {
        var missingSrc = """
public struct Missing { public int i; }
""";
        var missingRef = CreateCompilation(missingSrc, assemblyName: "missing").EmitToImageReference();

        var containerSrc = """
public struct Container { public Missing field; }
""";
        var containerRef = CreateCompilation(containerSrc, references: [missingRef]).EmitToImageReference();

        var src = """
Container.M();

static class E
{
    extension<T>(T t) where T : unmanaged
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, references: [containerRef]);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // Container.M();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "Container.M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1),
            // (1,11): error CS0117: 'Container' does not contain a definition for 'M'
            // Container.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("Container", "M").WithLocation(1, 11));

        src = """
new Container().M();

static class E
{
    public static void M<T>(this T t) where T : unmanaged { }
}
""";
        comp = CreateCompilation(src, references: [containerRef]);
        comp.VerifyEmitDiagnostics(
            // (1,17): error CS8377: The type 'Container' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'E.M<T>(T)'
            // new Container().M();
            Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M").WithArguments("E.M<T>(T)", "T", "Container").WithLocation(1, 17),
            // (1,17): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new Container().M();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 17));

        src = """
new object().M(new Container());

static class E
{
    public static void M<T>(this object o, T t) where T : unmanaged { }
}
""";
        comp = CreateCompilation(src, references: [containerRef]);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS8377: The type 'Container' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'E.M<T>(object, T)'
            // new object().M(new Container());
            Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "M").WithArguments("E.M<T>(object, T)", "T", "Container").WithLocation(1, 14),
            // (1,14): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // new object().M(new Container());
            Diagnostic(ErrorCode.ERR_NoTypeDef, "M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 14));
    }

    [Fact]
    public void GetCompatibleExtension_Constraint_UseSiteInfo_02()
    {
        var missingSrc = """
public struct Missing { public int i; }
""";
        var missingRef = CreateCompilation(missingSrc, assemblyName: "missing").EmitToImageReference();

        var containerSrc = """
public struct Container { public Missing field; }
""";
        var containerRef = CreateCompilation(containerSrc, references: [missingRef]).EmitToImageReference();

        var src = """
using N;

Container.M();

static class E
{
    extension<T>(T t) where T : unmanaged
    {
        public static void M(int inapplicable) => throw null;
    }
}

namespace N
{
    static class E2
    {
        extension<T>(T t)
        {
            public static void M() { }
        }
    }
}
""";
        var comp = CreateCompilation(src, references: [containerRef]);
        comp.VerifyEmitDiagnostics(
            // (3,1): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // Container.M();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "Container.M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 1));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Container.M");
        Assert.Equal("void N.E2.<>E__0<Container>.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());

        src = """
Container.M();

static class E
{
    extension<T>(T t) where T : unmanaged
    {
        public static void M(int inapplicable) => throw null;
    }
}
""";
        comp = CreateCompilation(src, references: [containerRef]);
        comp.VerifyEmitDiagnostics(
                // (1,1): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // Container.M();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Container.M").WithArguments("Missing", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1),
                // (1,11): error CS0117: 'Container' does not contain a definition for 'M'
                // Container.M();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("Container", "M").WithLocation(1, 11));
    }

    [Fact]
    public void InstancePropertyAccess_Simple()
    {
        var src = """
System.Console.Write(new object().P);

public static class Extensions
{
    extension(object o)
    {
        public int P => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().P");
        Assert.Equal("System.Int32 Extensions.<>E__0.P { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void InstancePropertyAccess_StaticExtensionProperty()
    {
        var src = """
System.Console.Write(new object().P);

public static class Extensions
{
    extension(object o)
    {
        public static int P => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,22): error CS0176: Member 'Extensions.extension.P' cannot be accessed with an instance reference; qualify it with a type name instead
            // System.Console.Write(new object().P);
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "new object().P").WithArguments("Extensions.extension.P").WithLocation(1, 22));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().P");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["System.Int32 Extensions.<>E__0.P { get; }"], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void InstancePropertyAccess_Invoked()
    {
        var src = """
new object().P();

public static class Extensions
{
    extension(object o)
    {
        public int P => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS1061: 'object' does not contain a definition for 'P' and no accessible extension method 'P' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // new object().P();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("object", "P").WithLocation(1, 14));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "new object().P()");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        // PROTOTYPE semantic model is undone
        Assert.Equal([], model.GetSymbolInfo(invocation).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void InstancePropertyAccess_Invoked_Invocable()
    {
        var src = """
new object().P();

public static class Extensions
{
    extension(object o)
    {
        public System.Action P { get { return () => { }; } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().P");
        Assert.Equal("System.Action Extensions.<>E__0.P { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        // PROTOTYPE semantic model is undone
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void StaticMethodInvocation_Simple()
    {
        var src = """
object.M();

public static class Extensions
{
    extension(object)
    {
        public static int M() => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "object.M()");
        Assert.Equal("System.Int32 Extensions.<>E__0.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        // PROTOTYPE semantic model is undone
        Assert.Equal([], model.GetSymbolInfo(invocation).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void StaticMethodInvocation_TypeArguments()
    {
        var source = """
C.M<object>(42);

class C { }

static class E
{
    extension(C)
    {
        public static void M(int i) => throw null;
        public static void M<T>(int i) { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M<object>");
        Assert.Equal("void E.<>E__0.M<System.Object>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Theory, CombinatorialData]
    public void StaticMethodInvocation_Ambiguity_Method(bool e1BeforeE2)
    {
        var e1 = """
static class E1
{
    extension(object)
    {
        public static void Method() => throw null;
    }
}
""";

        var e2 = """
static class E2
{
    extension(object)
    {
        public static void Method() => throw null;
    }
}
""";

        var src = $$"""
object.Method();

{{(e1BeforeE2 ? e1 : e2)}}
{{(e1BeforeE2 ? e2 : e1)}}
""";
        var comp = CreateCompilation(src);
        if (!e1BeforeE2)
        {
            comp.VerifyEmitDiagnostics(
                // (1,8): error CS0121: The call is ambiguous between the following methods or properties: 'E2.extension.Method()' and 'E1.extension.Method()'
                // object.Method();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("E2.extension.Method()", "E1.extension.Method()").WithLocation(1, 8));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (1,8): error CS0121: The call is ambiguous between the following methods or properties: 'E1.extension.Method()' and 'E2.extension.Method()'
                // object.Method();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("E1.extension.Method()", "E2.extension.Method()").WithLocation(1, 8));
        }
    }

    [Fact]
    public void StaticPropertyAccess_InstanceExtensionProperty()
    {
        var src = """
System.Console.Write(new object().P);

public static class Extensions
{
    extension(object o)
    {
        public static int P => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,22): error CS0176: Member 'Extensions.extension.P' cannot be accessed with an instance reference; qualify it with a type name instead
            // System.Console.Write(new object().P);
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "new object().P").WithArguments("Extensions.extension.P").WithLocation(1, 22));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().P");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["System.Int32 Extensions.<>E__0.P { get; }"], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_ConditionalOperator_Static_ExtensionMethod()
    {
        var source = """
bool b = true;
var x = b ? object.M : object.M;

static class E
{
    extension(object o)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (2,9): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'method group' and 'method group'
            // var x = b ? object.M : object.M;
            Diagnostic(ErrorCode.ERR_InvalidQM, "b ? object.M : object.M").WithArguments("method group", "method group").WithLocation(2, 9));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.M").ToArray();
        Assert.Null(model.GetSymbolInfo(memberAccess[0]).Symbol);
        Assert.Null(model.GetSymbolInfo(memberAccess[1]).Symbol);

        // PROTOTYPE semantic model is undone
        Assert.Empty(model.GetMemberGroup(memberAccess[0]));
        Assert.Empty(model.GetMemberGroup(memberAccess[1]));
    }

    [Fact]
    public void ResolveAll_ConditionalOperator_Static_ExtensionProperty()
    {
        var source = """
bool b = true;
var x = b ? object.StaticProperty : object.StaticProperty;
System.Console.Write(x);

static class E
{
    extension(object o)
    {
        public static int StaticProperty => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.StaticProperty").ToArray();
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ConditionalOperator_Static_DifferentTypes()
    {
        var source = """
bool b = true;
var x = b ? object.StaticProperty : object.StaticProperty2;
System.Console.Write(x.ToString());

static class E
{
    extension(object o)
    {
        public static int StaticProperty => 42;
        public static long StaticProperty2 => 43;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticProperty");
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess).Type.ToTestDisplayString());
        Assert.Equal("System.Int64", model.GetTypeInfo(memberAccess).ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ConditionalOperator_Static_WithTargetType()
    {
        var source = """
bool b = true;
long x = b ? object.StaticProperty : object.StaticProperty;
System.Console.Write(x.ToString());

static class E
{
    extension(object o)
    {
        public static int StaticProperty => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.StaticProperty").ToArray();
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());

        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess[0]).Type.ToTestDisplayString());
        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess[0]).ConvertedType.ToTestDisplayString());

        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess[1]).Type.ToTestDisplayString());
        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess[1]).ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ConditionalOperator_Static_TwoExtensions_WithTargetType()
    {
        var source = """
bool b = true;
string x = b ? D.f : D.f;
System.Console.Write(x);

class D { }

static class E1
{
    extension(D)
    {
        public static string f => "ran";
    }
}

static class E2
{
    extension(object o)
    {
        public static void f() { }
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (2,18): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // string x = b ? D.f : D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(2, 18),
            // (2,24): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // string x = b ? D.f : D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(2, 24));
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        //var tree = comp.SyntaxTrees.First();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "D.f").ToArray();
        //Assert.Equal("System.String E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        //Assert.Equal("System.String E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());

        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[0]).Type.ToTestDisplayString());
        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[0]).ConvertedType.ToTestDisplayString());

        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[1]).Type.ToTestDisplayString());
        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[1]).ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ConditionalOperator_Static_TwoExtensions_WithTargetDelegateType()
    {
        var source = """
bool b = true;
System.Action x = b ? D.f : D.f;
System.Console.Write(x);

class D { }

static class E
{
    extension(D)
    {
        public static string f => null;
    }
}

static class E2
{
    extension(object o)
    {
        public static void f() { }
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (2,25): error CS0229: Ambiguity between 'E2.extension.f()' and 'E.extension.f'
            // System.Action x = b ? D.f : D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E.extension.f").WithLocation(2, 25),
            // (2,31): error CS0229: Ambiguity between 'E2.extension.f()' and 'E.extension.f'
            // System.Action x = b ? D.f : D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E.extension.f").WithLocation(2, 31)
            //// (2,19): error CS0029: Cannot implicitly convert type 'string' to 'System.Action'
            //// System.Action x = b ? D.f : D.f;
            //Diagnostic(ErrorCode.ERR_NoImplicitConv, "b ? D.f : D.f").WithArguments("string", "System.Action").WithLocation(2, 19)
            );

        //var tree = comp.SyntaxTrees.First();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "D.f").ToArray();
        //Assert.Equal("System.String E.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        //Assert.Equal("System.String E.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());

        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[0]).Type.ToTestDisplayString());
        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[0]).ConvertedType.ToTestDisplayString());

        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[1]).Type.ToTestDisplayString());
        //Assert.Equal("System.String", model.GetTypeInfo(memberAccess[1]).ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Cast_Static_Operand()
    {
        var source = """
var x = (long)object.StaticProperty;
System.Console.Write(x.ToString());

static class E
{
    extension(object o)
    {
        public static int StaticProperty => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticProperty");
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess).Type.ToTestDisplayString());
        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess).ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Cast_Static_Operand_TwoExtensions()
    {
        var source = """
var x = (string)D.f;
System.Console.Write(x);

class D { }

static class E1
{
    extension(D)
    {
        public static string f => "ran";
    }
}

static class E2
{
    extension(object)
    {
        public static void f() { }
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,19): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // var x = (string)D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(1, 19));
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        //var tree = comp.SyntaxTrees.First();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "D.f");
        //Assert.Equal("System.String E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Cast_Static_Operand_TwoExtensions_DelegateType()
    {
        var source = """
var x = (System.Action)D.f;
System.Action a = D.f;

class D { }

static class E1
{
    extension(D)
    {
        public static string f => null;
    }
}

static class E2
{
    extension(object)
    {
        public static void f() => throw null;
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,26): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // var x = (System.Action)D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(1, 26),
            // (2,21): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // System.Action a = D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(2, 21));

        //var tree = comp.SyntaxTrees.First();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "D.f").ToArray();
        //Assert.Equal("System.String E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        //Assert.Equal("System.String E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());

        // Note: a conversion to a delegate type does not provide invocation context for resolving the member access
        source = """
var x = (System.Action)D.f;
System.Action a = D.f;

class C
{
    public static void f() { }
}

class D : C
{
    public static new string f => null!;
}
""";
        comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,9): error CS0030: Cannot convert type 'string' to 'System.Action'
            // var x = (System.Action)D.f;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Action)D.f").WithArguments("string", "System.Action").WithLocation(1, 9),
            // (2,19): error CS0029: Cannot implicitly convert type 'string' to 'System.Action'
            // System.Action a = D.f;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "D.f").WithArguments("string", "System.Action").WithLocation(2, 19));
    }

    [Fact]
    public void ResolveAll_MethodTypeInference()
    {
        var source = """
write(object.M);
void write<T>(T t) { System.Console.Write(t.ToString()); }

static class E
{
    extension(object)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess).Type.ToTestDisplayString());
        Assert.Equal("System.Int32", model.GetTypeInfo(memberAccess).ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ArrayCreation_Initializer_Static()
    {
        var source = """
var x = new[] { object.StaticProperty, object.StaticProperty };
System.Console.Write((x[0], x[1]));

static class E
{
    extension(object)
    {
        public static int StaticProperty => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 42)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.StaticProperty").ToArray();
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ArrayCreation_Rank()
    {
        var source = """
var x = new object[object.StaticProperty];
System.Console.Write(x.Length.ToString());

static class E
{
    extension(object o)
    {
        public static int StaticProperty => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.StaticProperty");
        Assert.Equal("System.Int32 E.<>E__0.StaticProperty { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Deconstruction_Declaration()
    {
        var source = """
var (x, y) = object.M;
System.Console.Write((x, y));

static class E
{
    extension(object)
    {
        public static (int, int) M => (42, 43);
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 43)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("(System.Int32, System.Int32) E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Deconstruction_Assignment()
    {
        var source = """
int x, y;
(x, y) = object.M;
System.Console.Write((x, y));

static class E
{
    extension(object o)
    {
        public static (int, int) M => (42, 43);
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 43)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("(System.Int32, System.Int32) E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_TupleExpression()
    {
        var source = """
System.Console.Write((object.M, object.M));

static class E
{
    extension(object o)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 42)").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.M").ToArray();
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_CollectionExpression()
    {
        var source = """
int[] x = [object.M];
System.Console.Write(x[0].ToString());

static class E
{
    extension(object o)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_CollectionExpression_ExtensionAddMethod()
    {
        var source = """
using System.Collections;
using System.Collections.Generic;

MyCollection c = [42];

static class E
{
    extension(MyCollection c)
    {
        public void Add(int i) { System.Console.Write("ran"); }
    }
}

public class MyCollection : IEnumerable<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void ResolveAll_CollectionExpression_ExtensionAddDelegateTypeProperty()
    {
        var source = """
using System.Collections;
using System.Collections.Generic;

MyCollection c = [42];

static class E
{
    extension(MyCollection c)
    {
        public System.Action<int> Add => (int i) => { };
    }
}

public class MyCollection : IEnumerable<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (4,18): error CS0118: 'Add' is a property but is used like a method
            // MyCollection c = [42];
            Diagnostic(ErrorCode.ERR_BadSKknown, "[42]").WithArguments("Add", "property", "method").WithLocation(4, 18));
    }

    [Fact]
    public void ResolveAll_Initializer_Property()
    {
        var source = """
var x = new System.Collections.Generic.List<int>() { object.M };
System.Console.Write(x[0]);

static class E
{
    extension(object o)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Initializer_Method()
    {
        var source = """
var x = new System.Collections.Generic.List<int>() { object.M };

static class E
{
    extension(object o)
    {
        public static void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,54): error CS1950: The best overloaded Add method 'List<int>.Add(int)' for the collection initializer has some invalid arguments
            // var x = new System.Collections.Generic.List<int>() { object.M };
            Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "object.M").WithArguments("System.Collections.Generic.List<int>.Add(int)").WithLocation(1, 54),
            // (1,54): error CS1503: Argument 1: cannot convert from 'method group' to 'int'
            // var x = new System.Collections.Generic.List<int>() { object.M };
            Diagnostic(ErrorCode.ERR_BadArgType, "object.M").WithArguments("1", "method group", "int").WithLocation(1, 54));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess).CandidateReason);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void ResolveAll_Initializer_ObjectInitializer()
    {
        var source = """
var x = new C() { f = object.M };
System.Console.Write(x.f.ToString());

class C
{
    public int f;
}

static class E
{
    extension(object o)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ConditionalAccess_Receiver()
    {
        var source = """
System.Console.Write(object.M?.ToString());

static class E
{
    extension(object o)
    {
        public static string M => "ran";
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ConditionalAccess_WhenNotNull_Property()
    {
        var source = """
var x = new object()?.M;
System.Console.Write(x.ToString());

static class E
{
    extension(object o)
    {
        public string M => "ran";
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        // CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberBinding = GetSyntax<MemberBindingExpressionSyntax>(tree, ".M");
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberBinding).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ConditionalAccess_WhenNotNull_Invocation()
    {
        var source = """
var x = new object()?.M();
System.Console.Write(x.ToString());

static class E
{
    extension(object o)
    {
        public string M() => "ran";
        public string M(int i) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        // CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberBinding = GetSyntax<MemberBindingExpressionSyntax>(tree, ".M");
        Assert.Equal("System.String E.<>E__0.M()", model.GetSymbolInfo(memberBinding).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_CompoundAssignment_Left()
    {
        var source = """
object.M += 41;
System.Console.Write(E.M.ToString());

static class E
{
    extension(object o)
    {
        public static int M { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; set; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_CompoundAssignment_Right()
    {
        var source = """
int x = 1;
x += object.M;
System.Console.Write(x.ToString());

static class E
{
    extension(object o)
    {
        public static int M => 41;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_BinaryOperator_UserDefinedOperator()
    {
        var source = """
var x = object.M + object.M;
System.Console.Write(x.ToString());

public class C
{
    public static int operator+(C c1, C c2) => 42;
}

static class E
{
    extension(object o)
    {
        public static C M => new C();
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.M").ToArray();
        Assert.Equal("C E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        Assert.Equal("C E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());

        var binaryOp = GetSyntax<BinaryExpressionSyntax>(tree, "object.M + object.M");
        Assert.Equal("System.Int32 C.op_Addition(C c1, C c2)", model.GetSymbolInfo(binaryOp).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_BinaryOperator_NoUserDefinedOperator()
    {
        var source = """
var x = object.M + object.M;
System.Console.Write(x.ToString());

public class C { }

static class E
{
    extension(object o)
    {
        public static C M => new C();
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,9): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'C'
            // var x = object.M + object.M;
            Diagnostic(ErrorCode.ERR_BadBinaryOps, "object.M + object.M").WithArguments("+", "C", "C").WithLocation(1, 9));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.M").ToArray();
        Assert.Equal("C E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        Assert.Equal("C E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());

        var binaryOp = GetSyntax<BinaryExpressionSyntax>(tree, "object.M + object.M");
        Assert.Null(model.GetSymbolInfo(binaryOp).Symbol);
    }

    [Fact]
    public void ResolveAll_IncrementOperator()
    {
        var source = """
object.M++;
System.Console.Write(E.M.ToString());

public class C { }

static class E
{
    extension(object o)
    {
        public static int M { get => 41; set { } }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; set; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        var unaryOp = GetSyntax<PostfixUnaryExpressionSyntax>(tree, "object.M++");
        Assert.Equal("System.Int32 System.Int32.op_Increment(System.Int32 value)", model.GetSymbolInfo(unaryOp).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_UnaryOperator()
    {
        var source = """
_ = !object.M;

public class C { }

static class E
{
    extension(object o)
    {
        public static bool M { get { System.Console.Write("ran"); return true; } }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Boolean E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        var unaryOp = GetSyntax<PrefixUnaryExpressionSyntax>(tree, "!object.M");
        Assert.Equal("System.Boolean System.Boolean.op_LogicalNot(System.Boolean value)",
            model.GetSymbolInfo(unaryOp).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_NullCoalescingOperator()
    {
        var source = """
var x = object.M ?? object.M2;
System.Console.Write(x);

static class E
{
    extension(object o)
    {
        public static string M => null;
        public static string M2 => "ran";
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M2");
        Assert.Equal("System.String E.<>E__0.M2 { get; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_NullCoalescingAssignmentOperator()
    {
        var source = """
object.M ??= object.M2;
System.Console.Write(E.M);

static class E
{
    extension(object o)
    {
        public static string M { get => null; set { } }
        public static string M2 => "ran";
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.String E.<>E__0.M { get; set; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M2");
        Assert.Equal("System.String E.<>E__0.M2 { get; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Query_Select()
    {
        var source = """
using System.Linq;

int[] array = [1];
var r = from int i in array select object.M;
foreach (var x in r)
{
    System.Console.Write(x.ToString());
}

static class E
{
    extension(object o)
    {
        public static string M => "ran";
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact(Skip = "PROTOTYPE WasPropertyBackingFieldAccessChecked asserts that we're setting twice")]
    public void ResolveAll_Query_Cast()
    {
        var source = """
using System.Linq;

var r = from string s in object.M from string s2 in object.M2 select s.ToString();
foreach (var x in r)
{
    System.Console.Write(x.ToString());
}

static class E
{
    extension(object o)
    {
        public static object[] M => ["ran"];
        public static object[] M2 => [""];
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Object[] E.M", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M2");
        Assert.Equal("System.Object[] E.M2", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Return_Lambda()
    {
        var source = """
var x = () =>
    {
        bool b = true;
        if (b)
            return object.M;
        else
            return object.M2;
    };
System.Console.Write(x().ToString());

static class E
{
    extension(object o)
    {
        public static int M => 42;
        public static int M2 => 0;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M2");
        Assert.Equal("System.Int32 E.<>E__0.M2 { get; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_ExpressionBodiedLambda()
    {
        var source = """
var x = () => object.M;
System.Console.Write(x().ToString());

static class E
{
    extension(object o)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_YieldReturn()
    {
        var source = """
foreach (var y in local())
{
    System.Console.Write(y.ToString());
}

System.Collections.Generic.IEnumerable<int> local()
{
    bool b = true;
    if (b)
        yield return object.M;
    else
        yield return object.M2;
}

static class E
{
    extension(object o)
    {
        public static int M => 42;
        public static int M2 => 0;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M2");
        Assert.Equal("System.Int32 E.<>E__0.M2 { get; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_YieldReturn_Lambda()
    {
        var source = """
var x = System.Collections.Generic.IEnumerable<int> () =>
    {
        bool b = true;
        if (b)
            yield return object.M;
        else
            yield return object.M2;
    };

foreach (var y in x())
{
    System.Console.Write(y.ToString());
}

static class E
{
    extension(object o)
    {
        public static int M => 42;
        public static int M2 => 0;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,56): error CS1643: Not all code paths return a value in lambda expression of type 'Func<IEnumerable<int>>'
            // var x = System.Collections.Generic.IEnumerable<int> () =>
            Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<System.Collections.Generic.IEnumerable<int>>").WithLocation(1, 56),
            // (5,13): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
            //             yield return object.M;
            Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield").WithLocation(5, 13),
            // (7,13): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
            //             yield return object.M2;
            Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield").WithLocation(7, 13));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M2");
        Assert.Equal("System.Int32 E.<>E__0.M2 { get; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Throw()
    {
        var source = """
try
{
    throw object.M;
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

static class E
{
    extension(object o)
    {
        public static System.Exception M => new System.Exception("ran");
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.Exception E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_FieldInitializer()
    {
        var source = """
System.Console.Write(C.field.ToString());

class C
{
    public static string field = object.M;
}

static class E
{
    extension(object o)
    {
        public static string M => "ran";
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Invocation_Static()
    {
        var src = """
local(object.M);

void local(string s)
{
    System.Console.Write(s);
}

static class E
{
    extension(object o)
    {
        public static string M => "ran";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.M").First();
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Invocation_Static_DelegateTypeParameter()
    {
        var src = """
local(object.M);

void local(System.Func<string> d)
{
    System.Console.Write(d());
}

static class E
{
    extension(object o)
    {
        public static string M() => "ran";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.M").First();
        Assert.Equal("System.String E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Invocation_Static_Inferred()
    {
        var src = """
System.Console.Write(local(object.M));

T local<T>(T t)
{
    return t;
}

static class E
{
    extension(object o)
    {
        public static string M => "ran";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.M").First();
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Invocation_Static_DelegateTypeParameter_InapplicableInstanceMember()
    {
        var src = """
local(object.ToString);

void local(System.Func<int, string> d)
{
    System.Console.Write(d(42));
}

static class E
{
    extension(object o)
    {
        public static string ToString(int i) => "ran";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.ToString").First();
        Assert.Equal("System.String E.<>E__0.ToString(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Invocation_Static_DelegateTypeParameter_PropertyAndMethod()
    {
        var src = """
var o = new object();
C.M(o.Member);

class C
{
    public static void M(System.Action a) { a(); }
}

static class E1
{
    extension(object o)
    {
        public string Member => throw null;
    }
}

public static class E2
{
    public static void Member(this object o)
    {
        System.Console.Write("ran");
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,5): error CS1503: Argument 1: cannot convert from 'string' to 'System.Action'
            // C.M(o.Member);
            Diagnostic(ErrorCode.ERR_BadArgType, "o.Member").WithArguments("1", "string", "System.Action").WithLocation(2, 5));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "o.Member");
        Assert.Equal("System.String E1.<>E__0.Member { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_ObjectCreation_Static()
    {
        var source = """
new C(object.M);

class C
{
    public C(string s) { System.Console.Write(s); }
}

static class E
{
    extension(object o)
    {
        public static string M => "ran";
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.String E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_BinaryOperator_Static_TwoExtensions()
    {
        var src = """
bool b = D.f + D.f;

class C
{
    public static bool operator +(C c, System.Action a) => true;
}

class D { }

static class E1
{
    extension(D d)
    {
        public static C f => null;
    }
}

static class E2
{
    extension(object o)
    {
        public static void f() { }
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,12): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // bool b = D.f + D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(1, 12),
            // (1,18): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // bool b = D.f + D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(1, 18)
            //// (1,10): error CS0019: Operator '+' cannot be applied to operands of type 'C' and 'C'
            //// bool b = D.f + D.f;
            //Diagnostic(ErrorCode.ERR_BadBinaryOps, "D.f + D.f").WithArguments("+", "C", "C").WithLocation(1, 10)
            );

        //var tree = comp.SyntaxTrees.First();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "D.f").ToArray();
        //Assert.Equal("C E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[0]).Symbol.ToTestDisplayString());
        //Assert.Equal("C E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess[1]).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Lambda_Static_TwoAsGoodExtensions_LambdaConverted()
    {
        var src = """
System.Func<System.Action> l = () => object.f;

static class E1
{
    extension(object o)
    {
        public static string f => null;
    }
}

static class E2
{
    extension(object o)
    {
        public static void f() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,45): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // System.Func<System.Action> l = () => object.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(1, 45));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void ResolveAll_Lambda_Instance_ExtensionMethodVsExtensionMember()
    {
        var src = """
System.Func<System.Action> lambda = () => new object().Member;

static class E
{
    extension(object o)
    {
        public string Member => throw null;
    }

    public static void Member(this object o)
    {
        System.Console.Write("ran");
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,43): error CS0029: Cannot implicitly convert type 'string' to 'System.Action'
            // System.Func<System.Action> lambda = () => new object().Member;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "new object().Member").WithArguments("string", "System.Action").WithLocation(1, 43),
            // (1,43): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
            // System.Func<System.Action> lambda = () => new object().Member;
            Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "new object().Member").WithArguments("lambda expression").WithLocation(1, 43));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().Member");
        Assert.Equal("System.String E.<>E__0.Member { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Lambda_Instance_MethodGroupWithMultipleOverloads()
    {
        var src = """
System.Func<System.Action> lambda = () => new object().Member;
lambda()();

static class E
{
    extension(object o)
    {
        public void Member() { System.Console.Write("ran"); }
        public void Member(int i) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().Member");
        Assert.Equal("void E.<>E__0.Member()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Lambda_Static_TwoExtensions_ConversionToDelegateType_ExplicitReturnType()
    {
        var src = """
var l = System.Action () => D.f;

class D { }

static class E1
{
    extension(object o)
    {
        public static string f => null;
    }
}
static class E2
{
    extension(object o)
    {
        public static void f() { System.Console.Write("ran"); }
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,31): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // var l = System.Action () => D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(1, 31)
            //// (1,29): error CS0029: Cannot implicitly convert type 'string' to 'System.Action'
            //// var l = System.Action () => D.f;
            //Diagnostic(ErrorCode.ERR_NoImplicitConv, "D.f").WithArguments("string", "System.Action").WithLocation(1, 29),
            //// (1,29): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
            //// var l = System.Action () => D.f;
            //Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "D.f").WithArguments("lambda expression").WithLocation(1, 29)
            );

        //var tree = comp.SyntaxTrees.First();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "D.f");
        //Assert.Equal("System.String E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Lambda_Static_TwoExtensions_ConversionToDelegateType()
    {
        var src = """
System.Func<System.Action> l = () => D.f;

class D { }

static class E1
{
    extension(D)
    {
        public static string f => null;
    }
}

static class E2
{
    extension(object)
    {
        public static void f() { System.Console.Write("ran"); }
    }
}
""";
        // PROTOTYPE we should prefer extension members that apply to a more specific type
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,40): error CS0229: Ambiguity between 'E2.extension.f()' and 'E1.extension.f'
            // System.Func<System.Action> l = () => D.f;
            Diagnostic(ErrorCode.ERR_AmbigMember, "f").WithArguments("E2.extension.f()", "E1.extension.f").WithLocation(1, 40)
            //// (1,38): error CS0029: Cannot implicitly convert type 'string' to 'System.Action'
            //// System.Func<System.Action> l = () => D.f;
            //Diagnostic(ErrorCode.ERR_NoImplicitConv, "D.f").WithArguments("string", "System.Action").WithLocation(1, 38),
            //// (1,38): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
            //// System.Func<System.Action> l = () => D.f;
            //Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "D.f").WithArguments("lambda expression").WithLocation(1, 38)
            );

        //var tree = comp.SyntaxTrees.First();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "D.f");
        //Assert.Equal("System.String E1.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_SwitchExpression_Static_Default()
    {
        var src = """
bool b = true;
var s = b switch { true => object.f, false => default };
System.Console.Write(s);

static class E
{
    extension(object)
    {
        public static string f => "hi";
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "hi").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("System.String E.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());

        var defaultExpr = GetSyntax<LiteralExpressionSyntax>(tree, "default");
        Assert.Equal("System.String", model.GetTypeInfo(defaultExpr).Type.ToTestDisplayString());
        Assert.Equal("System.String", model.GetTypeInfo(defaultExpr).ConvertedType.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_RefTernary()
    {
        var src = """
bool b = true;
var x = b ? ref object.f : ref object.f;
System.Console.Write(x);

static class E
{
    extension(object o)
    {
        public static ref string f => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.f").First();
        Assert.Equal("ref System.String E.<>E__0.f { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ResolveAll_Query_Static_InstanceMethodGroup()
    {
        var src = """
string query = from x in object.ToString select x;

static class E
{
    extension(object)
    {
        public static string ToString() => null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,33): error CS0119: 'object.ToString()' is a method, which is not valid in the given context
            // string query = from x in object.ToString select x;
            Diagnostic(ErrorCode.ERR_BadSKunknown, "ToString").WithArguments("object.ToString()", "method").WithLocation(1, 33));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.ToString");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["System.String System.Object.ToString()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_Query_Static_ExtensionMethodGroup()
    {
        var src = """
string query = from x in object.M select x;

static class E
{
    extension(object o)
    {
        public static string M() => null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,33): error CS0119: 'E.extension.M()' is a method, which is not valid in the given context
            // string query = from x in object.M select x;
            Diagnostic(ErrorCode.ERR_BadSKunknown, "M").WithArguments("E.extension.M()", "method").WithLocation(1, 33));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void ResolveAll_Instance_Invocation_InnerInapplicableExtensionMethodVsOuterInvocableExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            new object().M();
        }
    }

    public static class Extension
    {
        public static void M(this object o, int i) { } // not applicable because of second parameter
    }
}

static class E
{
    extension(object o)
    {
        public System.Action M => () => { System.Console.Write("ran"); };
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Equal("System.Action E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_Instance_Invocation_InnerIrrelevantExtensionMethodVsOuterInvocableExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            new object().M();
        }
    }

    public static class Extension
    {
        public static void M(this string o, int i) { } // not eligible because of `this` parameter
    }
}

static class E
{
    extension(object o)
    {
        public System.Action M => () => { System.Console.Write("ran"); };
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Equal("System.Action E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_Instance_InferredVariable_InnerExtensionMethodVsOuterInvocableExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            var x = new object().M;
            x(42);
        }
    }

    public static class Extension
    {
        public static void M(this object o, int i) { System.Console.Write("ran"); }
    }
}

static class E
{
    extension(object o)
    {
        public System.Action M => () => throw null;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Equal("void System.Object.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void System.Object.M(System.Int32 i)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_Instance_LocalDeclaration_InnerExtensionMethodVsOuterExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            int x = new object().M;
        }
    }

    public static class Extension
    {
        public static void M(this object o, int i) { }
    }
}

static class E
{
    extension(object o)
    {
        public int M => 42;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics(
            // (7,34): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
            //             int x = new object().M;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "int").WithLocation(7, 34));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["void System.Object.M(System.Int32 i)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_Static_LocalDeclaration_InnerExtensionMethodVsOuterExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            int x = object.M;
        }
    }

    public static class Extension
    {
        public static void M(this object o, int i) { }
    }
}

static class E
{
    extension(object o)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics(
            // (7,28): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
            //             int x = object.M;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "int").WithLocation(7, 28));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["void System.Object.M(System.Int32 i)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_Static_LocalDeclaration_InstanceInnerExtensionTypeMethodVsOuterExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            int x = object.M;
        }
    }

    static class E1
    {
        extension(object o)
        {
            public void M(int i) { }
        }
    }
}

static class E2
{
    extension(object)
    {
        public static int M => 42;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics(
            // (7,28): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
            //             int x = object.M;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "int").WithLocation(7, 28));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void ResolveAll_Instance_LocalDeclaration_StaticInnerExtensionTypeMethodVsOuterExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            int x = new object().M;
        }
    }

    static class E1
    {
        extension(object)
        {
            public static void M(int i) => throw null;
        }
    }
}

static class E2
{
    extension(object o)
    {
        public int M => 42;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics(
            // (7,34): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
            //             int x = new object().M;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "int").WithLocation(7, 34));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void ResolveAll_Instance_LocalDeclaration_InnerIrrelevantExtensionMethodVsOuterExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            int x = new object().M;
            System.Console.Write(x);
        }
    }

    public static class Extension
    {
        public static void M(this string o, int i) { } // not eligible because of `this` parameter
    }
}

static class E
{
    extension(object o)
    {
        public int M => 42;
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Equal("System.Int32 E.<>E__0.M { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings());
    }

    [Fact]
    public void ResolveAll_Instance_LocalDeclaration_DelegateType_InnerInapplicableExtensionMethodVsOuterExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            System.Action x = new object().M;
        }
    }

    public static class Extension
    {
        public static void M(this object o, int i) { }
    }
}

static class E
{
    extension(object o)
    {
        public void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void System.Object.M(System.Int32 i)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void ResolveAll_Instance_LocalDeclaration_DelegateType_InnerIrrelevantExtensionMethodVsOuterExtensionProperty()
    {
        var src = """
namespace N
{
    public class C
    {
        public static void Main()
        {
            System.Action x = new object().M;
        }
    }

    public static class Extension
    {
        public static void M(this string o, int i) { } // not eligible because of `this` parameter
    }
}

static class E
{
    extension(object o)
    {
        public void M() { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Equal("void E.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void ResolveAll_ConditionalOperator_Static_TwoAsGoodExtensions_Property()
    {
        var source = """
bool b = true;
var x = b ? object.StaticProperty : object.StaticProperty;

static class E1
{
    extension(object)
    {
        public static int StaticProperty => 42;
    }
}
static class E2
{
    extension(object)
    {
        public static int StaticProperty => 42;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (2,20): error CS0229: Ambiguity between 'E1.extension.StaticProperty' and 'E2.extension.StaticProperty'
            // var x = b ? object.StaticProperty : object.StaticProperty;
            Diagnostic(ErrorCode.ERR_AmbigMember, "StaticProperty").WithArguments("E1.extension.StaticProperty", "E2.extension.StaticProperty").WithLocation(2, 20),
            // (2,44): error CS0229: Ambiguity between 'E1.extension.StaticProperty' and 'E2.extension.StaticProperty'
            // var x = b ? object.StaticProperty : object.StaticProperty;
            Diagnostic(ErrorCode.ERR_AmbigMember, "StaticProperty").WithArguments("E1.extension.StaticProperty", "E2.extension.StaticProperty").WithLocation(2, 44));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "object.StaticProperty").ToArray();
        Assert.Null(model.GetSymbolInfo(memberAccess[0]).Symbol);
        Assert.Null(model.GetSymbolInfo(memberAccess[1]).Symbol);
    }

    [Fact]
    public void DelegateConversion_TypeReceiver()
    {
        var source = """
D d = C.M;
d(42);

delegate void D(int i);

class C { }

static class E
{
    extension(C)
    {
        public static void M(int i) { System.Console.Write("E.M"); }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "E.M");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_InstanceReceiver()
    {
        var source = """
D d = new C().M;
d(42);

delegate void D(int i);

class C
{
    public void M() => throw null;
}

static class E
{
    extension(C c)
    {
        public void M(int i) { System.Console.Write("E.M"); }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //var verifier = CompileAndVerify(comp, expectedOutput: "E.M");
        //verifier.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M");
        Assert.Equal("void E.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M()"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_TypeReceiver_Overloads()
    {
        var source = """
D d = C.M;
d(42);

C.M(42);

delegate void D(int i);

class C { }

static class E
{
    extension(C)
    {
        public static void M(int i) { System.Console.Write("ran "); }

        public static void M(string s) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C.M").First();
        Assert.Equal("void E.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_ValueReceiver_Overloads()
    {
        var source = """
D d = new C().M;
d(42);

new C().M(42);

delegate void D(int i);

class C { }

static class E
{
    extension(C c)
    {
        public void M(int i) { System.Console.Write("ran "); }
        public void M(string s) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").First();
        Assert.Equal("void E.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_TypeReceiver_Overloads_DifferentExtensions()
    {
        var source = """
D d = C.M;
d(42);

C.M(42);

delegate void D(int i);

class C { }

static class E1
{
    extension(C)
    {
        public static void M(int i) { System.Console.Write("ran "); }
    }
}
static class E2
{
    extension(C)
    {
        public static void M(string s) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C.M").First();
        Assert.Equal("void E1.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_WrongSignature()
    {
        var source = """
D d = new C().M;
d(42);

new C().M(42);

delegate void D(int i);

class C { }

static class E
{
    extension(C c)
    {
        public void M(string s) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,15): error CS0123: No overload for 'M' matches delegate 'D'
            // D d = new C().M;
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M").WithArguments("M", "D").WithLocation(1, 15),
            // (4,11): error CS1503: Argument 1: cannot convert from 'int' to 'string'
            // new C().M(42);
            Diagnostic(ErrorCode.ERR_BadArgType, "42").WithArguments("1", "int", "string").WithLocation(4, 11));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").First();
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_TypeReceiver_ZeroArityMatchesAny()
    {
        var source = """
D d = object.Method;
d("");

d = object.Method<string>;
d("");

delegate void D(string s);

static class E
{
    extension(object)
    {
        public static void Method(int i) => throw null;
        public static void Method<T>(T t) { System.Console.Write("Method "); }
        public static void Method<T1, T2>(T1 t1, T2 t2) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "Method Method");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.Method");
        Assert.Equal("void E.<>E__0.Method<System.String>(System.String t)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_ValueReceiver_Overloads_OuterScope_WithInapplicableInstanceMember()
    {
        var source = """
using N;

D d = new C().M;
d(42);

new C().M(42);

delegate void D(int i);

class C
{
    public void M(char c) { }
}

namespace N
{
    static class E1
    {
        extension(C c)
        {
            public void M(int i)
            {
                System.Console.Write("ran ran");
            }
        }
    }
}

static class E2
{
    extension(C c)
    {
        public void M(string s) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "new C().M").First();
        Assert.Equal("void N.E1.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal(["void C.M(System.Char c)"], model.GetMemberGroup(memberAccess).ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_TypeReceiver_Overloads_InnerScope()
    {
        var source = """
using N;

D d = C.M;
d(42);

delegate void D(int i);

class C { }

static class E1
{
    extension(C)
    {
        public static void M(int i) { System.Console.Write("ran"); }
    }
}

namespace N
{
    static class E2
    {
        extension(C)
        {
            public static void M(int i) => throw null;
        }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // 0.cs(1,1): hidden CS8019: Unnecessary using directive.
            // using N;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N;").WithLocation(1, 1));

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E1.<>E__0.M(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void DelegateConversion_TypeReceiver_TypeArguments()
    {
        var source = """
D d = C.M<object>;
d(42);

delegate void D(int i);
class C { }

static class E
{
    extension(C)
    {
        public static void M(int i) => throw null;
        public static void M<T>(int i) { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M<object>");
        Assert.Equal("void E.<>E__0.M<System.Object>(System.Int32 i)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void InstancePropertyAccess_Obsolete()
    {
        var src = """
_ = new object().Property;
new object().Property = 43;

static class E
{
    extension(object o)
    {
        [System.Obsolete("Property is obsolete", true)]
        public int Property { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0619: 'E.extension.Property' is obsolete: 'Property is obsolete'
            // _ = new object().Property;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new object().Property").WithArguments("E.extension.Property", "Property is obsolete").WithLocation(1, 5),
            // (2,1): error CS0619: 'E.extension.Property' is obsolete: 'Property is obsolete'
            // new object().Property = 43;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new object().Property").WithArguments("E.extension.Property", "Property is obsolete").WithLocation(2, 1));
    }

    [Fact]
    public void StaticPropertyAccess_Obsolete()
    {
        var src = """
_ = object.Property;
object.Property = 43;

static class E
{
    extension(object)
    {
        [System.Obsolete("Property is obsolete", true)]
        public static int Property { get => 42; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0619: 'E.extension.Property' is obsolete: 'Property is obsolete'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "object.Property").WithArguments("E.extension.Property", "Property is obsolete").WithLocation(1, 5),
            // (2,1): error CS0619: 'E.extension.Property' is obsolete: 'Property is obsolete'
            // object.Property = 43;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "object.Property").WithArguments("E.extension.Property", "Property is obsolete").WithLocation(2, 1));
    }

    [Fact]
    public void InstancePropertyAccess_Obsolete_InInvocation()
    {
        var src = """
new object().Property();

static class E
{
    extension(object o)
    {
        [System.Obsolete("Property is obsolete", true)]
        public System.Action Property { get => throw null; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0619: 'E.extension.Property' is obsolete: 'Property is obsolete'
            // new object().Property();
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new object().Property").WithArguments("E.extension.Property", "Property is obsolete").WithLocation(1, 1));
    }

    [Fact]
    public void InstancePropertyAccess_ColorColor()
    {
        var src = """
class C
{
    static void M(C C)
    {
        C.Property = 42;
    }
}

static class E
{
    extension(C c)
    {
        public int Property { set { System.Console.Write("Property"); } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "Property");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Property");
        Assert.Equal("System.Int32 E.<>E__0.Property { set; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));
    }

    [Fact]
    public void StaticPropertyAccess_ColorColor()
    {
        var src = """
class C
{
    static void M(C C)
    {
        C.Property = 42;
    }
}

static class E
{
    extension(C c)
    {
        public static int Property { set { System.Console.Write("Property"); } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "Property");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var property = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Property");
        Assert.Equal("System.Int32 E.<>E__0.Property { set; }", model.GetSymbolInfo(property).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(property));
    }

    [Fact]
    public void ConditionalReceiver_Property_MemberAccess()
    {
        var src = """
bool b = true;
System.Console.Write((b ? "" : null).Property.ToString());

static class E
{
    extension(string s)
    {
        public int Property => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE(instance) execute once instance scenarios are implemented
        //CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, """(b ? "" : null).Property""");
        Assert.Equal("System.Int32 E.<>E__0.Property { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void PropertyAccess_ReturnNotLValue()
    {
        var src = """
object.Property.field = 1;

public struct S
{
    public int field;
}
static class E
{
    extension(object)
    {
        public static S Property { get => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS1612: Cannot modify the return value of 'E.extension.Property' because it is not a variable
            // object.Property.field = 1;
            Diagnostic(ErrorCode.ERR_ReturnNotLValue, "object.Property").WithArguments("E.extension.Property").WithLocation(1, 1));
    }

    [Fact]
    public void StaticPropertyAccess_RefProperty_01()
    {
        var src = """
localFuncRef(ref object.Property);
localFuncOut(out object.Property);

void localFuncRef(ref int i) => throw null;
void localFuncOut(out int i) => throw null;

static class E
{
    extension(object)
    {
        public static int Property { get => throw null; set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,18): error CS0206: A non ref-returning property or indexer may not be used as an out or ref value
            // localFuncRef(ref object.Property);
            Diagnostic(ErrorCode.ERR_RefProperty, "object.Property").WithLocation(1, 18),
            // (2,18): error CS0206: A non ref-returning property or indexer may not be used as an out or ref value
            // localFuncOut(out object.Property);
            Diagnostic(ErrorCode.ERR_RefProperty, "object.Property").WithLocation(2, 18));
    }

    [Fact]
    public void StaticPropertyAccess_RefProperty_02()
    {
        var src = """
localFuncRef(ref object.Property);

void localFuncRef(ref int i) => throw null;

static class E
{
    extension(object)
    {
        public static ref int Property { get => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void StaticPropertyAccess_AssignReadonlyNotField()
    {
        var src = """
object.Property = 1;

static class E
{
    extension(object)
    {
        public static ref readonly int Property { get => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS8331: Cannot assign to property 'Property' or use it as the right hand side of a ref assignment because it is a readonly variable
            // object.Property = 1;
            Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "object.Property").WithArguments("property", "Property").WithLocation(1, 1));
    }

    [Fact]
    public void StaticPropertyAccess_AssgReadonlyProp()
    {
        var src = """
object.Property = 1;

static class E
{
    extension(object)
    {
        public static int Property { get => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0200: Property or indexer 'E.extension.Property' cannot be assigned to -- it is read only
            // object.Property = 1;
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "object.Property").WithArguments("E.extension.Property").WithLocation(1, 1));
    }

    [Fact]
    public void StaticPropertyAccess_InitOnlyProperty()
    {
        var src = """
object.Property = 1;

static class E
{
    extension(object)
    {
        public static int Property { init => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (7,38): error CS8856: The 'init' accessor is not valid on static members
            //         public static int Property { init => throw null; }
            Diagnostic(ErrorCode.ERR_BadInitAccessor, "init").WithLocation(7, 38));
    }

    [Fact]
    public void InstancePropertyAccess_InitOnlyProperty()
    {
        var src = """
new object().Property = 1;

static class E
{
    extension(object o)
    {
        public int Property { init => throw null; }
    }
}
""";
        // PROTOTYPE(instance) confirm whether init-only accessors should be allowed in extensions
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS8852: Init-only property or indexer 'E.extension.Property' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
            // new object().Property = 1;
            Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "new object().Property").WithArguments("E.extension.Property").WithLocation(1, 1));
    }

    [ConditionalFact(typeof(NoUsedAssembliesValidation))] // PROTOTYPE metadata is undone
    public void InstancePropertyAccess_InitOnlyProperty_ObjectInitializer()
    {
        var src = """
_ = new object() { Property = 1 };

static class E
{
    extension(object o)
    {
        public int Property { init => throw null; }
    }
}
""";
        // PROTOTYPE confirm whether init-only accessors should be allowed in extensions
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "Property = 1");
        Assert.Equal("System.Int32 E.<>E__0.Property { init; }", model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void StaticPropertyAccess_InaccessibleSetter()
    {
        var src = """
object.Property = 1;

static class E
{
    extension(object)
    {
        public static int Property { get => throw null; private set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0272: The property or indexer 'E.extension.Property' cannot be used in this context because the set accessor is inaccessible
            // object.Property = 1;
            Diagnostic(ErrorCode.ERR_InaccessibleSetter, "object.Property").WithArguments("E.extension.Property").WithLocation(1, 1));
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionTypeMethods()
    {
        // See ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_Method
        var source = """
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color { }

static class E
{
    extension(Color c)
    {
        public void M1(S1 x, int y = 0)
        {
            System.Console.WriteLine("instance");
        }

        public static void M1<T>(T x) where T : unmanaged
        {
            System.Console.WriteLine("static");
        }
    }
}
""";
        // PROTOTYPE missing ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
        comp.VerifyEmitDiagnostics(
            //// (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
            ////         Color.M1(this);
            //Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
            );

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.M1");
        Assert.Equal("void E.<>E__0.M1(S1 x, [System.Int32 y = 0])", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionTypeProperties()
    {
        var source = """
struct S1(Color Color)
{
    public void Test()
    {
        _ = Color.P1;
    }
}

class Color { }

static class E1
{
    extension(Color c)
    {
        public int P1 => 0;
    }
}

static class E2
{
    extension(Color)
    {
        public static int P1 => 0;
    }
}
""";
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
        comp.VerifyEmitDiagnostics(
            // (5,19): error CS0229: Ambiguity between 'E1.extension.P1' and 'E2.extension.P1'
            //         _ = Color.P1;
            Diagnostic(ErrorCode.ERR_AmbigMember, "P1").WithArguments("E1.extension.P1", "E2.extension.P1").WithLocation(5, 19));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.P1");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
    }

    [Fact]
    public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_ExtensionTypeMembersVsExtensionMethod()
    {
        var source = """
public struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

public class Color { }

public static class E1
{
    public static void M1(this Color c, S1 x, int y = 0)
    {
        System.Console.WriteLine("instance");
    }
}

static class E
{
    extension(Color)
    {
        public static void M1<T>(T x) where T : unmanaged
        {
            System.Console.WriteLine("static");
        }
    }
}
""";
        // PROTOTYPE missing ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
        comp.VerifyEmitDiagnostics(
            //// (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
            ////         Color.M1(this);
            //Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
            );

        Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Color.M1");
        Assert.Equal("void Color.M1(S1 x, [System.Int32 y = 0])", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void InstanceMethod_MemberAccess()
    {
        var src = """
new object().M.ToString();

static class E
{
    extension(object o)
    {
        public int M() => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS0119: 'E.extension.M()' is a method, which is not valid in the given context
            // new object().M.ToString();
            Diagnostic(ErrorCode.ERR_BadSKunknown, "M").WithArguments("E.extension.M()", "method").WithLocation(1, 14));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "new object().M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        // PROTOTYPE semantic model is undone
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void InstanceMethod_MemberAccess_Missing()
    {
        var src = """
new object().M.ToString();
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS1061: 'object' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // new object().M.ToString();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("object", "M").WithLocation(1, 14));
    }

    [Fact]
    public void CheckValueKind_AssignToMethodGroup()
    {
        var src = """
object.M = null;

static class E
{
    extension(object)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS1656: Cannot assign to 'M' because it is a 'method group'
            // object.M = null;
            Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "object.M").WithArguments("M", "method group").WithLocation(1, 1));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess).CandidateReason);
    }

    [Fact]
    public void AccessOnVoid_Invocation()
    {
        var src = """
object.M().ToString();

static class E
{
    extension(object)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,11): error CS0023: Operator '.' cannot be applied to operand of type 'void'
            // object.M().ToString();
            Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "void").WithLocation(1, 11));
    }

    [Fact]
    public void ExtensionMemberLookup_InaccessibleMembers()
    {
        var src = """
object.Method();
_ = object.Property;

static class E
{
    extension(object o)
    {
        private static void Method() => throw null;
        private static int Property => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'Method'
            // object.Method();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method").WithArguments("object", "Method").WithLocation(1, 8),
            // (2,12): error CS0117: 'object' does not contain a definition for 'Property'
            // _ = object.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property").WithLocation(2, 12));
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_Simple()
    {
        var src = """
_ = f($"{(object)1} {f2()}");

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendLiteralExtensionMethod()
    {
        var src = """
_ = f($"{(object)1} {f2()}");

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

public static class Extensions
{
    public static void AppendLiteral(this InterpolationHandler ih, string value) { }
}
""";

        // Interpolation handlers don't allow extension methods
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,20): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendLiteral' and no accessible extension method 'AppendLiteral' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, " ").WithArguments("InterpolationHandler", "AppendLiteral").WithLocation(1, 20),
            // (1,20): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, " ").WithArguments("?.()").WithLocation(1, 20));
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendLiteralExtensionDeclarationMethod()
    {
        var src = """
_ = f($"{(object)1} {f2()}");

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(InterpolationHandler i)
    {
        public void AppendLiteral(string value) { }
    }
}
""";

        // Interpolation handlers don't allow extension methods
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,20): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendLiteral' and no accessible extension method 'AppendLiteral' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, " ").WithArguments("InterpolationHandler", "AppendLiteral").WithLocation(1, 20),
            // (1,20): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, " ").WithArguments("?.()").WithLocation(1, 20));
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendFormattedExtensionMethod()
    {
        var src = """
/*<bind>*/
_ = f($"{(object)1} {f2()}");
/*</bind>*/

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendLiteral(string value) { }
}

public static class Extensions
{
    public static void AppendFormatted<T>(this InterpolationHandler ih, T hole, int alignment = 0, string format = null) { }
}
""";

        // Interpolation handlers don't allow extension methods
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (2,9): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{(object)1}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(2, 9),
            // (2,9): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{(object)1}").WithArguments("?.()").WithLocation(2, 9),
            // (2,21): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{f2()}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(2, 21),
            // (2,21): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{f2()}").WithArguments("?.()").WithLocation(2, 21)
            );
    }

    [Fact]
    public void ExtensionMemberLookup_InterpolationHandler_AppendFormattedExtensionTypeMethod()
    {
        var src = """
_ = f($"{(object)1} {f2()}");

static int f(InterpolationHandler s) => 0;
static string f2() => "hello";

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{
    public InterpolationHandler(int literalLength, int formattedCount) => throw null;
    public void AppendLiteral(string value) { }
}

static class E
{
    extension(InterpolationHandler i)
    {
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) { }
    }
}
""";

        // Interpolation handlers don't allow extension methods
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,9): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{(object)1}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(1, 9),
            // (1,9): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{(object)1}").WithArguments("?.()").WithLocation(1, 9),
            // (1,21): error CS1061: 'InterpolationHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'InterpolationHandler' could be found (are you missing a using directive or an assembly reference?)
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{f2()}").WithArguments("InterpolationHandler", "AppendFormatted").WithLocation(1, 21),
            // (1,21): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
            // _ = f($"{(object)1} {f2()}");
            Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{f2()}").WithArguments("?.()").WithLocation(1, 21));
    }

    [Fact]
    public void LiteralReceiver_Property_Enum_Set()
    {
        var src = """
Enum.Zero.Property = 1;

enum Enum
{
    Zero
}

static class E
{
    extension(Enum e)
    {
        public int Property { set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        // Consider improving the error message
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            // Enum.Zero.Property = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "Enum.Zero.Property").WithLocation(1, 1));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "Enum.Zero.Property");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["System.Int32 E.<>E__0.Property { set; }"], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.NotAVariable, model.GetSymbolInfo(memberAccess).CandidateReason);
    }

    [Fact]
    public void LiteralReceiver_Property_Integer_ForLong()
    {
        var src = """
1.Property = 42;
_ = 2.Property;

static class E
{
    extension(long l)
    {
        public int Property { get { System.Console.Write("get "); return 42; } set { System.Console.Write("set "); }  }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,3): error CS1061: 'int' does not contain a definition for 'Property' and no accessible extension method 'Property' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
            // 1.Property = 42;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Property").WithArguments("int", "Property").WithLocation(1, 3),
            // (2,7): error CS1061: 'int' does not contain a definition for 'Property' and no accessible extension method 'Property' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
            // _ = 2.Property;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Property").WithArguments("int", "Property").WithLocation(2, 7));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "1.Property");
        Assert.Null(model.GetSymbolInfo(memberAccess1).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess1).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess1).CandidateReason);

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "2.Property");
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess2).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess2).CandidateReason);
    }

    [Fact]
    public void LiteralReceiver_Property_String()
    {
        var src = """
"".Property = 42;
_ = "".Property;

static class E
{
    extension(string s)
    {
        public int Property { get { System.Console.Write("get "); return 42; } set { System.Console.Write("set "); }  }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "set get").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "\"\".Property").First();
        Assert.Equal("System.Int32 E.<>E__0.Property { get; set; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "\"\".Property").Last();
        Assert.Equal("System.Int32 E.<>E__0.Property { get; set; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void SwitchReceiver_Property_String()
    {
        var src = """
bool b = true;
(b switch { true => "", _ => "" }).Property = 42;
_ = (b switch { true => "", _ => "" }).Property;

static class E
{
    extension(string s)
    {
        public int Property { get { System.Console.Write("get "); return 42; } set { System.Console.Write("set "); }  }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "set get").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, """(b switch { true => "", _ => "" }).Property""").First();
        Assert.Equal("System.Int32 E.<>E__0.Property { get; set; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, """(b switch { true => "", _ => "" }).Property""").Last();
        Assert.Equal("System.Int32 E.<>E__0.Property { get; set; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ConditionalReceiver_Property_String()
    {
        var src = """
bool b = true;
(b ? "" : null).Property = 42;
_ = (b ? "" : null).Property;

static class E
{
    extension(string s)
    {
        public int Property { get { System.Console.Write("get "); return 42; } set { System.Console.Write("set "); }  }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "set get").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, """(b ? "" : null).Property""").First();
        Assert.Equal("System.Int32 E.<>E__0.Property { get; set; }", model.GetSymbolInfo(memberAccess1).Symbol.ToTestDisplayString());

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, """(b ? "" : null).Property""").Last();
        Assert.Equal("System.Int32 E.<>E__0.Property { get; set; }", model.GetSymbolInfo(memberAccess2).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void LiteralReceiver_Property_Integer_Get()
    {
        var src = """
_ = 1.Property;

static class E
{
    extension(int i)
    {
        public int Property { get { System.Console.Write("get"); return 42; } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "get").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "1.Property");
        Assert.Equal("System.Int32 E.<>E__0.Property { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void LiteralReceiver_Property_Integer_Set()
    {
        var src = """
1.Property = 1;

static class E
{
    extension(int i)
    {
        public int Property { set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        // Consider improving the error message
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            // 1.Property = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "1.Property").WithLocation(1, 1));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "1.Property");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["System.Int32 E.<>E__0.Property { set; }"], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.NotAVariable, model.GetSymbolInfo(memberAccess).CandidateReason);
    }

    [Fact]
    public void LiteralReceiver_Property_Null()
    {
        var src = """
null.Property = 1;
_ = null.Property;

static class E
{
    extension(object o)
    {
        public int Property { get => throw null; set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
            // null.Property = 1;
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.Property").WithArguments(".", "<null>").WithLocation(1, 1),
            // (2,5): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
            // _ = null.Property;
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.Property").WithArguments(".", "<null>").WithLocation(2, 5));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "null.Property").First();
        Assert.Null(model.GetSymbolInfo(memberAccess1).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess1).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess1).CandidateReason);

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "null.Property").Last();
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess2).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess2).CandidateReason);
    }

    [Fact]
    public void LiteralReceiver_Property_Default()
    {
        var src = """
default.Property = 1;
_ = default.Property;

static class E
{
    extension(object o)
    {
        public int Property { get => throw null; set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS8716: There is no target type for the default literal.
            // default.Property = 1;
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(1, 1),
            // (2,5): error CS8716: There is no target type for the default literal.
            // _ = default.Property;
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(2, 5));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "default.Property").First();
        Assert.Null(model.GetSymbolInfo(memberAccess1).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess1).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess1).CandidateReason);

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "default.Property").Last();
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess2).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess2).CandidateReason);
    }

    [Fact]
    public void LiteralReceiver_Property_Tuple_Get()
    {
        var src = """
_ = (1, 2).Property;

static class E
{
    extension((int, int) t)
    {
        public int Property { get { System.Console.Write("get "); return 42; } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "get").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "(1, 2).Property");
        Assert.Equal("System.Int32 E.<>E__0.Property { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void LiteralReceiver_Property_Tuple_Set()
    {
        var src = """
(1, 2).Property = 1;

static class E
{
    extension((int, int) t)
    {
        public int Property { set { System.Console.Write($"set(value)"); }}
    }
}
""";
        var comp = CreateCompilation(src);
        // Consider improving the error message
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            // (1, 2).Property = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "(1, 2).Property").WithLocation(1, 1));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "(1, 2).Property");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["System.Int32 E.<>E__0.Property { set; }"], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.NotAVariable, model.GetSymbolInfo(memberAccess).CandidateReason);
    }

    [Fact]
    public void LiteralReceiver_Property_Tuple_Default()
    {
        var src = """
(default, default).Property = 1;
_ = (default, default).Property;

static class E
{
    extension((object, object) t)
    {
        public int Property { get => throw null; set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,2): error CS8716: There is no target type for the default literal.
            // (default, default).Property = 1;
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(1, 2),
            // (1,11): error CS8716: There is no target type for the default literal.
            // (default, default).Property = 1;
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(1, 11),
            // (2,6): error CS8716: There is no target type for the default literal.
            // _ = (default, default).Property;
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(2, 6),
            // (2,15): error CS8716: There is no target type for the default literal.
            // _ = (default, default).Property;
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(2, 15));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "(default, default).Property").First();
        Assert.Null(model.GetSymbolInfo(memberAccess1).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess1).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess1).CandidateReason);

        var memberAccess2 = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "(default, default).Property").Last();
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess2).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess2).CandidateReason);
    }

    [Fact]
    public void LiteralReceiver_Property_Tuple_Integer_ForLong()
    {
        var src = """
(1, 1).Property = 1;
_ = (2, 2).Property;

static class E
{
    extension((long, long) t)
    {
        public int Property { get => throw null; set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS1061: '(int, int)' does not contain a definition for 'Property' and no accessible extension method 'Property' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
            // (1, 1).Property = 1;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Property").WithArguments("(int, int)", "Property").WithLocation(1, 8),
            // (2,12): error CS1061: '(int, int)' does not contain a definition for 'Property' and no accessible extension method 'Property' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
            // _ = (2, 2).Property;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Property").WithArguments("(int, int)", "Property").WithLocation(2, 12));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "(1, 1).Property");
        Assert.Null(model.GetSymbolInfo(memberAccess1).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess1).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess1).CandidateReason);

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "(2, 2).Property");
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess2).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(memberAccess2).CandidateReason);
    }

    [Fact]
    public void PreferMoreSpecific_Static_MethodAndProperty()
    {
        var src = """
System.Console.Write(object.M);

static class E1
{
    extension(object)
    {
        public static string M() => throw null;
    }
}

static class E2
{
    extension(object)
    {
        public static string M => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,29): error CS0229: Ambiguity between 'E1.extension.M()' and 'E2.extension.M'
            // System.Console.Write(object.M);
            Diagnostic(ErrorCode.ERR_AmbigMember, "M").WithArguments("E1.extension.M()", "E2.extension.M").WithLocation(1, 29));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["System.String E1.<>E__0.M()", "System.String E2.<>E__0.M { get; }"],
            model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void PreferMoreSpecific_Static_MethodAndProperty_Invocation()
    {
        var src = """
System.Console.Write(object.M());

static class E1
{
    extension(object)
    {
        public static string M() => "ran";
    }
}

static class E2
{
    extension(object)
    {
        public static string M => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Equal("System.String E1.<>E__0.M()", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void GetCompatibleExtensions_TwoSubstitutions()
    {
        var src = """
C.M();
new C().M2();

interface I<T> { }
class C : I<int>, I<string> { }

static class E
{
    extension<T>(I<T>)
    {
        public static void M() { }
    }

    public static void M2<T>(this I<T> i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,3): error CS0117: 'C' does not contain a definition for 'M'
            // C.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("C", "M").WithLocation(1, 3),
            // (2,9): error CS1061: 'C' does not contain a definition for 'M2' and no accessible extension method 'M2' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            // new C().M2();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M2").WithArguments("C", "M2").WithLocation(2, 9));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var memberAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess1).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess1).CandidateSymbols.ToTestDisplayStrings());
        Assert.Empty(model.GetMemberGroup(memberAccess1));

        var memberAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "new C().M2");
        Assert.Null(model.GetSymbolInfo(memberAccess2).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess2).CandidateSymbols.ToTestDisplayStrings());
        Assert.Empty(model.GetMemberGroup(memberAccess2));
    }

    [Theory, ClassData(typeof(ThreePermutationGenerator))]
    public void PreferMoreSpecific_Static_MethodAndMoreSpecificInvocablePropertyAndMoreSpecificMethod(int first, int second, int third)
    {
        string[] segments = [
            """
            static class E1
            {
                extension(object)
                {
                    public static string M() => throw null;
                }
            }
            """,
            """
            static class E2
            {
                extension(C)
                {
                    public static System.Func<string> M => null;
                }
            }
            """,
            """
            static class E3
            {
                extension(C)
                {
                    public static string M() => throw null;
                }
            }
            """];

        var src = $$"""
System.Console.Write(C.M());

class C { }

{{segments[first]}}

{{segments[second]}}

{{segments[third]}}
""";
        var comp = CreateCompilation(src);

        // PROTOTYPE we should prefer extension members that apply to a more specific type (ie. no error)
        comp.VerifyEmitDiagnostics(
            // (1,24): error CS0229: Ambiguity between 'E1.extension.M()' and 'E3.extension.M()'
            // System.Console.Write(C.M());
            Diagnostic(ErrorCode.ERR_AmbigMember, "M").WithArguments("E1.extension.M()", "E3.extension.M()").WithLocation(1, 24));

        //var tree = comp.SyntaxTrees.Single();
        //var model = comp.GetSemanticModel(tree);
        //var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        //Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);

        //Assert.Equal(["System.Func<System.String> E2.<>E__0.M", "System.String E3.<>E__0.M()"],
        //    model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());

        //Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void AmbiguousCallOnInterface()
    {
        var src = """
I2.M();

interface I<T>
{
    public static void M() { }
}

interface I2 : I<int>, I<string> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE consider improving the symbols in this error message
        comp.VerifyEmitDiagnostics(
            // (1,4): error CS0121: The call is ambiguous between the following methods or properties: 'I<T>.M()' and 'I<T>.M()'
            // I2.M();
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("I<T>.M()", "I<T>.M()").WithLocation(1, 4));
    }

    [Fact]
    public void AmbiguousCallOnInterface_Generic()
    {
        var src = """
I2.M<int>();

interface I<T>
{
    public static void M<U>() { }
}

interface I2 : I<int>, I<string> { }
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,4): error CS0121: The call is ambiguous between the following methods or properties: 'I<T>.M<U>()' and 'I<T>.M<U>()'
            // I2.M<int>();
            Diagnostic(ErrorCode.ERR_AmbigCall, "M<int>").WithArguments("I<T>.M<U>()", "I<T>.M<U>()").WithLocation(1, 4));
    }

    [Fact]
    public void OmittedTypeArguments()
    {
        var src = """
object.P<int>;
object.P<>;

static class E
{
    extension(object)
    {
        public static int P => 42;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            // object.P<int>;
            Diagnostic(ErrorCode.ERR_IllegalStatement, "object.P<int>").WithLocation(1, 1),
            // (1,8): error CS0117: 'object' does not contain a definition for 'P'
            // object.P<int>;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "P<int>").WithArguments("object", "P").WithLocation(1, 8),
            // (2,1): error CS8389: Omitting the type argument is not allowed in the current context
            // object.P<>;
            Diagnostic(ErrorCode.ERR_OmittedTypeArgument, "object.P<>").WithLocation(2, 1),
            // (2,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            // object.P<>;
            Diagnostic(ErrorCode.ERR_IllegalStatement, "object.P<>").WithLocation(2, 1),
            // (2,8): error CS0117: 'object' does not contain a definition for 'P'
            // object.P<>;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "P<>").WithArguments("object", "P").WithLocation(2, 8));
    }

    [Fact(Skip = "PROTOTYPE: crash when binding foreach")]
    public void ExtensionMemberLookup_PatternBased_ForEach_NoMethod()
    {
        var src = """
foreach (var x in new C())
{
    System.Console.Write(x);
    break;
}

class C { }
class D { }

static class E
{
    extension(C c)
    {
        public D GetEnumerator() => new D();
    }
    extension(D d)
    {
        public bool MoveNext() => true;
        public int Current => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "42");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).MoveNextMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).CurrentProperty);
    }

    [Fact(Skip = "PROTOTYPE: crash when binding foreach")]
    public void ExtensionMemberLookup_PatternBased_ForEach_NoApplicableMethod()
    {
        var src = """
foreach (var x in new C())
{
    System.Console.Write(x);
    break;
}

class C
{
    public void GetEnumerator(int notApplicable) { } // not applicable
}
class D { }

static class E
{
    extension(C c)
    {
        public D GetEnumerator() => new D();
    }
    extension(D d)
    {
        public bool MoveNext() => true;
        public int Current => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).MoveNextMethod);
        Assert.Null(model.GetForEachStatementInfo(loop).CurrentProperty);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_ForEach_WrongArity()
    {
        var src = """
using System.Collections;

foreach (var x in new C()) { }

class C { }

static class E
{
    extension(C c)
    {
        public IEnumerator GetEnumerator<T>() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,19): error CS0411: The type arguments for method 'E.extension.GetEnumerator<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            // foreach (var x in new C()) { }
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "new C()").WithArguments("E.extension.GetEnumerator<T>()").WithLocation(3, 19),
            // (3,19): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
            // foreach (var x in new C()) { }
            Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(3, 19));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_ForEach_NonInvocable()
    {
        var src = """
using System.Collections;

foreach (var x in new C()) { }

class C { }

static class E
{
    extension(C c)
    {
        public IEnumerator GetEnumerator => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,19): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
            // foreach (var x in new C()) { }
            Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new C()").WithArguments("C", "GetEnumerator").WithLocation(3, 19));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var loop = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        Assert.Null(model.GetForEachStatementInfo(loop).GetEnumeratorMethod);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Deconstruct_NoMethod()
    {
        var src = """
var (x, y) = new C();
System.Console.Write((x, y));

class C { }

static class E
{
    extension(C c)
    {
        public void Deconstruct(out int i, out int j) { i = 42; j = 43; }
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE confirm when spec'ing pattern-based deconstruction
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void E.<>E__0.Deconstruct(out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Deconstruct_FallbackToExtensionMethod()
    {
        // If the method from the extension type is not applicable, we fall back
        // to a Deconstruct extension method
        var src = """
var (x, y) = new C();
System.Console.Write((x, y));

public class C { }

static class E
{
    extension(C c)
    {
        public void Deconstruct(int inapplicable) => throw null;
    }
}

public static class E2
{
    public static void Deconstruct(this C c, out int i, out int j) { i = 42; j = 43; }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE confirm when spec'ing pattern-based deconstruction
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void E2.Deconstruct(this C c, out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Deconstruct_DelegateTypeProperty()
    {
        var src = """
var (x, y) = new C();

class C { }

delegate void D(out int i, out int j);

static class E
{
    extension(C c)
    {
        public D Deconstruct => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE revisit pattern-based deconstruction
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void D.Invoke(out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [Fact(Skip = "PROTOTYPE Asserts in BindDynamicInvocation")]
    public void ExtensionMemberLookup_PatternBased_Deconstruct_DynamicProperty()
    {
        var src = """
var (x, y) = new C();

class C { }

static class E
{
    extension(C c)
    {
        public dynamic Deconstruct => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE revisit pattern-based deconstruction
        comp.VerifyEmitDiagnostics(
            // (1,6): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
            // var (x, y) = new C();
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(1, 6),
            // (1,9): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
            // var (x, y) = new C();
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(1, 9),
            // (1,14): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 2 out parameters and a void return type.
            // var (x, y) = new C();
            Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(1, 14)
            );
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Null(model.GetDeconstructionInfo(deconstruction).Method);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Deconstruct_NoApplicableMethod()
    {
        var src = """
var (x, y) = new C();
System.Console.Write((x, y));

class C
{
    public void Deconstruct() { } // not applicable
}

static class E
{
    extension(C c)
    {
        public void Deconstruct(out int i, out int j) { i = 42; j = 43; }
    }
}
""";
        // PROTOTYPE confirm when spec'ing pattern-based deconstruction
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "(42, 43)");

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var deconstruction = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First();

        Assert.Equal("void E.<>E__0.Deconstruct(out System.Int32 i, out System.Int32 j)",
            model.GetDeconstructionInfo(deconstruction).Method.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Dispose_Async_NoMethod()
    {
        var src = """
using System.Threading.Tasks;

/*<bind>*/
await using var x = new C();
/*</bind>*/

class C { }

static class E
{
    extension(C c)
    {
        public async Task DisposeAsync()
        {
            System.Console.Write("RAN");
            await Task.Yield();
        }
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE confirm when spec'ing pattern-based disposal
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "RAN");

        string expectedOperationTree = """
IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task E.<>E__0.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
DeclarationGroup:
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x = new C()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = new C()')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                  Arguments(0)
                  Initializer:
                    null
      Initializer:
        null
""";
        var expectedDiagnostics = DiagnosticDescription.None;

        VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(src, expectedOperationTree, expectedDiagnostics);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Dispose_Async_DelegateTypeProperty()
    {
        var src = """
using System.Threading.Tasks;

await using var x = new C();

class C { }

static class E
{
    extension(C c)
    {
        public System.Func<Task> DisposeAsync => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        // PROTOTYPE(instance) confirm when spec'ing pattern-based disposal
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Dispose_Async_NoApplicableMethod()
    {
        var src = """
using System.Threading.Tasks;

/*<bind>*/
await using var x = new C();
/*</bind>*/

class C
{
    public Task DisposeAsync(int notApplicable) => throw null; // not applicable
}

static class E
{
    extension(C c)
    {
        public async Task DisposeAsync()
        {
            System.Console.Write("RAN");
            await Task.Yield();
        }
    }
}
""";
        // PROTOTYPE confirm when spec'ing pattern-based disposal
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "RAN");

        // PROTOTYPE verify IOperation
    }

    [ConditionalFact(typeof(NoUsedAssembliesValidation))] // PROTOTYPE metadata is undone
    public void ExtensionMemberLookup_PatternBased_Dispose_RefStruct()
    {
        var src = """
using var x = new S();

ref struct S { }

static class E
{
    extension(S s)
    {
        public void Dispose() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Fixed_NoMethod()
    {
        var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

class Fixable { }

static class E
{
    extension(Fixable f)
    {
        public ref int GetPinnableReference() { return ref (new int[] { 1, 2, 3 })[0]; }
    }
}
";
        var comp = CreateCompilation(text, options: TestOptions.UnsafeReleaseExe);
        // PROTOTYPE confirm when spec'ing pattern-based fixed
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Fixed_NoMethod_DelegateTypeProperty()
    {
        var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

class Fixable { }

delegate ref int MyDelegate();

static class E
{
    extension(Fixable f)
    {
        public MyDelegate GetPinnableReference => throw null;
    }
}
";
        var comp = CreateCompilation(text, options: TestOptions.UnsafeReleaseExe);
        // PROTOTYPE confirm when spec'ing pattern-based fixed
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Fixed_NoApplicableMethod()
    {
        var src = """
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
            System.Console.WriteLine(p[1]);
        }
    }
}

class Fixable
{
    public ref int GetPinnableReference(int notApplicable) => throw null; // not applicable
}

static class E
{
    extension(Fixable f)
    {
        public ref int GetPinnableReference() { return ref (new int[] { 1, 2, 3 })[0]; }
    }
}
""";

        // PROTOTYPE confirm when spec'ing pattern-based fixed
        var comp = CreateCompilation(src, options: TestOptions.UnsafeReleaseExe);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        // PROTOTYPE verify IOperation
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Fixed_Static()
    {
        var text = @"
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable())
        {
        }
    }
}

class Fixable { }

static class E
{
    extension(Fixable f)
    {
        public static ref int GetPinnableReference() => throw null;
    }
}
";

        var comp = CreateCompilation(text, options: TestOptions.UnsafeReleaseExe);
        // PROTOTYPE confirm when spec'ing pattern-based fixed
        comp.VerifyEmitDiagnostics(
            // (6,25): error CS0176: Member 'E.extension.GetPinnableReference()' cannot be accessed with an instance reference; qualify it with a type name instead
            //         fixed (int* p = new Fixable())
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "new Fixable()").WithArguments("E.extension.GetPinnableReference()").WithLocation(6, 25),
            // (6,25): error CS8385: The given expression cannot be used in a fixed statement
            //         fixed (int* p = new Fixable())
            Diagnostic(ErrorCode.ERR_ExprCannotBeFixed, "new Fixable()").WithLocation(6, 25));
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Await_ExtensionIsCompleted()
    {
        var text = @"
using System;
using System.Runtime.CompilerServices;

int i = await new C();
System.Console.Write(i);

class C
{
    public D GetAwaiter() => new D();
}

class D : INotifyCompletion
{
    public int GetResult() => 42;
    public void OnCompleted(Action continuation) => throw null;
}

static class E
{
    extension(D d)
    {
        public bool IsCompleted => true;
    }
}
";

        // PROTOTYPE confirm when spec'ing pattern-based await
        var comp = CreateCompilation(text);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Await_ExtensionGetAwaiter()
    {
        var text = @"
using System;
using System.Runtime.CompilerServices;

int i = await new C();
System.Console.Write(i);

class C
{
}

class D : INotifyCompletion
{
    public int GetResult() => 42;
    public void OnCompleted(Action continuation) => throw null;
    public bool IsCompleted => true;
}

static class E
{
    extension(C c)
    {
        public D GetAwaiter() => new D();
    }
}
";

        // PROTOTYPE confirm when spec'ing pattern-based await
        var comp = CreateCompilation(text);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_Await_ExtensionGetResult()
    {
        var text = @"
using System;
using System.Runtime.CompilerServices;

int i = await new C();
System.Console.Write(i);

class C
{
    public D GetAwaiter() => new D();
}

class D : INotifyCompletion
{
    public void OnCompleted(Action continuation) => throw null;
    public bool IsCompleted => true;
}

static class E
{
    extension(D d)
    {
        public int GetResult() => 42;
    }
}
";

        // PROTOTYPE confirm when spec'ing pattern-based await
        var comp = CreateCompilation(text);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_IndexIndexer_NoLength()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c[^1];
/*</bind>*/

class C
{
    public int this[int i]
    {
        get { System.Console.Write("indexer "); return 0; }
    }
}

static class E
{
    extension(C c)
    {
        public int Length
        {
            get { System.Console.Write("length "); return 42; }
        }
    }
}
""";
        DiagnosticDescription[] expectedDiagnostics = [
            // (4,7): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
            // _ = c[^1];
            Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(4, 7)];

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit as part of "implicit indexer access" section
        comp.VerifyEmitDiagnostics(expectedDiagnostics);
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "length indexer");

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '_ = c[^1]')
Left:
  IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
Right:
  IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'c[^1]')
    Children(2):
        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
        IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index, IsInvalid) (Syntax: '^1')
          Operand:
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_RangeIndexer_NoMethod()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c[1..^1];
/*</bind>*/

class C { }

static class E
{
    extension(C c)
    {
        public int Slice(int i, int j) { System.Console.Write("slice "); return 0; }

        public int Length
        {
            get { System.Console.Write("length "); return 42; }
        }
    }
}
""";

        DiagnosticDescription[] expectedDiagnostics = [
            // (4,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[1..^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[1..^1]").WithArguments("C").WithLocation(4, 5)];

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE revisit as part of "implicit indexer access" section
        comp.VerifyEmitDiagnostics(expectedDiagnostics);
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "length slice");

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '_ = c[1..^1]')
Left:
  IDiscardOperation (Symbol: ? _) (OperationKind.Discard, Type: ?) (Syntax: '_')
Right:
  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'c[1..^1]')
    Children(2):
        IRangeOperation (OperationKind.Range, Type: System.Range, IsInvalid) (Syntax: '1..^1')
          LeftOperand:
            IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Index System.Index.op_Implicit(System.Int32 value)) (OperationKind.Conversion, Type: System.Index, IsInvalid, IsImplicit) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Index System.Index.op_Implicit(System.Int32 value))
              Operand:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          RightOperand:
            IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index, IsInvalid) (Syntax: '^1')
              Operand:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
""";

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Net70);
    }

    [Fact]
    public void ExtensionMemberLookup_PatternBased_RangeIndexer_NoApplicableMethod()
    {
        var src = """
var c = new C();

/*<bind>*/
_ = c[1..^1];
/*</bind>*/

class C
{
    public int Slice(int notApplicable) => throw null; // not applicable
}

static class E
{
    extension(C c)
    {
        public int Slice(int i, int j) { System.Console.Write("slice "); return 0; }

        public int Length
        {
            get { System.Console.Write("length "); return 42; }
        }
    }
}
""";

        // PROTOTYPE revisit as part of "implicit indexer access" section
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
            // _ = c[1..^1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[1..^1]").WithArguments("C").WithLocation(4, 5));
    }

    [Fact]
    public void ExtensionMemberLookup_Patterns()
    {
        var src = """
var c = new C();

_ = c is { Property: 42 };

class C { }

static class E
{
    extension(C c)
    {
        public int Property
        {
            get { System.Console.Write("property"); return 42; }
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "property");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var nameColon = GetSyntax<NameColonSyntax>(tree, "Property:");
        Assert.Equal("System.Int32 E.<>E__0.Property { get; }", model.GetSymbolInfo(nameColon.Name).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_Patterns_ExtendedPropertyPattern()
    {
        var src = """
var c = new C();

_ = c is { Property.Property2: 43 };

class C { }

static class E1
{
    extension(C c)
    {
        public int Property { get { System.Console.Write("property "); return 42; } }
    }
}

static class E2
{
    extension(int i)
    {
        public int Property2 { get { System.Console.Write("property2"); return 43; } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "property property2");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var expressionColon = GetSyntax<ExpressionColonSyntax>(tree, "Property.Property2:");
        Assert.Equal("System.Int32 E2.<>E__0.Property2 { get; }", model.GetSymbolInfo(expressionColon.Expression).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_Patterns_ListPattern_NoInstanceLength()
    {
        var src = """
System.Console.Write(new C() is ["hi"]);

class C
{
    public string this[System.Index i]
    {
        get { System.Console.Write("indexer "); return "hi"; }
    }
}

static class E
{
    extension(C c)
    {
        public int Length
        {
            get { System.Console.Write("length "); return 42; }
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        // PROTOTYPE confirm that we want extensions to contribute to list-patterns
        comp.VerifyEmitDiagnostics(
            // (1,33): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
            // System.Console.Write(new C() is ["hi"]);
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, @"[""hi""]").WithArguments("C").WithLocation(1, 33)
            );
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "length indexer");
    }

    [ConditionalFact(typeof(NoUsedAssembliesValidation))] // PROTOTYPE metadata is undone
    public void ExtensionMemberLookup_ObjectInitializer()
    {
        var src = """
/*<bind>*/
_ = new C() { Property = 42 };
/*</bind>*/

class C { }

static class E
{
    extension(C c)
    {
        public int Property { set { System.Console.Write("property"); } }
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "property");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "Property = 42");
        Assert.Equal("System.Int32 E.<>E__0.Property { set; }", model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString());
    }

    [ConditionalFact(typeof(NoUsedAssembliesValidation))] // PROTOTYPE metadata is undone
    public void ExtensionMemberLookup_With()
    {
        var src = """
/*<bind>*/
_ = new S() with { Property = 42 };
/*</bind>*/

struct S { }

static class E
{
    extension(S s)
    {
        public int Property { set { System.Console.Write("property"); } }
    }
}
""";

        var comp = CreateCompilation(src);
        // PROTOTYPE need to decide whether extensions apply here
        comp.VerifyDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "property");

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var assignment = GetSyntax<AssignmentExpressionSyntax>(tree, "Property = 42");
        Assert.Equal("System.Int32 E.<>E__0.Property { set; }", model.GetSymbolInfo(assignment.Left).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void ExtensionMemberLookup_CollectionInitializer_NoMethod()
    {
        var src = """
using System.Collections;
using System.Collections.Generic;

/*<bind>*/
_ = new C() { 42 };
/*</bind>*/

class C : IEnumerable<int>, IEnumerable
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

static class E
{
    extension(C c)
    {
        public void Add(int i) { System.Console.Write("add"); }
    }
}
""";

        var comp = CreateCompilation(src);
        // PROTOTYPE confirm when spec'ing pattern-based collection initializer
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "add");

        string expectedOperationTree = """
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '_ = new C() { 42 }')
Left:
  IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
Right:
  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { 42 }')
    Arguments(0)
    Initializer:
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ 42 }')
        Initializers(1):
            IInvocationOperation ( void E.<>E__0.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '42')
              Instance Receiver:
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '42')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""";
        var expectedDiagnostics = DiagnosticDescription.None;

        VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(src, expectedOperationTree, expectedDiagnostics);
    }

    [Fact]
    public void ExtensionMemberLookup_CollectionInitializer_NoApplicableMethod()
    {
        var src = """
using System.Collections;
using System.Collections.Generic;

/*<bind>*/
_ = new C() { 42 };
/*</bind>*/

class C : IEnumerable<int>, IEnumerable
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add(string notApplicable) => throw null;
}

static class E
{
    extension(object o)
    {
        public void Add(int i) { System.Console.Write("add"); }
    }
}
""";

        // PROTOTYPE confirm when spec'ing pattern-based collection initializer
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "add");
    }

    [Fact]
    public void ExtensionMemberLookup_Query_NoMethod()
    {
        var src = """
/*<bind>*/
string query = from x in new C() select x;
/*</bind>*/

System.Console.Write(query);

class C { }

static class E
{
    extension(C c)
    {
        public string Select(System.Func<C, C> selector) => "hello";
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "hello");

        string expectedOperationTree = """
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'string quer ... ) select x;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'string quer ... () select x')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.String query) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'query = fro ... () select x')
          Initializer:
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= from x in ... () select x')
              ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.String) (Syntax: 'from x in n ... () select x')
                Expression:
                  IInvocationOperation ( System.String E.<>E__0.Select(System.Func<C, C> selector)) (OperationKind.Invocation, Type: System.String, IsImplicit) (Syntax: 'select x')
                    Instance Receiver:
                      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                        Arguments(0)
                        Initializer:
                          null
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<C, C>, IsImplicit) (Syntax: 'x')
                            Target:
                              IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
                                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                                    ReturnedValue:
                                      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
""";

        VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(src, expectedOperationTree, DiagnosticDescription.None);
    }

    [Fact]
    public void ExtensionMemberLookup_Query_NoApplicableMethod()
    {
        var src = """
/*<bind>*/
string query = from x in new C() select x;
/*</bind>*/

System.Console.Write(query);

class C
{
    public string Select(int notApplicable) => throw null; // not applicable
}

static class E
{
    extension(C c)
    {
        public string Select(System.Func<C, C> selector) => "hello";
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        //CompileAndVerify(comp, expectedOutput: "hello");

        // PROTOTYPE verify IOperation
    }

    [Fact]
    public void ExtensionMemberLookup_Invocation_ZeroArityMatchesAny()
    {
        var source = $$"""
object.Method("");
object.Method<string>("");

static class E
{
    extension(object)
    {
        public static void Method(int i) => throw null;
        public static void Method<T>(T t) { System.Console.Write("Method "); }
        public static void Method<T1, T2>(T1 t1, T2 t2) => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
        // var verifier = CompileAndVerify(comp, expectedOutput: "Method Method Method Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, """object.Method("")""");
        Assert.Equal("void E.<>E__0.Method<System.String>(System.String t)", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        Assert.Empty(model.GetMemberGroup(invocation)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void StaticPropertyAccess_ZeroArityMatchesAny()
    {
        var source = """
int i = object.P;

static class E1
{
    extension(object)
    {
        public static int P => 42;
    }
}

static class E2
{
    extension(object)
    {
        public static void P<T>() => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS0229: Ambiguity between 'E2.extension.P<T>()' and 'E1.extension.P'
            // int i = object.P;
            Diagnostic(ErrorCode.ERR_AmbigMember, "P").WithArguments("E2.extension.P<T>()", "E1.extension.P").WithLocation(1, 16));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.P");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal(["void E2.<>E__0.P<T>()", "System.Int32 E1.<>E__0.P { get; }"], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());
    }

    [Fact]
    public void StaticPropertyAccess_NonZeroArity()
    {
        var source = """
int i = object.P<int>;

static class E1
{
    extension(object)
    {
        public static int P => 42;
    }
}

static class E2
{
    extension(object)
    {
        public static void P<T>() => throw null;
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,16): error CS0428: Cannot convert method group 'P' to non-delegate type 'int'. Did you intend to invoke the method?
            // int i = object.P<int>;
            Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "P<int>").WithArguments("P", "int").WithLocation(1, 16));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.P<int>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void StaticMethodAccess_NonZeroArity()
    {
        var source = """
object.M<int>();

static class E1
{
    extension(object)
    {
        public static void M() { }
        public static void M<T1, T2>() { }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,8): error CS0117: 'object' does not contain a definition for 'M'
            // object.M<int>();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M<int>").WithArguments("object", "M").WithLocation(1, 8));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M<int>");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Equal([], model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings()); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void AddressOf_Simple()
    {
        var src = """
unsafe class C
{
    static void M1()
    {
        delegate*<string, object, void> ptr = &C.M;
    }
}

static class E
{
    extension(C)
    {
        public static void M(string s, object o) {}
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Equal("void E.<>E__0.M(System.String s, System.Object o)", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void AddressOf_AmbiguousBestMethod()
    {
        var src = """
unsafe class C
{
    static void M1()
    {
        delegate*<string, string, void> ptr = &C.M;
    }
}

static class E
{
    extension(C)
    {
        public static void M(string s, object o) {}
        public static void M(object o, string s) {}
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (5,48): error CS0121: The call is ambiguous between the following methods or properties: 'E.extension.M(string, object)' and 'E.extension.M(object, string)'
            //         delegate*<string, string, void> ptr = &C.M;
            Diagnostic(ErrorCode.ERR_AmbigCall, "C.M").WithArguments("E.extension.M(string, object)", "E.extension.M(object, string)").WithLocation(5, 48));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE need to fix the semantic model
    }

    [Fact]
    public void TwoExtensions_MethodAndProperty()
    {
        var src = """
System.Console.Write(object.M());

static class E1
{
    extension(object)
    {
        public static string M() => throw null;
    }
}

static class E2
{
    extension(object)
    {
        public static System.Func<string> M => null;
    }
}
""";
        var comp = CreateCompilation(src);

        comp.VerifyEmitDiagnostics(
            // (1,29): error CS0229: Ambiguity between 'E1.extension.M()' and 'E2.extension.M'
            // System.Console.Write(object.M());
            Diagnostic(ErrorCode.ERR_AmbigMember, "M").WithArguments("E1.extension.M()", "E2.extension.M").WithLocation(1, 29));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "object.M");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);

        Assert.Equal(["System.String E1.<>E__0.M()", "System.Func<System.String> E2.<>E__0.M { get; }"],
            model.GetSymbolInfo(memberAccess).CandidateSymbols.ToTestDisplayStrings());

        Assert.Empty(model.GetMemberGroup(memberAccess)); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void Nameof_Static_Method()
    {
        var src = """
System.Console.Write(nameof(C.Method));

class C { }

static class E
{
    extension(C)
    {
        public static string Method() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void Nameof_Static_Property()
    {
        var src = """
System.Console.Write(nameof(C.Property));

class C { }

static class E
{
    extension(C)
    {
        public static string Property => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "Property").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Property");
        Assert.Equal("System.String E.<>E__0.Property { get; }", model.GetSymbolInfo(memberAccess).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void Nameof_Static_WrongArityMethod()
    {
        var src = """
System.Console.Write(nameof(C.Method));

class C { }

static class E
{
    extension(C)
    {
        public static string Method<T>() => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "Method").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "C.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol); // PROTOTYPE semantic model is undone
    }

    [Fact]
    public void Nameof_Overloads()
    {
        var src = """
System.Console.Write($"{nameof(object.M)} ");

static class E
{
    extension(object)
    {
        public static void M() { }
        public static void M(int i) { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "M").VerifyDiagnostics();
    }

    [Fact]
    public void Nameof_SimpleName()
    {
        var src = """
class C
{
    void M()
    {
        _ = nameof(Method);
        _ = nameof(Property);
    }
}

static class E
{
    extension(object)
    {
        public static void Method() { }
        public static int Property => 0;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,20): error CS0103: The name 'Method' does not exist in the current context
            //         _ = nameof(Method);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Method").WithArguments("Method").WithLocation(5, 20),
            // (6,20): error CS0103: The name 'Property' does not exist in the current context
            //         _ = nameof(Property);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Property").WithArguments("Property").WithLocation(6, 20));
    }

    [Fact]
    public void Nameof_NoParameter()
    {
        var src = """
class C
{
    void M()
    {
        System.Console.Write(nameof());
    }
}

static class E
{
    extension(C c)
    {
        public string nameof() => throw null;
    }
}
""";

        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (5,30): error CS0103: The name 'nameof' does not exist in the current context
            //         System.Console.Write(nameof());
            Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(5, 30));
    }

    [Fact]
    public void Nameof_SingleParameter()
    {
        var src = """
class C
{
    public static void Main()
    {
        string x = "";
        System.Console.Write(nameof(x));
    }
}

static class E
{
    extension(C c)
    {
        public string nameof(string s) => throw null;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "x").VerifyDiagnostics();
    }

    [Fact]
    public void StaticMethodInvocation_TypeParameter_InNameof()
    {
        var source = """
public static class C
{
    static void M<T>()
    {
        _ = nameof(T.Method);
    }

    extension<T>(T)
    {
        public static void Method() { }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // 0.cs(5,20): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         _ = nameof(T.Method);
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(5, 20));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "T.Method");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void SymbolInfoForMethodGroup03()
    {
        var source = """
public class A { }

static class E
{
    extension(A a)
    {
        public string Extension() { return null; }
    }
}
public class Program
{
    public static void Main(string[] args)
    {
        A a = null;
        _ = nameof(a.Extension);
    }
}
""";
        var comp = CreateCompilation(source);
        // PROTOTYPE should we produce ERR_NameofExtensionMethod (Extension method groups are not allowed as an argument to 'nameof') or something similar?
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var memberAccess = GetSyntax<MemberAccessExpressionSyntax>(tree, "a.Extension");
        Assert.Null(model.GetSymbolInfo(memberAccess).Symbol);
        Assert.Empty(model.GetMemberGroup(memberAccess));
    }

    [Fact]
    public void StaticMethodInvocation_PartialStaticClass()
    {
        var source = """
object.M();
object.M2();

public static partial class C
{
    extension(object)
    {
        public static void M() { }
    }
}

public static partial class C
{
    extension(object)
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
        // PROTOTYPE metadata is undone
    }

    [Fact]
    public void StaticMethodInvocation_TupleTypeReceiver()
    {
        var src = """
(string, string).M();
(int a, int b).M();
""";
        // PROTOTYPE consider parsing this
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,2): error CS1525: Invalid expression term 'string'
            // (string, string).M();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 2),
            // (1,10): error CS1525: Invalid expression term 'string'
            // (string, string).M();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 10),
            // (2,2): error CS8185: A declaration is not allowed in this context.
            // (int a, int b).M();
            Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int a").WithLocation(2, 2),
            // (2,2): error CS0165: Use of unassigned local variable 'a'
            // (int a, int b).M();
            Diagnostic(ErrorCode.ERR_UseDefViolation, "int a").WithArguments("a").WithLocation(2, 2),
            // (2,9): error CS8185: A declaration is not allowed in this context.
            // (int a, int b).M();
            Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int b").WithLocation(2, 9),
            // (2,9): error CS0165: Use of unassigned local variable 'b'
            // (int a, int b).M();
            Diagnostic(ErrorCode.ERR_UseDefViolation, "int b").WithArguments("b").WithLocation(2, 9),
            // (2,16): error CS1061: '(int a, int b)' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type '(int a, int b)' could be found (are you missing a using directive or an assembly reference?)
            // (int a, int b).M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("(int a, int b)", "M").WithLocation(2, 16));
    }

    [Fact]
    public void StaticMethodInvocation_TupleTypeReceiver_02()
    {
        var src = """
((string, string)).M();
((int a, int b)).M();
""";
        // PROTOTYPE consider parsing this
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,3): error CS1525: Invalid expression term 'string'
            // ((string, string)).M();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 3),
            // (1,11): error CS1525: Invalid expression term 'string'
            // ((string, string)).M();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(1, 11),
            // (2,3): error CS8185: A declaration is not allowed in this context.
            // ((int a, int b)).M();
            Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int a").WithLocation(2, 3),
            // (2,3): error CS0165: Use of unassigned local variable 'a'
            // ((int a, int b)).M();
            Diagnostic(ErrorCode.ERR_UseDefViolation, "int a").WithArguments("a").WithLocation(2, 3),
            // (2,10): error CS8185: A declaration is not allowed in this context.
            // ((int a, int b)).M();
            Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int b").WithLocation(2, 10),
            // (2,10): error CS0165: Use of unassigned local variable 'b'
            // ((int a, int b)).M();
            Diagnostic(ErrorCode.ERR_UseDefViolation, "int b").WithArguments("b").WithLocation(2, 10),
            // (2,18): error CS1061: '(int a, int b)' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type '(int a, int b)' could be found (are you missing a using directive or an assembly reference?)
            // ((int a, int b)).M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("(int a, int b)", "M").WithLocation(2, 18));
    }

    [Fact]
    public void StaticMethodInvocation_PointerTypeReceiver()
    {
        var src = """
unsafe class C
{
    void M()
    {
        int*.M();
        delegate*<void>.M();
    }
}
""";
        // PROTOTYPE consider parsing this
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
            // (5,13): error CS1001: Identifier expected
            //         int*.M();
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(5, 13),
            // (5,13): error CS1003: Syntax error, ',' expected
            //         int*.M();
            Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",").WithLocation(5, 13),
            // (5,14): error CS1002: ; expected
            //         int*.M();
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "M").WithLocation(5, 14),
            // (6,17): error CS1514: { expected
            //         delegate*<void>.M();
            Diagnostic(ErrorCode.ERR_LbraceExpected, "*").WithLocation(6, 17),
            // (6,17): warning CS8848: Operator '*' cannot be used here due to precedence. Use parentheses to disambiguate.
            //         delegate*<void>.M();
            Diagnostic(ErrorCode.WRN_PrecedenceInversion, "*").WithArguments("*").WithLocation(6, 17),
            // (6,18): error CS1525: Invalid expression term '<'
            //         delegate*<void>.M();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(6, 18),
            // (6,19): error CS1525: Invalid expression term 'void'
            //         delegate*<void>.M();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "void").WithArguments("void").WithLocation(6, 19),
            // (6,24): error CS1525: Invalid expression term '.'
            //         delegate*<void>.M();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments(".").WithLocation(6, 24));
    }

    [Fact]
    public void StaticMethodInvocation_DynamicTypeReceiver()
    {
        var src = """
dynamic.M();
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,1): error CS0103: The name 'dynamic' does not exist in the current context
            // dynamic.M();
            Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(1, 1));
    }
}
