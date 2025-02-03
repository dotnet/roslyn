// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public class ExtensionsTests : CompilingTestBase
{
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
        // Type parameter same as out type parameter
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
public static class Extensions<T>
{
    extension(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.True(symbol.IsGenericType);
        var member = symbol.ContainingType.GetMembers().Single();
        Assert.Equal("Extensions<T>.<>E__0", member.ToTestDisplayString());
    }

    [Fact]
    public void BadContainer_TopLevel()
    {
        var src = """
extension(object) { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS1106: Extensions must be declared in a top-level, non-generic, static class
            // extension(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(1, 1));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var extension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Single();

        var symbol = model.GetDeclaredSymbol(extension);
        Assert.Null(symbol.ContainingType);
        Assert.Equal("<>E__0", symbol.ToTestDisplayString());
    }

    [Fact]
    public void BadContainer_Nested()
    {
        var src = """
public static class Extensions
{
    public static class Extensions2
    {
        extension(object) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,9): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //         extension(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 9));

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
public static class Extensions
{
    extension(object)
    {
        extension(string) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,9): error CS9501: Extension declarations can include only methods or properties
            //         extension(string) { }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "extension").WithLocation(5, 9));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var nestedExtension = tree.GetRoot().DescendantNodes().OfType<ExtensionDeclarationSyntax>().Last();

        var nestedExtensionSymbol = model.GetDeclaredSymbol(nestedExtension);
        AssertExtensionDeclaration(nestedExtensionSymbol);
        Assert.Equal("Extensions.<>E__0", nestedExtensionSymbol.ContainingType.ToTestDisplayString());
        var member = nestedExtensionSymbol.ContainingType.GetMembers().Single();
        Assert.Equal("Extensions.<>E__0.<>E__0", member.ToTestDisplayString());
    }

    [Fact]
    public void BadContainer_TypeKind()
    {
        var src = """
public static struct Extensions
{
    extension(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,22): error CS0106: The modifier 'static' is not valid for this item
            // public static struct Extensions
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "Extensions").WithArguments("static").WithLocation(1, 22),
            // (3,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));
    }

    [Fact]
    public void BadContainer_NotStatic()
    {
        var src = """
public class Extensions
{
    extension(object) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9502: Extensions must be declared in a top-level, non-generic, static class
            //     extension(object) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));
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
public static class Extensions
{
    extension(object) { int field = 0; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,29): error CS9501: Extension declarations can include only methods or properties
            //     extension(object) { int field = 0; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "field").WithLocation(3, 29),
            // (3,29): warning CS0169: The field 'Extensions.extension.field' is never used
            //     extension(object) { int field = 0; }
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("Extensions.extension.field").WithLocation(3, 29));

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
            // error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C<T>'
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied).WithArguments("C<T>", "T", "T").WithLocation(1, 1));

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
        Assert.True(parameter.HasExplicitDefaultValue);
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
public static class Extensions
{
    extension(__arglist) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist) { }
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15));
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
}
