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
public class ExtensionTests : CompilingTestBase
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
            // Method begins at RVA 0x2069
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        void '<Extension>M' (
            object o
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method Extensions::'<Extension>M'
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
            // Method begins at RVA 0x2069
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        void '<StaticExtension>M' () cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method Extensions::'<StaticExtension>M'
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
            // Method begins at RVA 0x206d
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::get_Property
        .method private hidebysig specialname 
            instance void set_Property (
                int32 'value'
            ) cil managed 
        {
            // Method begins at RVA 0x206d
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::set_Property
        // Properties
        .property instance int32 Property()
        {
            .get instance int32 Extensions/'<>E__0'::get_Property()
            .set instance void Extensions/'<>E__0'::set_Property(int32)
        }
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        int32 '<Extension>get_Property' (
            object o
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 3 (0x3)
        .maxstack 8
        IL_0000: ldc.i4.s 42
        IL_0002: ret
    } // end of method Extensions::'<Extension>get_Property'
    .method private hidebysig specialname static 
        void '<Extension>set_Property' (
            object o,
            int32 'value'
        ) cil managed 
    {
        // Method begins at RVA 0x206b
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method Extensions::'<Extension>set_Property'
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
            // Method begins at RVA 0x206d
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::get_Property
        .method private hidebysig specialname static 
            void set_Property (
                int32 'value'
            ) cil managed 
        {
            // Method begins at RVA 0x206d
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::set_Property
        // Properties
        .property int32 Property()
        {
            .get int32 Extensions/'<>E__0'::get_Property()
            .set void Extensions/'<>E__0'::set_Property(int32)
        }
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        int32 '<StaticExtension>get_Property' () cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 3 (0x3)
        .maxstack 8
        IL_0000: ldc.i4.s 42
        IL_0002: ret
    } // end of method Extensions::'<StaticExtension>get_Property'
    .method private hidebysig specialname static 
        void '<StaticExtension>set_Property' (
            int32 'value'
        ) cil managed 
    {
        // Method begins at RVA 0x206b
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method Extensions::'<StaticExtension>set_Property'
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
            // Method begins at RVA 0x206d
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::get_Item
        .method private hidebysig specialname 
            instance void set_Item (
                int32 i,
                int32 'value'
            ) cil managed 
        {
            // Method begins at RVA 0x206d
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
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
    // Methods
    .method private hidebysig specialname static 
        int32 '<Extension>get_Item' (
            object o,
            int32 i
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 3 (0x3)
        .maxstack 8
        IL_0000: ldc.i4.s 42
        IL_0002: ret
    } // end of method Extensions::'<Extension>get_Item'
    .method private hidebysig specialname static 
        void '<Extension>set_Item' (
            object o,
            int32 i,
            int32 'value'
        ) cil managed 
    {
        // Method begins at RVA 0x206b
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method Extensions::'<Extension>set_Item'
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
public static class Extensions
{
    extension(__arglist)
    {
        void M(){}
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist)
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15));

        MethodSymbol implementation = comp.GetTypeByMetadataName("Extensions").GetMembers().OfType<MethodSymbol>().Single();
        Assert.Equal(0, implementation.ParameterCount);
        Assert.Equal("void Extensions.<Extension>M()", implementation.ToTestDisplayString());
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
        verifier.VerifyIL("Extensions.<>E__0.M()", """
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  throw
}
""");

        verifier.VerifyIL("Extensions.<Extension>M", """
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
    public void ReceiverNotInScopeInStaticMember()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        static object M1() => o;
        static object M2() { return o; }
        static object P1 => o;
        static object P2 { get { return o; } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,31): error CS0103: The name 'o' does not exist in the current context
            //         static object M1() => o;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(5, 31),
            // (6,37): error CS0103: The name 'o' does not exist in the current context
            //         static object M2() { return o; }
            Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(6, 37),
            // (7,29): error CS0103: The name 'o' does not exist in the current context
            //         static object P1 => o;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(7, 29),
            // (8,41): error CS0103: The name 'o' does not exist in the current context
            //         static object P2 { get { return o; } }
            Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(8, 41)
            );
    }

    [Fact]
    public void Implementation_InstanceMethod_01()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        void M(string s)
        {
            o.ToString();
            _ = s.Length;
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance void M (
                string s
            ) cil managed 
        {
            // Method begins at RVA 0x2077
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        void '<Extension>M' (
            object o,
            string s
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 15 (0xf)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0006: pop
        IL_0007: ldarg.1
        IL_0008: callvirt instance int32 [mscorlib]System.String::get_Length()
        IL_000d: pop
        IL_000e: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_02()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        string M(string s) => o + s;
    }
}
""";
        var comp = CreateCompilation(src);

        MethodSymbol implementation = comp.GetTypeByMetadataName("Extensions").GetMembers().OfType<MethodSymbol>().Single();
        Assert.True(implementation.IsStatic);
        Assert.Equal(MethodKind.Ordinary, implementation.MethodKind);
        Assert.Equal(2, implementation.ParameterCount);
        AssertEx.Equal("System.String Extensions.<Extension>M(System.Object o, System.String s)", implementation.ToTestDisplayString());
        Assert.True(implementation.IsImplicitlyDeclared);
        Assert.False(implementation.IsExtensionMethod);
        Assert.True(implementation.HasSpecialName);
        Assert.False(implementation.HasRuntimeSpecialName);

        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance string M (
                string s
            ) cil managed 
        {
            // Method begins at RVA 0x207b
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>M' (
            object o,
            string s
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 19 (0x13)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: brtrue.s IL_0006
        IL_0003: ldnull
        IL_0004: br.s IL_000c
        IL_0006: ldarg.0
        IL_0007: callvirt instance string [mscorlib]System.Object::ToString()
        IL_000c: ldarg.1
        IL_000d: call string [mscorlib]System.String::Concat(string, string)
        IL_0012: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_03_WithLocalFunction()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        string M(string s)
        {
            string local() => o + s;
            return local();
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance string M (
                string s
            ) cil managed 
        {
            // Method begins at RVA 0x20ab
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
        extends [mscorlib]System.ValueType
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public object o
        .field public string s
    } // end of class <>c__DisplayClass0_0
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>M' (
            object o,
            string s
        ) cil managed 
    {
        // Method begins at RVA 0x2068
        // Code size 24 (0x18)
        .maxstack 2
        .locals init (
            [0] valuetype Extensions/'<>c__DisplayClass0_0'
        )
        IL_0000: ldloca.s 0
        IL_0002: ldarg.0
        IL_0003: stfld object Extensions/'<>c__DisplayClass0_0'::o
        IL_0008: ldloca.s 0
        IL_000a: ldarg.1
        IL_000b: stfld string Extensions/'<>c__DisplayClass0_0'::s
        IL_0010: ldloca.s 0
        IL_0012: call string Extensions::'<<Extension>M>b__0_0'(valuetype Extensions/'<>c__DisplayClass0_0'&)
        IL_0017: ret
    } // end of method Extensions::'<Extension>M'
    .method assembly hidebysig static 
        string '<<Extension>M>b__0_0' (
            valuetype Extensions/'<>c__DisplayClass0_0'& ''
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x208c
        // Code size 30 (0x1e)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld object Extensions/'<>c__DisplayClass0_0'::o
        IL_0006: dup
        IL_0007: brtrue.s IL_000d
        IL_0009: pop
        IL_000a: ldnull
        IL_000b: br.s IL_0012
        IL_000d: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0012: ldarg.0
        IL_0013: ldfld string Extensions/'<>c__DisplayClass0_0'::s
        IL_0018: call string [mscorlib]System.String::Concat(string, string)
        IL_001d: ret
    } // end of method Extensions::'<<Extension>M>b__0_0'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_04_WithLambda()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        string M(string s)
        {
            System.Func<string> local = () => o + s;
            return local();
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance string M (
                string s
            ) cil managed 
        {
            // Method begins at RVA 0x208c
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public object o
        .field public string s
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor () cil managed 
        {
            // Method begins at RVA 0x208f
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method '<>c__DisplayClass0_0'::.ctor
        .method assembly hidebysig 
            instance string '<<Extension>M>b__0' () cil managed 
        {
            // Method begins at RVA 0x2097
            // Code size 30 (0x1e)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld object Extensions/'<>c__DisplayClass0_0'::o
            IL_0006: dup
            IL_0007: brtrue.s IL_000d
            IL_0009: pop
            IL_000a: ldnull
            IL_000b: br.s IL_0012
            IL_000d: callvirt instance string [mscorlib]System.Object::ToString()
            IL_0012: ldarg.0
            IL_0013: ldfld string Extensions/'<>c__DisplayClass0_0'::s
            IL_0018: call string [mscorlib]System.String::Concat(string, string)
            IL_001d: ret
        } // end of method '<>c__DisplayClass0_0'::'<<Extension>M>b__0'
    } // end of class <>c__DisplayClass0_0
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>M' (
            object o,
            string s
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 36 (0x24)
        .maxstack 8
        IL_0000: newobj instance void Extensions/'<>c__DisplayClass0_0'::.ctor()
        IL_0005: dup
        IL_0006: ldarg.0
        IL_0007: stfld object Extensions/'<>c__DisplayClass0_0'::o
        IL_000c: dup
        IL_000d: ldarg.1
        IL_000e: stfld string Extensions/'<>c__DisplayClass0_0'::s
        IL_0013: ldftn instance string Extensions/'<>c__DisplayClass0_0'::'<<Extension>M>b__0'()
        IL_0019: newobj instance void class [mscorlib]System.Func`1<string>::.ctor(object, native int)
        IL_001e: callvirt instance !0 class [mscorlib]System.Func`1<string>::Invoke()
        IL_0023: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_05_Iterator()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        System.Collections.Generic.IEnumerable<string> M(string s)
        {
            yield return o + s;
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", ("""
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance class [mscorlib]System.Collections.Generic.IEnumerable`1<string> M (
                string s
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
                01 00 1d 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
                45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
                00 00
            )
            // Method begins at RVA 0x207e
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<<Extension>M>d__0'
        extends [mscorlib]System.Object
        implements class [mscorlib]System.Collections.Generic.IEnumerable`1<string>,
                   [mscorlib]System.Collections.IEnumerable,
                   class [mscorlib]System.Collections.Generic.IEnumerator`1<string>,

""" +
        (ExecutionConditionUtil.IsMonoOrCoreClr ?
"""
                   [mscorlib]System.Collections.IEnumerator,
                   [mscorlib]System.IDisposable

""" :
"""
                   [mscorlib]System.IDisposable,
                   [mscorlib]System.Collections.IEnumerator

""") +
"""
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field private int32 '<>1__state'
        .field private string '<>2__current'
        .field private int32 '<>l__initialThreadId'
        .field private object o
        .field public object '<>3__o'
        .field private string s
        .field public string '<>3__s'
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                int32 '<>1__state'
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            // Method begins at RVA 0x2081
            // Code size 25 (0x19)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ldarg.0
            IL_0007: ldarg.1
            IL_0008: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_000d: ldarg.0
            IL_000e: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
            IL_0013: stfld int32 Extensions/'<<Extension>M>d__0'::'<>l__initialThreadId'
            IL_0018: ret
        } // end of method '<<Extension>M>d__0'::.ctor
        .method private final hidebysig newslot virtual 
            instance void System.IDisposable.Dispose () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.IDisposable::Dispose()
            // Method begins at RVA 0x209b
            // Code size 9 (0x9)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldc.i4.s -2
            IL_0003: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_0008: ret
        } // end of method '<<Extension>M>d__0'::System.IDisposable.Dispose
        .method private final hidebysig newslot virtual 
            instance bool MoveNext () cil managed 
        {
            .override method instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
            // Method begins at RVA 0x20a8
            // Code size 76 (0x4c)
            .maxstack 3
            .locals init (
                [0] int32
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_0006: stloc.0
            IL_0007: ldloc.0
            IL_0008: brfalse.s IL_0010
            IL_000a: ldloc.0
            IL_000b: ldc.i4.1
            IL_000c: beq.s IL_0043
            IL_000e: ldc.i4.0
            IL_000f: ret
            IL_0010: ldarg.0
            IL_0011: ldc.i4.m1
            IL_0012: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_0017: ldarg.0
            IL_0018: ldarg.0
            IL_0019: ldfld object Extensions/'<<Extension>M>d__0'::o
            IL_001e: dup
            IL_001f: brtrue.s IL_0025
            IL_0021: pop
            IL_0022: ldnull
            IL_0023: br.s IL_002a
            IL_0025: callvirt instance string [mscorlib]System.Object::ToString()
            IL_002a: ldarg.0
            IL_002b: ldfld string Extensions/'<<Extension>M>d__0'::s
            IL_0030: call string [mscorlib]System.String::Concat(string, string)
            IL_0035: stfld string Extensions/'<<Extension>M>d__0'::'<>2__current'
            IL_003a: ldarg.0
            IL_003b: ldc.i4.1
            IL_003c: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_0041: ldc.i4.1
            IL_0042: ret
            IL_0043: ldarg.0
            IL_0044: ldc.i4.m1
            IL_0045: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_004a: ldc.i4.0
            IL_004b: ret
        } // end of method '<<Extension>M>d__0'::MoveNext
        .method private final hidebysig specialname newslot virtual 
            instance string 'System.Collections.Generic.IEnumerator<System.String>.get_Current' () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<string>::get_Current()
            // Method begins at RVA 0x2100
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld string Extensions/'<<Extension>M>d__0'::'<>2__current'
            IL_0006: ret
        } // end of method '<<Extension>M>d__0'::'System.Collections.Generic.IEnumerator<System.String>.get_Current'
        .method private final hidebysig newslot virtual 
            instance void System.Collections.IEnumerator.Reset () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.Collections.IEnumerator::Reset()
            // Method begins at RVA 0x2108
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<<Extension>M>d__0'::System.Collections.IEnumerator.Reset
        .method private final hidebysig specialname newslot virtual 
            instance object System.Collections.IEnumerator.get_Current () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance object [mscorlib]System.Collections.IEnumerator::get_Current()
            // Method begins at RVA 0x2100
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld string Extensions/'<<Extension>M>d__0'::'<>2__current'
            IL_0006: ret
        } // end of method '<<Extension>M>d__0'::System.Collections.IEnumerator.get_Current
        .method private final hidebysig newslot virtual 
            instance class [mscorlib]System.Collections.Generic.IEnumerator`1<string> 'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator' () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<string>::GetEnumerator()
            // Method begins at RVA 0x2110
            // Code size 67 (0x43)
            .maxstack 2
            .locals init (
                [0] class Extensions/'<<Extension>M>d__0'
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_0006: ldc.i4.s -2
            IL_0008: bne.un.s IL_0022
            IL_000a: ldarg.0
            IL_000b: ldfld int32 Extensions/'<<Extension>M>d__0'::'<>l__initialThreadId'
            IL_0010: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
            IL_0015: bne.un.s IL_0022
            IL_0017: ldarg.0
            IL_0018: ldc.i4.0
            IL_0019: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_001e: ldarg.0
            IL_001f: stloc.0
            IL_0020: br.s IL_0029
            IL_0022: ldc.i4.0
            IL_0023: newobj instance void Extensions/'<<Extension>M>d__0'::.ctor(int32)
            IL_0028: stloc.0
            IL_0029: ldloc.0
            IL_002a: ldarg.0
            IL_002b: ldfld object Extensions/'<<Extension>M>d__0'::'<>3__o'
            IL_0030: stfld object Extensions/'<<Extension>M>d__0'::o
            IL_0035: ldloc.0
            IL_0036: ldarg.0
            IL_0037: ldfld string Extensions/'<<Extension>M>d__0'::'<>3__s'
            IL_003c: stfld string Extensions/'<<Extension>M>d__0'::s
            IL_0041: ldloc.0
            IL_0042: ret
        } // end of method '<<Extension>M>d__0'::'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator'
        .method private final hidebysig newslot virtual 
            instance class [mscorlib]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [mscorlib]System.Collections.IEnumerator [mscorlib]System.Collections.IEnumerable::GetEnumerator()
            // Method begins at RVA 0x215f
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance class [mscorlib]System.Collections.Generic.IEnumerator`1<string> Extensions/'<<Extension>M>d__0'::'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator'()
            IL_0006: ret
        } // end of method '<<Extension>M>d__0'::System.Collections.IEnumerable.GetEnumerator
        // Properties
        .property instance string 'System.Collections.Generic.IEnumerator<System.String>.Current'()
        {
            .get instance string Extensions/'<<Extension>M>d__0'::'System.Collections.Generic.IEnumerator<System.String>.get_Current'()
        }
        .property instance object System.Collections.IEnumerator.Current()
        {
            .get instance object Extensions/'<<Extension>M>d__0'::System.Collections.IEnumerator.get_Current()
        }
    } // end of class <<Extension>M>d__0
    // Methods
    .method private hidebysig specialname static 
        class [mscorlib]System.Collections.Generic.IEnumerable`1<string> '<Extension>M' (
            object o,
            string s
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
            01 00 1d 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
            45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
            00 00
        )
        // Method begins at RVA 0x2067
        // Code size 22 (0x16)
        .maxstack 8
        IL_0000: ldc.i4.s -2
        IL_0002: newobj instance void Extensions/'<<Extension>M>d__0'::.ctor(int32)
        IL_0007: dup
        IL_0008: ldarg.0
        IL_0009: stfld object Extensions/'<<Extension>M>d__0'::'<>3__o'
        IL_000e: dup
        IL_000f: ldarg.1
        IL_0010: stfld string Extensions/'<<Extension>M>d__0'::'<>3__s'
        IL_0015: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions

""").Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_06_Async()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        async System.Threading.Tasks.Task<string> M(string s)
        {
            await System.Threading.Tasks.Task.Yield();
            return o + s;
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance class [mscorlib]System.Threading.Tasks.Task`1<string> M (
                string s
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
                01 00 1d 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
                45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
                00 00
            )
            // Method begins at RVA 0x20b3
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<<Extension>M>d__0'
        extends [mscorlib]System.ValueType
        implements [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public int32 '<>1__state'
        .field public valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> '<>t__builder'
        .field public object o
        .field public string s
        .field private valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
        // Methods
        .method private final hidebysig newslot virtual 
            instance void MoveNext () cil managed 
        {
            .override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
            // Method begins at RVA 0x20b8
            // Code size 178 (0xb2)
            .maxstack 3
            .locals init (
                [0] int32,
                [1] string,
                [2] valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
                [3] valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable,
                [4] class [mscorlib]System.Exception
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_0006: stloc.0
            .try
            {
                IL_0007: ldloc.0
                IL_0008: brfalse.s IL_0041
                IL_000a: call valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable [mscorlib]System.Threading.Tasks.Task::Yield()
                IL_000f: stloc.3
                IL_0010: ldloca.s 3
                IL_0012: call instance valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [mscorlib]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
                IL_0017: stloc.2
                IL_0018: ldloca.s 2
                IL_001a: call instance bool [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
                IL_001f: brtrue.s IL_005d
                IL_0021: ldarg.0
                IL_0022: ldc.i4.0
                IL_0023: dup
                IL_0024: stloc.0
                IL_0025: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
                IL_002a: ldarg.0
                IL_002b: ldloc.2
                IL_002c: stfld valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter Extensions/'<<Extension>M>d__0'::'<>u__1'
                IL_0031: ldarg.0
                IL_0032: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<Extension>M>d__0'::'<>t__builder'
                IL_0037: ldloca.s 2
                IL_0039: ldarg.0
                IL_003a: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, valuetype Extensions/'<<Extension>M>d__0'>(!!0&, !!1&)
                IL_003f: leave.s IL_00b1
                IL_0041: ldarg.0
                IL_0042: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter Extensions/'<<Extension>M>d__0'::'<>u__1'
                IL_0047: stloc.2
                IL_0048: ldarg.0
                IL_0049: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter Extensions/'<<Extension>M>d__0'::'<>u__1'
                IL_004e: initobj [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
                IL_0054: ldarg.0
                IL_0055: ldc.i4.m1
                IL_0056: dup
                IL_0057: stloc.0
                IL_0058: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
                IL_005d: ldloca.s 2
                IL_005f: call instance void [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
                IL_0064: ldarg.0
                IL_0065: ldfld object Extensions/'<<Extension>M>d__0'::o
                IL_006a: dup
                IL_006b: brtrue.s IL_0071
                IL_006d: pop
                IL_006e: ldnull
                IL_006f: br.s IL_0076
                IL_0071: callvirt instance string [mscorlib]System.Object::ToString()
                IL_0076: ldarg.0
                IL_0077: ldfld string Extensions/'<<Extension>M>d__0'::s
                IL_007c: call string [mscorlib]System.String::Concat(string, string)
                IL_0081: stloc.1
                IL_0082: leave.s IL_009d
            } // end .try
            catch [mscorlib]System.Exception
            {
                IL_0084: stloc.s 4
                IL_0086: ldarg.0
                IL_0087: ldc.i4.s -2
                IL_0089: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
                IL_008e: ldarg.0
                IL_008f: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<Extension>M>d__0'::'<>t__builder'
                IL_0094: ldloc.s 4
                IL_0096: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetException(class [mscorlib]System.Exception)
                IL_009b: leave.s IL_00b1
            } // end handler
            IL_009d: ldarg.0
            IL_009e: ldc.i4.s -2
            IL_00a0: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
            IL_00a5: ldarg.0
            IL_00a6: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<Extension>M>d__0'::'<>t__builder'
            IL_00ab: ldloc.1
            IL_00ac: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetResult(!0)
            IL_00b1: ret
        } // end of method '<<Extension>M>d__0'::MoveNext
        .method private final hidebysig newslot virtual 
            instance void SetStateMachine (
                class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
            // Method begins at RVA 0x2188
            // Code size 13 (0xd)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<Extension>M>d__0'::'<>t__builder'
            IL_0006: ldarg.1
            IL_0007: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
            IL_000c: ret
        } // end of method '<<Extension>M>d__0'::SetStateMachine
    } // end of class <<Extension>M>d__0
    // Methods
    .method private hidebysig specialname static 
        class [mscorlib]System.Threading.Tasks.Task`1<string> '<Extension>M' (
            object o,
            string s
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
            01 00 1d 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
            45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
            00 00
        )
        // Method begins at RVA 0x2068
        // Code size 63 (0x3f)
        .maxstack 2
        .locals init (
            [0] valuetype Extensions/'<<Extension>M>d__0'
        )
        IL_0000: ldloca.s 0
        IL_0002: call valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::Create()
        IL_0007: stfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<Extension>M>d__0'::'<>t__builder'
        IL_000c: ldloca.s 0
        IL_000e: ldarg.0
        IL_000f: stfld object Extensions/'<<Extension>M>d__0'::o
        IL_0014: ldloca.s 0
        IL_0016: ldarg.1
        IL_0017: stfld string Extensions/'<<Extension>M>d__0'::s
        IL_001c: ldloca.s 0
        IL_001e: ldc.i4.m1
        IL_001f: stfld int32 Extensions/'<<Extension>M>d__0'::'<>1__state'
        IL_0024: ldloca.s 0
        IL_0026: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<Extension>M>d__0'::'<>t__builder'
        IL_002b: ldloca.s 0
        IL_002d: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::Start<valuetype Extensions/'<<Extension>M>d__0'>(!!0&)
        IL_0032: ldloca.s 0
        IL_0034: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<Extension>M>d__0'::'<>t__builder'
        IL_0039: call instance class [mscorlib]System.Threading.Tasks.Task`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::get_Task()
        IL_003e: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_07_Generic()
    {
        var src = """
public static class Extensions
{
    extension<T>(C<T> o)
    {
        string M<U>(T t, U u)
        {
            return o.GetString() + u.ToString() + t.ToString();
        }
    }
}

public class C<T>
{
    public string GetString() => null;
}
""";
        var comp = CreateCompilation(src);

        MethodSymbol implementation = comp.GetTypeByMetadataName("Extensions").GetMembers().OfType<MethodSymbol>().Single();
        Assert.True(implementation.IsStatic);
        Assert.Equal(MethodKind.Ordinary, implementation.MethodKind);
        Assert.Equal(3, implementation.ParameterCount);
        AssertEx.Equal("System.String Extensions.<Extension>M<T, U>(C<T> o, T t, U u)", implementation.ToTestDisplayString());
        Assert.True(implementation.IsImplicitlyDeclared);
        Assert.False(implementation.IsExtensionMethod);
        Assert.True(implementation.HasSpecialName);
        Assert.False(implementation.HasRuntimeSpecialName);

        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<T>
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance string M<U> (
                !T t,
                !!U u
            ) cil managed 
        {
            // Method begins at RVA 0x2099
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0`1'::M
    } // end of class <>E__0`1
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>M'<T, U> (
            class C`1<!!T> o,
            !!T t,
            !!U u
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 38 (0x26)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: callvirt instance string class C`1<!!T>::GetString()
        IL_0006: ldarga.s u
        IL_0008: constrained. !!U
        IL_000e: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0013: ldarga.s t
        IL_0015: constrained. !!T
        IL_001b: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0020: call string [mscorlib]System.String::Concat(string, string, string)
        IL_0025: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_08_WithLocalFunction_Generic()
    {
        var src = """
public static class Extensions
{
    extension<T>(C<T> o)
    {
        string M<U>(T t1, U u1)
        {
            U local<X, Y, Z>(T t2, U u2, X x2, Y y2, Z z2)
            {
                _ = o.GetString() + u1.ToString() + t1.ToString() + u2.ToString() + t2.ToString() + x2.ToString() + y2.ToString() + z2.ToString();
                return u2;
            };

            return local(t1, u1, 0, t1, u1).ToString();
        }
    }
}

public class C<T>
{
    public string GetString() => null;
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<T>
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance string M<U> (
                !T t1,
                !!U u1
            ) cil managed 
        {
            // Method begins at RVA 0x216a
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0`1'::M
    } // end of class <>E__0`1
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0`2'<T, U>
        extends [mscorlib]System.ValueType
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public class C`1<!T> o
        .field public !U u1
        .field public !T t1
    } // end of class <>c__DisplayClass0_0`2
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>M'<T, U> (
            class C`1<!!T> o,
            !!T t1,
            !!U u1
        ) cil managed 
    {
        // Method begins at RVA 0x2068
        // Code size 71 (0x47)
        .maxstack 6
        .locals init (
            [0] valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>,
            [1] !!U
        )
        IL_0000: ldloca.s 0
        IL_0002: ldarg.0
        IL_0003: stfld class C`1<!0> valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::o
        IL_0008: ldloca.s 0
        IL_000a: ldarg.2
        IL_000b: stfld !1 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::u1
        IL_0010: ldloca.s 0
        IL_0012: ldarg.1
        IL_0013: stfld !0 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::t1
        IL_0018: ldloc.0
        IL_0019: ldfld !0 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::t1
        IL_001e: ldloc.0
        IL_001f: ldfld !1 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::u1
        IL_0024: ldc.i4.0
        IL_0025: ldloc.0
        IL_0026: ldfld !0 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::t1
        IL_002b: ldloc.0
        IL_002c: ldfld !1 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::u1
        IL_0031: ldloca.s 0
        IL_0033: call !!1 Extensions::'<<Extension>M>b__0_0'<!!T, !!U, int32, !!T, !!U>(!!0, !!1, !!2, !!3, !!4, valuetype Extensions/'<>c__DisplayClass0_0`2'<!!0, !!1>&)
        IL_0038: stloc.1
        IL_0039: ldloca.s 1
        IL_003b: constrained. !!U
        IL_0041: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0046: ret
    } // end of method Extensions::'<Extension>M'
    .method assembly hidebysig static 
        !!U '<<Extension>M>b__0_0'<T, U, X, Y, Z> (
            !!T t2,
            !!U u2,
            !!X x2,
            !!Y y2,
            !!Z z2,
            valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>& ''
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x20bc
        // Code size 151 (0x97)
        .maxstack 4
        IL_0000: ldc.i4.8
        IL_0001: newarr [mscorlib]System.String
        IL_0006: dup
        IL_0007: ldc.i4.0
        IL_0008: ldarg.s 5
        IL_000a: ldfld class C`1<!0> valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::o
        IL_000f: callvirt instance string class C`1<!!T>::GetString()
        IL_0014: stelem.ref
        IL_0015: dup
        IL_0016: ldc.i4.1
        IL_0017: ldarg.s 5
        IL_0019: ldflda !1 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::u1
        IL_001e: constrained. !!U
        IL_0024: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0029: stelem.ref
        IL_002a: dup
        IL_002b: ldc.i4.2
        IL_002c: ldarg.s 5
        IL_002e: ldflda !0 valuetype Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::t1
        IL_0033: constrained. !!T
        IL_0039: callvirt instance string [mscorlib]System.Object::ToString()
        IL_003e: stelem.ref
        IL_003f: dup
        IL_0040: ldc.i4.3
        IL_0041: ldarga.s u2
        IL_0043: constrained. !!U
        IL_0049: callvirt instance string [mscorlib]System.Object::ToString()
        IL_004e: stelem.ref
        IL_004f: dup
        IL_0050: ldc.i4.4
        IL_0051: ldarga.s t2
        IL_0053: constrained. !!T
        IL_0059: callvirt instance string [mscorlib]System.Object::ToString()
        IL_005e: stelem.ref
        IL_005f: dup
        IL_0060: ldc.i4.5
        IL_0061: ldarga.s x2
        IL_0063: constrained. !!X
        IL_0069: callvirt instance string [mscorlib]System.Object::ToString()
        IL_006e: stelem.ref
        IL_006f: dup
        IL_0070: ldc.i4.6
        IL_0071: ldarga.s y2
        IL_0073: constrained. !!Y
        IL_0079: callvirt instance string [mscorlib]System.Object::ToString()
        IL_007e: stelem.ref
        IL_007f: dup
        IL_0080: ldc.i4.7
        IL_0081: ldarga.s z2
        IL_0083: constrained. !!Z
        IL_0089: callvirt instance string [mscorlib]System.Object::ToString()
        IL_008e: stelem.ref
        IL_008f: call string [mscorlib]System.String::Concat(string[])
        IL_0094: pop
        IL_0095: ldarg.1
        IL_0096: ret
    } // end of method Extensions::'<<Extension>M>b__0_0'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_09_WithLambda_Generic()
    {
        var src = """
public static class Extensions
{
    extension<T>(C<T> o)
    {
        string M<U>(T t1, U u1)
        {
            System.Func<T, U, U> local = (T t2, U u2) =>
            {
                _ = o.GetString() + u1.ToString() + t1.ToString() + u2.ToString() + t2.ToString();
                return u2;
            };

            return local(t1, u1).ToString();
        }
    }
}

public class C<T>
{
    public string GetString() => null;
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<T>
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance string M<U> (
                !T t1,
                !!U u1
            ) cil managed 
        {
            // Method begins at RVA 0x20c6
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0`1'::M
    } // end of class <>E__0`1
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0`2'<T, U>
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public class C`1<!T> o
        .field public !U u1
        .field public !T t1
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor () cil managed 
        {
            // Method begins at RVA 0x20be
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method '<>c__DisplayClass0_0`2'::.ctor
        .method assembly hidebysig 
            instance !U '<<Extension>M>b__0' (
                !T t2,
                !U u2
            ) cil managed 
        {
            // Method begins at RVA 0x20cc
            // Code size 100 (0x64)
            .maxstack 4
            IL_0000: ldc.i4.5
            IL_0001: newarr [mscorlib]System.String
            IL_0006: dup
            IL_0007: ldc.i4.0
            IL_0008: ldarg.0
            IL_0009: ldfld class C`1<!0> class Extensions/'<>c__DisplayClass0_0`2'<!T, !U>::o
            IL_000e: callvirt instance string class C`1<!T>::GetString()
            IL_0013: stelem.ref
            IL_0014: dup
            IL_0015: ldc.i4.1
            IL_0016: ldarg.0
            IL_0017: ldflda !1 class Extensions/'<>c__DisplayClass0_0`2'<!T, !U>::u1
            IL_001c: constrained. !U
            IL_0022: callvirt instance string [mscorlib]System.Object::ToString()
            IL_0027: stelem.ref
            IL_0028: dup
            IL_0029: ldc.i4.2
            IL_002a: ldarg.0
            IL_002b: ldflda !0 class Extensions/'<>c__DisplayClass0_0`2'<!T, !U>::t1
            IL_0030: constrained. !T
            IL_0036: callvirt instance string [mscorlib]System.Object::ToString()
            IL_003b: stelem.ref
            IL_003c: dup
            IL_003d: ldc.i4.3
            IL_003e: ldarga.s u2
            IL_0040: constrained. !U
            IL_0046: callvirt instance string [mscorlib]System.Object::ToString()
            IL_004b: stelem.ref
            IL_004c: dup
            IL_004d: ldc.i4.4
            IL_004e: ldarga.s t2
            IL_0050: constrained. !T
            IL_0056: callvirt instance string [mscorlib]System.Object::ToString()
            IL_005b: stelem.ref
            IL_005c: call string [mscorlib]System.String::Concat(string[])
            IL_0061: pop
            IL_0062: ldarg.2
            IL_0063: ret
        } // end of method '<>c__DisplayClass0_0`2'::'<<Extension>M>b__0'
    } // end of class <>c__DisplayClass0_0`2
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>M'<T, U> (
            class C`1<!!T> o,
            !!T t1,
            !!U u1
        ) cil managed 
    {
        // Method begins at RVA 0x2068
        // Code size 71 (0x47)
        .maxstack 3
        .locals init (
            [0] class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>,
            [1] !!U
        )
        IL_0000: newobj instance void class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::.ctor()
        IL_0005: stloc.0
        IL_0006: ldloc.0
        IL_0007: ldarg.0
        IL_0008: stfld class C`1<!0> class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::o
        IL_000d: ldloc.0
        IL_000e: ldarg.2
        IL_000f: stfld !1 class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::u1
        IL_0014: ldloc.0
        IL_0015: ldarg.1
        IL_0016: stfld !0 class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::t1
        IL_001b: ldloc.0
        IL_001c: ldftn instance !1 class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::'<<Extension>M>b__0'(!0, !1)
        IL_0022: newobj instance void class [mscorlib]System.Func`3<!!T, !!U, !!U>::.ctor(object, native int)
        IL_0027: ldloc.0
        IL_0028: ldfld !0 class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::t1
        IL_002d: ldloc.0
        IL_002e: ldfld !1 class Extensions/'<>c__DisplayClass0_0`2'<!!T, !!U>::u1
        IL_0033: callvirt instance !2 class [mscorlib]System.Func`3<!!T, !!U, !!U>::Invoke(!0, !1)
        IL_0038: stloc.1
        IL_0039: ldloca.s 1
        IL_003b: constrained. !!U
        IL_0041: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0046: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_10_Iterator_Generic()
    {
        var src = """
public static class Extensions
{
    extension<T>(C<T> o)
    {
        System.Collections.Generic.IEnumerable<string> M<U>(T t1, U u1)
        {
            yield return o.GetString() + u1.ToString() + t1.ToString();
        }
    }
}

public class C<T>
{
    public string GetString() => null;
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", ("""
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<T>
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance class [mscorlib]System.Collections.Generic.IEnumerable`1<string> M<U> (
                !T t1,
                !!U u1
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
                01 00 1f 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
                45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
                60 32 00 00
            )
            // Method begins at RVA 0x2090
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0`1'::M
    } // end of class <>E__0`1
    .class nested private auto ansi sealed beforefieldinit '<<Extension>M>d__0`2'<T, U>
        extends [mscorlib]System.Object
        implements class [mscorlib]System.Collections.Generic.IEnumerable`1<string>,
                   [mscorlib]System.Collections.IEnumerable,
                   class [mscorlib]System.Collections.Generic.IEnumerator`1<string>,

""" +
        (ExecutionConditionUtil.IsMonoOrCoreClr ?
"""
                   [mscorlib]System.Collections.IEnumerator,
                   [mscorlib]System.IDisposable

""" :
"""
                   [mscorlib]System.IDisposable,
                   [mscorlib]System.Collections.IEnumerator

""") +
"""
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field private int32 '<>1__state'
        .field private string '<>2__current'
        .field private int32 '<>l__initialThreadId'
        .field private class C`1<!T> o
        .field public class C`1<!T> '<>3__o'
        .field private !U u1
        .field public !U '<>3__u1'
        .field private !T t1
        .field public !T '<>3__t1'
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                int32 '<>1__state'
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            // Method begins at RVA 0x2093
            // Code size 25 (0x19)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ldarg.0
            IL_0007: ldarg.1
            IL_0008: stfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_000d: ldarg.0
            IL_000e: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
            IL_0013: stfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>l__initialThreadId'
            IL_0018: ret
        } // end of method '<<Extension>M>d__0`2'::.ctor
        .method private final hidebysig newslot virtual 
            instance void System.IDisposable.Dispose () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.IDisposable::Dispose()
            // Method begins at RVA 0x20ad
            // Code size 9 (0x9)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldc.i4.s -2
            IL_0003: stfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_0008: ret
        } // end of method '<<Extension>M>d__0`2'::System.IDisposable.Dispose
        .method private final hidebysig newslot virtual 
            instance bool MoveNext () cil managed 
        {
            .override method instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
            // Method begins at RVA 0x20b8
            // Code size 97 (0x61)
            .maxstack 4
            .locals init (
                [0] int32
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_0006: stloc.0
            IL_0007: ldloc.0
            IL_0008: brfalse.s IL_0010
            IL_000a: ldloc.0
            IL_000b: ldc.i4.1
            IL_000c: beq.s IL_0058
            IL_000e: ldc.i4.0
            IL_000f: ret
            IL_0010: ldarg.0
            IL_0011: ldc.i4.m1
            IL_0012: stfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_0017: ldarg.0
            IL_0018: ldarg.0
            IL_0019: ldfld class C`1<!0> class Extensions/'<<Extension>M>d__0`2'<!T, !U>::o
            IL_001e: callvirt instance string class C`1<!T>::GetString()
            IL_0023: ldarg.0
            IL_0024: ldflda !1 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::u1
            IL_0029: constrained. !U
            IL_002f: callvirt instance string [mscorlib]System.Object::ToString()
            IL_0034: ldarg.0
            IL_0035: ldflda !0 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::t1
            IL_003a: constrained. !T
            IL_0040: callvirt instance string [mscorlib]System.Object::ToString()
            IL_0045: call string [mscorlib]System.String::Concat(string, string, string)
            IL_004a: stfld string class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>2__current'
            IL_004f: ldarg.0
            IL_0050: ldc.i4.1
            IL_0051: stfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_0056: ldc.i4.1
            IL_0057: ret
            IL_0058: ldarg.0
            IL_0059: ldc.i4.m1
            IL_005a: stfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_005f: ldc.i4.0
            IL_0060: ret
        } // end of method '<<Extension>M>d__0`2'::MoveNext
        .method private final hidebysig specialname newslot virtual 
            instance string 'System.Collections.Generic.IEnumerator<System.String>.get_Current' () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<string>::get_Current()
            // Method begins at RVA 0x2125
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld string class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>2__current'
            IL_0006: ret
        } // end of method '<<Extension>M>d__0`2'::'System.Collections.Generic.IEnumerator<System.String>.get_Current'
        .method private final hidebysig newslot virtual 
            instance void System.Collections.IEnumerator.Reset () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.Collections.IEnumerator::Reset()
            // Method begins at RVA 0x212d
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<<Extension>M>d__0`2'::System.Collections.IEnumerator.Reset
        .method private final hidebysig specialname newslot virtual 
            instance object System.Collections.IEnumerator.get_Current () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance object [mscorlib]System.Collections.IEnumerator::get_Current()
            // Method begins at RVA 0x2125
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld string class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>2__current'
            IL_0006: ret
        } // end of method '<<Extension>M>d__0`2'::System.Collections.IEnumerator.get_Current
        .method private final hidebysig newslot virtual 
            instance class [mscorlib]System.Collections.Generic.IEnumerator`1<string> 'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator' () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<string>::GetEnumerator()
            // Method begins at RVA 0x2134
            // Code size 79 (0x4f)
            .maxstack 2
            .locals init (
                [0] class Extensions/'<<Extension>M>d__0`2'<!T, !U>
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_0006: ldc.i4.s -2
            IL_0008: bne.un.s IL_0022
            IL_000a: ldarg.0
            IL_000b: ldfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>l__initialThreadId'
            IL_0010: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
            IL_0015: bne.un.s IL_0022
            IL_0017: ldarg.0
            IL_0018: ldc.i4.0
            IL_0019: stfld int32 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_001e: ldarg.0
            IL_001f: stloc.0
            IL_0020: br.s IL_0029
            IL_0022: ldc.i4.0
            IL_0023: newobj instance void class Extensions/'<<Extension>M>d__0`2'<!T, !U>::.ctor(int32)
            IL_0028: stloc.0
            IL_0029: ldloc.0
            IL_002a: ldarg.0
            IL_002b: ldfld class C`1<!0> class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>3__o'
            IL_0030: stfld class C`1<!0> class Extensions/'<<Extension>M>d__0`2'<!T, !U>::o
            IL_0035: ldloc.0
            IL_0036: ldarg.0
            IL_0037: ldfld !0 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>3__t1'
            IL_003c: stfld !0 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::t1
            IL_0041: ldloc.0
            IL_0042: ldarg.0
            IL_0043: ldfld !1 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>3__u1'
            IL_0048: stfld !1 class Extensions/'<<Extension>M>d__0`2'<!T, !U>::u1
            IL_004d: ldloc.0
            IL_004e: ret
        } // end of method '<<Extension>M>d__0`2'::'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator'
        .method private final hidebysig newslot virtual 
            instance class [mscorlib]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [mscorlib]System.Collections.IEnumerator [mscorlib]System.Collections.IEnumerable::GetEnumerator()
            // Method begins at RVA 0x218f
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance class [mscorlib]System.Collections.Generic.IEnumerator`1<string> class Extensions/'<<Extension>M>d__0`2'<!T, !U>::'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator'()
            IL_0006: ret
        } // end of method '<<Extension>M>d__0`2'::System.Collections.IEnumerable.GetEnumerator
        // Properties
        .property instance string 'System.Collections.Generic.IEnumerator<System.String>.Current'()
        {
            .get instance string Extensions/'<<Extension>M>d__0`2'::'System.Collections.Generic.IEnumerator<System.String>.get_Current'()
        }
        .property instance object System.Collections.IEnumerator.Current()
        {
            .get instance object Extensions/'<<Extension>M>d__0`2'::System.Collections.IEnumerator.get_Current()
        }
    } // end of class <<Extension>M>d__0`2
    // Methods
    .method private hidebysig specialname static 
        class [mscorlib]System.Collections.Generic.IEnumerable`1<string> '<Extension>M'<T, U> (
            class C`1<!!T> o,
            !!T t1,
            !!U u1
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
            01 00 1f 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
            45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
            60 32 00 00
        )
        // Method begins at RVA 0x2067
        // Code size 29 (0x1d)
        .maxstack 8
        IL_0000: ldc.i4.s -2
        IL_0002: newobj instance void class Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::.ctor(int32)
        IL_0007: dup
        IL_0008: ldarg.0
        IL_0009: stfld class C`1<!0> class Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::'<>3__o'
        IL_000e: dup
        IL_000f: ldarg.1
        IL_0010: stfld !0 class Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::'<>3__t1'
        IL_0015: dup
        IL_0016: ldarg.2
        IL_0017: stfld !1 class Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::'<>3__u1'
        IL_001c: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""").Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceMethod_11_Async_Generic()
    {
        var src = """
public static class Extensions
{
    extension<T>(C<T> o)
    {
        async System.Threading.Tasks.Task<string> M<U>(T t1, U u1)
        {
            await System.Threading.Tasks.Task.Yield();
            return o.GetString() + u1.ToString() + t1.ToString();
        }
    }
}

public class C<T>
{
    public string GetString() => null;
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0`1'<T>
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig 
            instance class [mscorlib]System.Threading.Tasks.Task`1<string> M<U> (
                !T t1,
                !!U u1
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
                01 00 1f 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
                45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
                60 32 00 00
            )
            // Method begins at RVA 0x20c6
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0`1'::M
    } // end of class <>E__0`1
    .class nested private auto ansi sealed beforefieldinit '<<Extension>M>d__0`2'<T, U>
        extends [mscorlib]System.ValueType
        implements [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public int32 '<>1__state'
        .field public valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> '<>t__builder'
        .field public class C`1<!T> o
        .field public !U u1
        .field public !T t1
        .field private valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
        // Methods
        .method private final hidebysig newslot virtual 
            instance void MoveNext () cil managed 
        {
            .override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
            // Method begins at RVA 0x20cc
            // Code size 202 (0xca)
            .maxstack 3
            .locals init (
                [0] int32,
                [1] string,
                [2] valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
                [3] valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable,
                [4] class [mscorlib]System.Exception
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_0006: stloc.0
            .try
            {
                IL_0007: ldloc.0
                IL_0008: brfalse.s IL_0044
                IL_000a: call valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable [mscorlib]System.Threading.Tasks.Task::Yield()
                IL_000f: stloc.3
                IL_0010: ldloca.s 3
                IL_0012: call instance valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [mscorlib]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
                IL_0017: stloc.2
                IL_0018: ldloca.s 2
                IL_001a: call instance bool [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
                IL_001f: brtrue.s IL_0060
                IL_0021: ldarg.0
                IL_0022: ldc.i4.0
                IL_0023: dup
                IL_0024: stloc.0
                IL_0025: stfld int32 valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
                IL_002a: ldarg.0
                IL_002b: ldloc.2
                IL_002c: stfld valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>u__1'
                IL_0031: ldarg.0
                IL_0032: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>t__builder'
                IL_0037: ldloca.s 2
                IL_0039: ldarg.0
                IL_003a: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>>(!!0&, !!1&)
                IL_003f: leave IL_00c9
                IL_0044: ldarg.0
                IL_0045: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>u__1'
                IL_004a: stloc.2
                IL_004b: ldarg.0
                IL_004c: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>u__1'
                IL_0051: initobj [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
                IL_0057: ldarg.0
                IL_0058: ldc.i4.m1
                IL_0059: dup
                IL_005a: stloc.0
                IL_005b: stfld int32 valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
                IL_0060: ldloca.s 2
                IL_0062: call instance void [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
                IL_0067: ldarg.0
                IL_0068: ldfld class C`1<!0> valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::o
                IL_006d: callvirt instance string class C`1<!T>::GetString()
                IL_0072: ldarg.0
                IL_0073: ldflda !1 valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::u1
                IL_0078: constrained. !U
                IL_007e: callvirt instance string [mscorlib]System.Object::ToString()
                IL_0083: ldarg.0
                IL_0084: ldflda !0 valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::t1
                IL_0089: constrained. !T
                IL_008f: callvirt instance string [mscorlib]System.Object::ToString()
                IL_0094: call string [mscorlib]System.String::Concat(string, string, string)
                IL_0099: stloc.1
                IL_009a: leave.s IL_00b5
            } // end .try
            catch [mscorlib]System.Exception
            {
                IL_009c: stloc.s 4
                IL_009e: ldarg.0
                IL_009f: ldc.i4.s -2
                IL_00a1: stfld int32 valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
                IL_00a6: ldarg.0
                IL_00a7: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>t__builder'
                IL_00ac: ldloc.s 4
                IL_00ae: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetException(class [mscorlib]System.Exception)
                IL_00b3: leave.s IL_00c9
            } // end handler
            IL_00b5: ldarg.0
            IL_00b6: ldc.i4.s -2
            IL_00b8: stfld int32 valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>1__state'
            IL_00bd: ldarg.0
            IL_00be: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>t__builder'
            IL_00c3: ldloc.1
            IL_00c4: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetResult(!0)
            IL_00c9: ret
        } // end of method '<<Extension>M>d__0`2'::MoveNext
        .method private final hidebysig newslot virtual 
            instance void SetStateMachine (
                class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
            // Method begins at RVA 0x21b4
            // Code size 13 (0xd)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> valuetype Extensions/'<<Extension>M>d__0`2'<!T, !U>::'<>t__builder'
            IL_0006: ldarg.1
            IL_0007: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
            IL_000c: ret
        } // end of method '<<Extension>M>d__0`2'::SetStateMachine
    } // end of class <<Extension>M>d__0`2
    // Methods
    .method private hidebysig specialname static 
        class [mscorlib]System.Threading.Tasks.Task`1<string> '<Extension>M'<T, U> (
            class C`1<!!T> o,
            !!T t1,
            !!U u1
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
            01 00 1f 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
            45 78 74 65 6e 73 69 6f 6e 3e 4d 3e 64 5f 5f 30
            60 32 00 00
        )
        // Method begins at RVA 0x2068
        // Code size 71 (0x47)
        .maxstack 2
        .locals init (
            [0] valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>
        )
        IL_0000: ldloca.s 0
        IL_0002: call valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::Create()
        IL_0007: stfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::'<>t__builder'
        IL_000c: ldloca.s 0
        IL_000e: ldarg.0
        IL_000f: stfld class C`1<!0> valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::o
        IL_0014: ldloca.s 0
        IL_0016: ldarg.1
        IL_0017: stfld !0 valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::t1
        IL_001c: ldloca.s 0
        IL_001e: ldarg.2
        IL_001f: stfld !1 valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::u1
        IL_0024: ldloca.s 0
        IL_0026: ldc.i4.m1
        IL_0027: stfld int32 valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::'<>1__state'
        IL_002c: ldloca.s 0
        IL_002e: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::'<>t__builder'
        IL_0033: ldloca.s 0
        IL_0035: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::Start<valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>>(!!0&)
        IL_003a: ldloca.s 0
        IL_003c: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> valuetype Extensions/'<<Extension>M>d__0`2'<!!T, !!U>::'<>t__builder'
        IL_0041: call instance class [mscorlib]System.Threading.Tasks.Task`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::get_Task()
        IL_0046: ret
    } // end of method Extensions::'<Extension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_StaticMethod_01()
    {
        var src = """
public static class Extensions
{
    extension(object _)
    {
        static string M(object o, string s)
        {
            return o + s;
        }
    }
}
""";
        var comp = CreateCompilation(src);

        MethodSymbol implementation = comp.GetTypeByMetadataName("Extensions").GetMembers().OfType<MethodSymbol>().Single();
        Assert.True(implementation.IsStatic);
        Assert.Equal(MethodKind.Ordinary, implementation.MethodKind);
        Assert.Equal(2, implementation.ParameterCount);
        AssertEx.Equal("System.String Extensions.<StaticExtension>M(System.Object o, System.String s)", implementation.ToTestDisplayString());
        Assert.True(implementation.IsImplicitlyDeclared);
        Assert.False(implementation.IsExtensionMethod);
        Assert.True(implementation.HasSpecialName);
        Assert.False(implementation.HasRuntimeSpecialName);

        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig static 
            string M (
                object o,
                string s
            ) cil managed 
        {
            // Method begins at RVA 0x207b
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        string '<StaticExtension>M' (
            object o,
            string s
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 19 (0x13)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: brtrue.s IL_0006
        IL_0003: ldnull
        IL_0004: br.s IL_000c
        IL_0006: ldarg.0
        IL_0007: callvirt instance string [mscorlib]System.Object::ToString()
        IL_000c: ldarg.1
        IL_000d: call string [mscorlib]System.String::Concat(string, string)
        IL_0012: ret
    } // end of method Extensions::'<StaticExtension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_StaticMethod_02_WithLocalFunction()
    {
        var src = """
public static class Extensions
{
    extension(object _)
    {
        static string M(object o, string s)
        {
            string local() => o + s;
            return local();
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig static 
            string M (
                object o,
                string s
            ) cil managed 
        {
            // Method begins at RVA 0x20ab
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
        extends [mscorlib]System.ValueType
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public object o
        .field public string s
    } // end of class <>c__DisplayClass0_0
    // Methods
    .method private hidebysig specialname static 
        string '<StaticExtension>M' (
            object o,
            string s
        ) cil managed 
    {
        // Method begins at RVA 0x2068
        // Code size 24 (0x18)
        .maxstack 2
        .locals init (
            [0] valuetype Extensions/'<>c__DisplayClass0_0'
        )
        IL_0000: ldloca.s 0
        IL_0002: ldarg.0
        IL_0003: stfld object Extensions/'<>c__DisplayClass0_0'::o
        IL_0008: ldloca.s 0
        IL_000a: ldarg.1
        IL_000b: stfld string Extensions/'<>c__DisplayClass0_0'::s
        IL_0010: ldloca.s 0
        IL_0012: call string Extensions::'<<StaticExtension>M>b__0_0'(valuetype Extensions/'<>c__DisplayClass0_0'&)
        IL_0017: ret
    } // end of method Extensions::'<StaticExtension>M'
    .method assembly hidebysig static 
        string '<<StaticExtension>M>b__0_0' (
            valuetype Extensions/'<>c__DisplayClass0_0'& ''
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x208c
        // Code size 30 (0x1e)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: ldfld object Extensions/'<>c__DisplayClass0_0'::o
        IL_0006: dup
        IL_0007: brtrue.s IL_000d
        IL_0009: pop
        IL_000a: ldnull
        IL_000b: br.s IL_0012
        IL_000d: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0012: ldarg.0
        IL_0013: ldfld string Extensions/'<>c__DisplayClass0_0'::s
        IL_0018: call string [mscorlib]System.String::Concat(string, string)
        IL_001d: ret
    } // end of method Extensions::'<<StaticExtension>M>b__0_0'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_StaticMethod_03_WithLambda()
    {
        var src = """
public static class Extensions
{
    extension(object _)
    {
        static string M(object o, string s)
        {
            System.Func<string> local = () => o + s;
            return local();
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig static 
            string M (
                object o,
                string s
            ) cil managed 
        {
            // Method begins at RVA 0x208c
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public object o
        .field public string s
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor () cil managed 
        {
            // Method begins at RVA 0x208f
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method '<>c__DisplayClass0_0'::.ctor
        .method assembly hidebysig 
            instance string '<<StaticExtension>M>b__0' () cil managed 
        {
            // Method begins at RVA 0x2097
            // Code size 30 (0x1e)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld object Extensions/'<>c__DisplayClass0_0'::o
            IL_0006: dup
            IL_0007: brtrue.s IL_000d
            IL_0009: pop
            IL_000a: ldnull
            IL_000b: br.s IL_0012
            IL_000d: callvirt instance string [mscorlib]System.Object::ToString()
            IL_0012: ldarg.0
            IL_0013: ldfld string Extensions/'<>c__DisplayClass0_0'::s
            IL_0018: call string [mscorlib]System.String::Concat(string, string)
            IL_001d: ret
        } // end of method '<>c__DisplayClass0_0'::'<<StaticExtension>M>b__0'
    } // end of class <>c__DisplayClass0_0
    // Methods
    .method private hidebysig specialname static 
        string '<StaticExtension>M' (
            object o,
            string s
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 36 (0x24)
        .maxstack 8
        IL_0000: newobj instance void Extensions/'<>c__DisplayClass0_0'::.ctor()
        IL_0005: dup
        IL_0006: ldarg.0
        IL_0007: stfld object Extensions/'<>c__DisplayClass0_0'::o
        IL_000c: dup
        IL_000d: ldarg.1
        IL_000e: stfld string Extensions/'<>c__DisplayClass0_0'::s
        IL_0013: ldftn instance string Extensions/'<>c__DisplayClass0_0'::'<<StaticExtension>M>b__0'()
        IL_0019: newobj instance void class [mscorlib]System.Func`1<string>::.ctor(object, native int)
        IL_001e: callvirt instance !0 class [mscorlib]System.Func`1<string>::Invoke()
        IL_0023: ret
    } // end of method Extensions::'<StaticExtension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_StaticMethod_04_Iterator()
    {
        var src = """
public static class Extensions
{
    extension(object _)
    {
        static System.Collections.Generic.IEnumerable<string> M(object o, string s)
        {
            yield return o + s;
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", ("""
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig static 
            class [mscorlib]System.Collections.Generic.IEnumerable`1<string> M (
                object o,
                string s
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
                01 00 23 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
                53 74 61 74 69 63 45 78 74 65 6e 73 69 6f 6e 3e
                4d 3e 64 5f 5f 30 00 00
            )
            // Method begins at RVA 0x207e
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<<StaticExtension>M>d__0'
        extends [mscorlib]System.Object
        implements class [mscorlib]System.Collections.Generic.IEnumerable`1<string>,
                   [mscorlib]System.Collections.IEnumerable,
                   class [mscorlib]System.Collections.Generic.IEnumerator`1<string>,

""" +
        (ExecutionConditionUtil.IsMonoOrCoreClr ?
"""
                   [mscorlib]System.Collections.IEnumerator,
                   [mscorlib]System.IDisposable

""" :
"""
                   [mscorlib]System.IDisposable,
                   [mscorlib]System.Collections.IEnumerator

""") +
"""
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field private int32 '<>1__state'
        .field private string '<>2__current'
        .field private int32 '<>l__initialThreadId'
        .field private object o
        .field public object '<>3__o'
        .field private string s
        .field public string '<>3__s'
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                int32 '<>1__state'
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            // Method begins at RVA 0x2081
            // Code size 25 (0x19)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ldarg.0
            IL_0007: ldarg.1
            IL_0008: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_000d: ldarg.0
            IL_000e: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
            IL_0013: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>l__initialThreadId'
            IL_0018: ret
        } // end of method '<<StaticExtension>M>d__0'::.ctor
        .method private final hidebysig newslot virtual 
            instance void System.IDisposable.Dispose () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.IDisposable::Dispose()
            // Method begins at RVA 0x209b
            // Code size 9 (0x9)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldc.i4.s -2
            IL_0003: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_0008: ret
        } // end of method '<<StaticExtension>M>d__0'::System.IDisposable.Dispose
        .method private final hidebysig newslot virtual 
            instance bool MoveNext () cil managed 
        {
            .override method instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
            // Method begins at RVA 0x20a8
            // Code size 76 (0x4c)
            .maxstack 3
            .locals init (
                [0] int32
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_0006: stloc.0
            IL_0007: ldloc.0
            IL_0008: brfalse.s IL_0010
            IL_000a: ldloc.0
            IL_000b: ldc.i4.1
            IL_000c: beq.s IL_0043
            IL_000e: ldc.i4.0
            IL_000f: ret
            IL_0010: ldarg.0
            IL_0011: ldc.i4.m1
            IL_0012: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_0017: ldarg.0
            IL_0018: ldarg.0
            IL_0019: ldfld object Extensions/'<<StaticExtension>M>d__0'::o
            IL_001e: dup
            IL_001f: brtrue.s IL_0025
            IL_0021: pop
            IL_0022: ldnull
            IL_0023: br.s IL_002a
            IL_0025: callvirt instance string [mscorlib]System.Object::ToString()
            IL_002a: ldarg.0
            IL_002b: ldfld string Extensions/'<<StaticExtension>M>d__0'::s
            IL_0030: call string [mscorlib]System.String::Concat(string, string)
            IL_0035: stfld string Extensions/'<<StaticExtension>M>d__0'::'<>2__current'
            IL_003a: ldarg.0
            IL_003b: ldc.i4.1
            IL_003c: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_0041: ldc.i4.1
            IL_0042: ret
            IL_0043: ldarg.0
            IL_0044: ldc.i4.m1
            IL_0045: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_004a: ldc.i4.0
            IL_004b: ret
        } // end of method '<<StaticExtension>M>d__0'::MoveNext
        .method private final hidebysig specialname newslot virtual 
            instance string 'System.Collections.Generic.IEnumerator<System.String>.get_Current' () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<string>::get_Current()
            // Method begins at RVA 0x2100
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld string Extensions/'<<StaticExtension>M>d__0'::'<>2__current'
            IL_0006: ret
        } // end of method '<<StaticExtension>M>d__0'::'System.Collections.Generic.IEnumerator<System.String>.get_Current'
        .method private final hidebysig newslot virtual 
            instance void System.Collections.IEnumerator.Reset () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.Collections.IEnumerator::Reset()
            // Method begins at RVA 0x2108
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<<StaticExtension>M>d__0'::System.Collections.IEnumerator.Reset
        .method private final hidebysig specialname newslot virtual 
            instance object System.Collections.IEnumerator.get_Current () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance object [mscorlib]System.Collections.IEnumerator::get_Current()
            // Method begins at RVA 0x2100
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldfld string Extensions/'<<StaticExtension>M>d__0'::'<>2__current'
            IL_0006: ret
        } // end of method '<<StaticExtension>M>d__0'::System.Collections.IEnumerator.get_Current
        .method private final hidebysig newslot virtual 
            instance class [mscorlib]System.Collections.Generic.IEnumerator`1<string> 'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator' () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<string>::GetEnumerator()
            // Method begins at RVA 0x2110
            // Code size 67 (0x43)
            .maxstack 2
            .locals init (
                [0] class Extensions/'<<StaticExtension>M>d__0'
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_0006: ldc.i4.s -2
            IL_0008: bne.un.s IL_0022
            IL_000a: ldarg.0
            IL_000b: ldfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>l__initialThreadId'
            IL_0010: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
            IL_0015: bne.un.s IL_0022
            IL_0017: ldarg.0
            IL_0018: ldc.i4.0
            IL_0019: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_001e: ldarg.0
            IL_001f: stloc.0
            IL_0020: br.s IL_0029
            IL_0022: ldc.i4.0
            IL_0023: newobj instance void Extensions/'<<StaticExtension>M>d__0'::.ctor(int32)
            IL_0028: stloc.0
            IL_0029: ldloc.0
            IL_002a: ldarg.0
            IL_002b: ldfld object Extensions/'<<StaticExtension>M>d__0'::'<>3__o'
            IL_0030: stfld object Extensions/'<<StaticExtension>M>d__0'::o
            IL_0035: ldloc.0
            IL_0036: ldarg.0
            IL_0037: ldfld string Extensions/'<<StaticExtension>M>d__0'::'<>3__s'
            IL_003c: stfld string Extensions/'<<StaticExtension>M>d__0'::s
            IL_0041: ldloc.0
            IL_0042: ret
        } // end of method '<<StaticExtension>M>d__0'::'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator'
        .method private final hidebysig newslot virtual 
            instance class [mscorlib]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance class [mscorlib]System.Collections.IEnumerator [mscorlib]System.Collections.IEnumerable::GetEnumerator()
            // Method begins at RVA 0x215f
            // Code size 7 (0x7)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: call instance class [mscorlib]System.Collections.Generic.IEnumerator`1<string> Extensions/'<<StaticExtension>M>d__0'::'System.Collections.Generic.IEnumerable<System.String>.GetEnumerator'()
            IL_0006: ret
        } // end of method '<<StaticExtension>M>d__0'::System.Collections.IEnumerable.GetEnumerator
        // Properties
        .property instance string 'System.Collections.Generic.IEnumerator<System.String>.Current'()
        {
            .get instance string Extensions/'<<StaticExtension>M>d__0'::'System.Collections.Generic.IEnumerator<System.String>.get_Current'()
        }
        .property instance object System.Collections.IEnumerator.Current()
        {
            .get instance object Extensions/'<<StaticExtension>M>d__0'::System.Collections.IEnumerator.get_Current()
        }
    } // end of class <<StaticExtension>M>d__0
    // Methods
    .method private hidebysig specialname static 
        class [mscorlib]System.Collections.Generic.IEnumerable`1<string> '<StaticExtension>M' (
            object o,
            string s
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
            01 00 23 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
            53 74 61 74 69 63 45 78 74 65 6e 73 69 6f 6e 3e
            4d 3e 64 5f 5f 30 00 00
        )
        // Method begins at RVA 0x2067
        // Code size 22 (0x16)
        .maxstack 8
        IL_0000: ldc.i4.s -2
        IL_0002: newobj instance void Extensions/'<<StaticExtension>M>d__0'::.ctor(int32)
        IL_0007: dup
        IL_0008: ldarg.0
        IL_0009: stfld object Extensions/'<<StaticExtension>M>d__0'::'<>3__o'
        IL_000e: dup
        IL_000f: ldarg.1
        IL_0010: stfld string Extensions/'<<StaticExtension>M>d__0'::'<>3__s'
        IL_0015: ret
    } // end of method Extensions::'<StaticExtension>M'
} // end of class Extensions

""").Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_StaticMethod_05_Async()
    {
        var src = """
public static class Extensions
{
    extension(object _)
    {
        static async System.Threading.Tasks.Task<string> M(object o, string s)
        {
            await System.Threading.Tasks.Task.Yield();
            return o + s;
        }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig static 
            class [mscorlib]System.Threading.Tasks.Task`1<string> M (
                object o,
                string s
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
                01 00 23 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
                53 74 61 74 69 63 45 78 74 65 6e 73 69 6f 6e 3e
                4d 3e 64 5f 5f 30 00 00
            )
            // Method begins at RVA 0x20b3
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::M
    } // end of class <>E__0
    .class nested private auto ansi sealed beforefieldinit '<<StaticExtension>M>d__0'
        extends [mscorlib]System.ValueType
        implements [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Fields
        .field public int32 '<>1__state'
        .field public valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> '<>t__builder'
        .field public object o
        .field public string s
        .field private valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
        // Methods
        .method private final hidebysig newslot virtual 
            instance void MoveNext () cil managed 
        {
            .override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
            // Method begins at RVA 0x20b8
            // Code size 178 (0xb2)
            .maxstack 3
            .locals init (
                [0] int32,
                [1] string,
                [2] valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
                [3] valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable,
                [4] class [mscorlib]System.Exception
            )
            IL_0000: ldarg.0
            IL_0001: ldfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_0006: stloc.0
            .try
            {
                IL_0007: ldloc.0
                IL_0008: brfalse.s IL_0041
                IL_000a: call valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable [mscorlib]System.Threading.Tasks.Task::Yield()
                IL_000f: stloc.3
                IL_0010: ldloca.s 3
                IL_0012: call instance valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [mscorlib]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
                IL_0017: stloc.2
                IL_0018: ldloca.s 2
                IL_001a: call instance bool [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
                IL_001f: brtrue.s IL_005d
                IL_0021: ldarg.0
                IL_0022: ldc.i4.0
                IL_0023: dup
                IL_0024: stloc.0
                IL_0025: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
                IL_002a: ldarg.0
                IL_002b: ldloc.2
                IL_002c: stfld valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter Extensions/'<<StaticExtension>M>d__0'::'<>u__1'
                IL_0031: ldarg.0
                IL_0032: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<StaticExtension>M>d__0'::'<>t__builder'
                IL_0037: ldloca.s 2
                IL_0039: ldarg.0
                IL_003a: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, valuetype Extensions/'<<StaticExtension>M>d__0'>(!!0&, !!1&)
                IL_003f: leave.s IL_00b1
                IL_0041: ldarg.0
                IL_0042: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter Extensions/'<<StaticExtension>M>d__0'::'<>u__1'
                IL_0047: stloc.2
                IL_0048: ldarg.0
                IL_0049: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter Extensions/'<<StaticExtension>M>d__0'::'<>u__1'
                IL_004e: initobj [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
                IL_0054: ldarg.0
                IL_0055: ldc.i4.m1
                IL_0056: dup
                IL_0057: stloc.0
                IL_0058: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
                IL_005d: ldloca.s 2
                IL_005f: call instance void [mscorlib]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
                IL_0064: ldarg.0
                IL_0065: ldfld object Extensions/'<<StaticExtension>M>d__0'::o
                IL_006a: dup
                IL_006b: brtrue.s IL_0071
                IL_006d: pop
                IL_006e: ldnull
                IL_006f: br.s IL_0076
                IL_0071: callvirt instance string [mscorlib]System.Object::ToString()
                IL_0076: ldarg.0
                IL_0077: ldfld string Extensions/'<<StaticExtension>M>d__0'::s
                IL_007c: call string [mscorlib]System.String::Concat(string, string)
                IL_0081: stloc.1
                IL_0082: leave.s IL_009d
            } // end .try
            catch [mscorlib]System.Exception
            {
                IL_0084: stloc.s 4
                IL_0086: ldarg.0
                IL_0087: ldc.i4.s -2
                IL_0089: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
                IL_008e: ldarg.0
                IL_008f: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<StaticExtension>M>d__0'::'<>t__builder'
                IL_0094: ldloc.s 4
                IL_0096: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetException(class [mscorlib]System.Exception)
                IL_009b: leave.s IL_00b1
            } // end handler
            IL_009d: ldarg.0
            IL_009e: ldc.i4.s -2
            IL_00a0: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
            IL_00a5: ldarg.0
            IL_00a6: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<StaticExtension>M>d__0'::'<>t__builder'
            IL_00ab: ldloc.1
            IL_00ac: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetResult(!0)
            IL_00b1: ret
        } // end of method '<<StaticExtension>M>d__0'::MoveNext
        .method private final hidebysig newslot virtual 
            instance void SetStateMachine (
                class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
                01 00 00 00
            )
            .override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
            // Method begins at RVA 0x2188
            // Code size 13 (0xd)
            .maxstack 8
            IL_0000: ldarg.0
            IL_0001: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<StaticExtension>M>d__0'::'<>t__builder'
            IL_0006: ldarg.1
            IL_0007: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
            IL_000c: ret
        } // end of method '<<StaticExtension>M>d__0'::SetStateMachine
    } // end of class <<StaticExtension>M>d__0
    // Methods
    .method private hidebysig specialname static 
        class [mscorlib]System.Threading.Tasks.Task`1<string> '<StaticExtension>M' (
            object o,
            string s
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
            01 00 23 45 78 74 65 6e 73 69 6f 6e 73 2b 3c 3c
            53 74 61 74 69 63 45 78 74 65 6e 73 69 6f 6e 3e
            4d 3e 64 5f 5f 30 00 00
        )
        // Method begins at RVA 0x2068
        // Code size 63 (0x3f)
        .maxstack 2
        .locals init (
            [0] valuetype Extensions/'<<StaticExtension>M>d__0'
        )
        IL_0000: ldloca.s 0
        IL_0002: call valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::Create()
        IL_0007: stfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<StaticExtension>M>d__0'::'<>t__builder'
        IL_000c: ldloca.s 0
        IL_000e: ldarg.0
        IL_000f: stfld object Extensions/'<<StaticExtension>M>d__0'::o
        IL_0014: ldloca.s 0
        IL_0016: ldarg.1
        IL_0017: stfld string Extensions/'<<StaticExtension>M>d__0'::s
        IL_001c: ldloca.s 0
        IL_001e: ldc.i4.m1
        IL_001f: stfld int32 Extensions/'<<StaticExtension>M>d__0'::'<>1__state'
        IL_0024: ldloca.s 0
        IL_0026: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<StaticExtension>M>d__0'::'<>t__builder'
        IL_002b: ldloca.s 0
        IL_002d: call instance void valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::Start<valuetype Extensions/'<<StaticExtension>M>d__0'>(!!0&)
        IL_0032: ldloca.s 0
        IL_0034: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string> Extensions/'<<StaticExtension>M>d__0'::'<>t__builder'
        IL_0039: call instance class [mscorlib]System.Threading.Tasks.Task`1<!0> valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<string>::get_Task()
        IL_003e: ret
    } // end of method Extensions::'<StaticExtension>M'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceProperty_01()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        string P => o.ToString();
    }
}
""";
        var comp = CreateCompilation(src);

        MethodSymbol implementation = comp.GetTypeByMetadataName("Extensions").GetMembers().OfType<MethodSymbol>().Single();
        Assert.True(implementation.IsStatic);
        Assert.Equal(MethodKind.Ordinary, implementation.MethodKind);
        Assert.Equal(1, implementation.ParameterCount);
        AssertEx.Equal("System.String Extensions.<Extension>get_P(System.Object o)", implementation.ToTestDisplayString());
        Assert.True(implementation.IsImplicitlyDeclared);
        Assert.False(implementation.IsExtensionMethod);
        Assert.True(implementation.HasSpecialName);
        Assert.False(implementation.HasRuntimeSpecialName);

        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig specialname 
            instance string get_P () cil managed 
        {
            // Method begins at RVA 0x206f
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::get_P
        // Properties
        .property instance string P()
        {
            .get instance string Extensions/'<>E__0'::get_P()
        }
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>get_P' (
            object o
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0006: ret
    } // end of method Extensions::'<Extension>get_P'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }

    [Fact]
    public void Implementation_InstanceProperty_02()
    {
        var src = """
public static class Extensions
{
    extension(object o)
    {
        string P { get { return o.ToString(); } }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics(); // PROTOTYPE: Consider executing and verifying behavior

        verifier.VerifyTypeIL("Extensions", """
.class public auto ansi abstract sealed beforefieldinit Extensions
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi sealed beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig specialname 
            instance string get_P () cil managed 
        {
            // Method begins at RVA 0x206f
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::get_P
        // Properties
        .property instance string P()
        {
            .get instance string Extensions/'<>E__0'::get_P()
        }
    } // end of class <>E__0
    // Methods
    .method private hidebysig specialname static 
        string '<Extension>get_P' (
            object o
        ) cil managed 
    {
        // Method begins at RVA 0x2067
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0006: ret
    } // end of method Extensions::'<Extension>get_P'
} // end of class Extensions
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }
}
