// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class SymbolEqualityTests : CSharpTestBase
    {
        [Fact]
        public void SynthesizedIntrinsicOperatorSymbol()
        {
            var src = @"
class C
{
    void M()
    {
        string s1 = """";
        s1 = s1 + s1;
        string? s2 = null;
        s2 = s2 + s2;
    }
}
";
            var comp = CreateCompilation(src, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var invocations = root.DescendantNodes().OfType<BinaryExpressionSyntax>().ToList();

            var nonNullPlus = (IMethodSymbol)model.GetSymbolInfo(invocations[0]).Symbol;
            var nullPlus = (IMethodSymbol)model.GetSymbolInfo(invocations[1]).Symbol;

            Assert.IsType<SynthesizedIntrinsicOperatorSymbol>(nonNullPlus.GetSymbol());
            Assert.IsType<SynthesizedIntrinsicOperatorSymbol>(nullPlus.GetSymbol());

            Assert.Equal(nonNullPlus, nullPlus);
            Assert.Equal<object>(nonNullPlus, nullPlus);
        }

        [Fact]
        public void ReducedExtensionMethodSymbol()
        {
            var src = @"
public static class Extensions
{
    public static void StringExt(this object o)
    { }
}
class C
{
    void M1()
    {
        string s = """";
        s.StringExt();
    }
    void M2()
    {
        string? s = null;
        s.StringExt();
    }
}";
            var comp = CreateCompilation(src, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (17,9): warning CS8604: Possible null reference argument for parameter 'o' in 'void Extensions.StringExt(object o)'.
                //         s.StringExt();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("o", "void Extensions.StringExt(object o)").WithLocation(17, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            var nonNullStringExt = (IMethodSymbol)model.GetSymbolInfo(invocations[0]).Symbol;
            Assert.NotNull(nonNullStringExt);

            var nullStringExt = (IMethodSymbol)model.GetSymbolInfo(invocations[1]).Symbol;
            Assert.NotNull(nullStringExt);

            Assert.IsType<ReducedExtensionMethodSymbol>(nonNullStringExt.GetSymbol());
            Assert.IsType<ReducedExtensionMethodSymbol>(nullStringExt.GetSymbol());

            Assert.Equal(nonNullStringExt, nullStringExt);
            Assert.Equal<object>(nonNullStringExt, nullStringExt);

            // IMethodSymbol does not have a Reduce method that takes annotations, so
            // there's no way to make a reduced method symbol with annotations in the
            // public API
        }

        [Fact]
        public void LocalFunctionSymbol()
        {
            var src = @"
class C
{
    void M()
    {
        void local<T>(T t) { };
        string s1 = """";
        string? s2 = null;
        local(s1);
        local(s2);
    }
}";
            var comp = CreateCompilation(src, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            var nonNullM = model.GetSymbolInfo(invocations[0]).Symbol;
            var nullM = model.GetSymbolInfo(invocations[1]).Symbol;

            Assert.IsType<ConstructedMethodSymbol>(nonNullM.GetSymbol());
            Assert.IsType<ConstructedMethodSymbol>(nullM.GetSymbol());

            var nonNullOriginal = nonNullM.OriginalDefinition;
            var nullOriginal = nullM.OriginalDefinition;

            Assert.IsType<LocalFunctionSymbol>(nonNullOriginal.GetSymbol());
            Assert.IsType<LocalFunctionSymbol>(nullOriginal.GetSymbol());

            Assert.Equal(nonNullOriginal, nullOriginal);
            Assert.Equal<object>(nonNullOriginal, nullOriginal);
        }

        [Fact]
        public void SubstitutedMethodSymbol()
        {
            var src = @"
class C<T>
{
    public static void M<U>(T t, U u) {}
} 
class B
{
    void M()
    {
        C<string>.M<string>("""", """");
        C<string?>.M<string?>(null, null);
    }
}
";
            var comp = CreateCompilation(src, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            var nonNullM = (IMethodSymbol)model.GetSymbolInfo(invocations[0]).Symbol;
            var nullM = (IMethodSymbol)model.GetSymbolInfo(invocations[1]).Symbol;

            var nonNullSubstituted = nonNullM.ContainingType.GetMembers("M").Single();
            var nullSubstituted = nullM.ContainingType.GetMembers("M").Single();

            Assert.IsType<SubstitutedMethodSymbol>(nonNullSubstituted.GetSymbol());
            Assert.IsType<SubstitutedMethodSymbol>(nullSubstituted.GetSymbol());

            Assert.Equal(nonNullSubstituted, nullSubstituted);
            Assert.Equal<object>(nonNullSubstituted, nullSubstituted);
        }

        [Fact]
        public void Internal_Symbol_Equality()
        {
            var source =
@"
public class A
{
    public static A field1;
    public static A field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var member1 = comp.GetMember("A.field1");
            var member2 = comp.GetMember("A.field2");

            Assert.True(member1.Equals(member1));
            Assert.True(member2.Equals(member2));
            Assert.False(member1.Equals(member2));
            Assert.False(member2.Equals(member1));

            var field1 = (FieldSymbol)member1;
            var field2 = (FieldSymbol)member2;

            Assert.True(field1.Equals(field1));
            Assert.True(field2.Equals(field2));
            Assert.False(field1.Equals(field2));
            Assert.False(field2.Equals(field1));
        }

        [Fact]
        public void Internal_Type_Equality_With_No_Nullability()
        {
            var source =
@"
public class A
{
    public static A field1;
    public static A field2;
}
";
            var comp = (Compilation)CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((IFieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((IFieldSymbol)comp.GetMember("A.field2")).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true
            );
        }

        [Fact]
        public void Internal_Type_Equality_With_Top_Level_Nullability()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A
{
    public static A field1;
    public static A? field2;
}
";
            var comp = (Compilation)CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((IFieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((IFieldSymbol)comp.GetMember("A.field2")).Type;

            VerifyEquality(type1.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), type2.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None),
                expectedDefault: true,
                expectedIncludeNullability: true // We don't consider top-level nullability
                );

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            VerifyEquality(type1, type2.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None),
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            VerifyEquality(type1.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), type2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        public void Internal_Type_Equality_With_Nested_Nullability()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A<T>
{
    public static A<object> field1;
    public static A<object?> field2;
}
";
            var comp = (Compilation)CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((IFieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((IFieldSymbol)comp.GetMember("A.field2")).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: false // nested nullability is different
                );
        }

        [Fact]
        public void Internal_Type_Equality_With_Same_Nested_Nullability()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A<T>
{
    public static A<object?> field1;
    public static A<object?> field2;
}
";
            var comp = (Compilation)CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((IFieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((IFieldSymbol)comp.GetMember("A.field2")).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true // nested nullability is the same
                );
        }

        [Fact]
        public void Internal_Type_Equality_From_Metadata()
        {
            var source1 =
@"
#nullable enable
#pragma warning disable 8618
public class A<T>
{
    public static A<object?> field1;
}
";
            var comp1 = (Compilation)CreateCompilation(source1);
            comp1.VerifyDiagnostics();

            var source2 =
@"
#nullable enable
#pragma warning disable 8618
public class B
{
    public static A<object?> field2;
}
";
            var comp2 = (Compilation)CreateCompilation(source2, new[] { new CSharpCompilationReference((CSharpCompilation)comp1) });
            comp2.VerifyDiagnostics();


            var type1comp1 = ((IFieldSymbol)comp1.GetMember("A.field1")).Type;
            var type1comp2 = ((IFieldSymbol)comp2.GetMember("A.field1")).Type;
            var type2 = ((IFieldSymbol)comp2.GetMember("B.field2")).Type;

            VerifyEquality(type1comp1, type1comp2,
                expectedDefault: true,
                expectedIncludeNullability: true
                );

            VerifyEquality(type1comp1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true
                );

            VerifyEquality(type1comp2, type2,
                expectedDefault: true,
                expectedIncludeNullability: true
                );
        }

        [Fact]
        public void Internal_Symbol_Equality_With_Null()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A<T>
{
    public static A<object?> field1;
}
";
            var comp = (Compilation)CreateCompilation(source);
            comp.VerifyDiagnostics();

            var symbol1 = ((IFieldSymbol)comp.GetMember("A.field1")).Type;
            ISymbol symbol2 = null;
            ISymbol symbol3 = null;

            Assert.False(SymbolEqualityComparer.Default.Equals(symbol1, symbol2));
            Assert.False(SymbolEqualityComparer.Default.Equals(symbol2, symbol1));
            Assert.NotEqual(SymbolEqualityComparer.Default.GetHashCode(symbol1), SymbolEqualityComparer.Default.GetHashCode(symbol2));
            Assert.False(SymbolEqualityComparer.ConsiderEverything.Equals(symbol1, symbol2));
            Assert.False(SymbolEqualityComparer.ConsiderEverything.Equals(symbol2, symbol1));
            Assert.NotEqual(SymbolEqualityComparer.ConsiderEverything.GetHashCode(symbol1), SymbolEqualityComparer.ConsiderEverything.GetHashCode(symbol2));

            Assert.True(SymbolEqualityComparer.Default.Equals(symbol2, symbol3));
            Assert.True(SymbolEqualityComparer.Default.Equals(symbol3, symbol2));
            Assert.Equal(SymbolEqualityComparer.Default.GetHashCode(symbol2), SymbolEqualityComparer.Default.GetHashCode(symbol3));
            Assert.True(SymbolEqualityComparer.ConsiderEverything.Equals(symbol2, symbol3));
            Assert.True(SymbolEqualityComparer.ConsiderEverything.Equals(symbol3, symbol2));
            Assert.Equal(SymbolEqualityComparer.ConsiderEverything.GetHashCode(symbol2), SymbolEqualityComparer.ConsiderEverything.GetHashCode(symbol3));
        }

        [Fact]
        public void SemanticModel_Symbol_Equality()
        {
            var source =
@"
public class A
{
    public static A field1;
    public static A field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var member1 = model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0]);
            var member2 = model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0]);

            Assert.True(member1.Equals(member1));
            Assert.True(member2.Equals(member2));
            Assert.False(member1.Equals(member2));
            Assert.False(member2.Equals(member1));

            var field1 = (IFieldSymbol)member1;
            var field2 = (IFieldSymbol)member2;

            Assert.True(field1.Equals(field1));
            Assert.True(field2.Equals(field2));
            Assert.False(field1.Equals(field2));
            Assert.False(field2.Equals(field1));
        }

        [Fact]
        public void SemanticModel_Type_Equality_With_No_Nullability()
        {
            var source =
@"
public class A
{
    public static A field1;
    public static A field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var type1 = ((IFieldSymbol)model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0])).Type;
            var type2 = ((IFieldSymbol)model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0])).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true
            );
        }

        [Fact]
        public void SemanticModel_Type_Equality_With_Top_Level_Nullability()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A
{
    public static A field1;
    public static A? field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var type1 = ((IFieldSymbol)model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0])).Type;
            var type2 = ((IFieldSymbol)model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0])).Type;

            VerifyEquality(type1.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), type2.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None),
                expectedDefault: true,
                expectedIncludeNullability: true // We don't consider top-level nullability
                );

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            VerifyEquality(type1, type2.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None),
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            VerifyEquality(type1.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), type2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        public void SemanticModel_Type_Equality_With_Nested_Nullability()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A<T>
{
    public static A<object> field1;
    public static A<object?> field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var type1 = ((IFieldSymbol)model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0])).Type;
            var type2 = ((IFieldSymbol)model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0])).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: false // nested nullability is different
                );
        }

        [Fact]
        public void SemanticModel_Type_Equality_With_Same_Nested_Nullability()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A<T>
{
    public static A<object?> field1;
    public static A<object?> field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var type1 = ((IFieldSymbol)model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0])).Type;
            var type2 = ((IFieldSymbol)model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0])).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true // nested nullability is the same
                );
        }

        [Fact]
        public void SemanticModel_Method_Equality()
        {
            var source =
@"
#nullable enable
public class A
{
    public static T Create<T> (T t) => t;

    public static void M(string? x)
    {
        _ = Create(x);
        if (x is null) return;
        _ = Create(x);
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var create1Syntax = (InvocationExpressionSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.InvocationExpression);
            var create2Syntax = (InvocationExpressionSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.InvocationExpression);

            var model = comp.GetSemanticModel(syntaxTree);

            var create1Symbol = model.GetSymbolInfo(create1Syntax).Symbol;
            var create2Symbol = model.GetSymbolInfo(create2Syntax).Symbol;

            VerifyEquality(create1Symbol, create2Symbol,
                expectedDefault: true,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        public void SemanticModel_Property_Equality()
        {
            var source =
@"
#nullable enable
public class A<T>
{
    public static A<T> Property => throw null!;
}
public class B
{
    public A<string> field1 = new A<string>();
    public A<string?> field2 = new A<string?>();
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();

            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var type1 = ((IFieldSymbol)model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0])).Type;
            var type2 = ((IFieldSymbol)model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0])).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            var property1 = (IPropertySymbol)type1.GetMembers()[0];
            var property2 = (IPropertySymbol)type2.GetMembers()[0];

            VerifyEquality(property1, property2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            var prop1Type = property1.Type;
            var prop2Type = property2.Type;

            VerifyEquality(prop1Type, prop2Type,
                expectedDefault: true,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        public void SemanticModel_Field_Equality()
        {
            var source =
@"
#nullable enable
public class A<T>
{
    public static A<T> field = null!;
}
public class B
{
    public A<string> field1 = new A<string>();
    public A<string?> field2 = new A<string?>();
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();

            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.ClassDeclaration).DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.ClassDeclaration).DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var type1 = ((IFieldSymbol)model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0])).Type;
            var type2 = ((IFieldSymbol)model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0])).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            var field1 = (IFieldSymbol)type1.GetMembers()[0];
            var field2 = (IFieldSymbol)type2.GetMembers()[0];

            VerifyEquality(field1, field2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            var prop1Type = field1.Type;
            var prop2Type = field2.Type;

            VerifyEquality(prop1Type, prop2Type,
                expectedDefault: true,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        public void SemanticModel_Event_Equality()
        {
            var source =
@"
#nullable enable
#pragma warning disable 8618
public class A<T>
{
    public static event System.EventHandler<T> MyEvent;

    public static void Invoke() => MyEvent(default, default!);
}
public class B
{
    public A<string> field1 = new A<string>();
    public A<string?> field2 = new A<string?>();
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();

            var member1Syntax = (FieldDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.FieldDeclaration);
            var member2Syntax = (FieldDeclarationSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.FieldDeclaration);

            var model = comp.GetSemanticModel(syntaxTree);

            var type1 = ((IFieldSymbol)model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0])).Type;
            var type2 = ((IFieldSymbol)model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0])).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            var event1 = (IEventSymbol)type1.GetMembers()[2];
            var event2 = (IEventSymbol)type2.GetMembers()[2];

            VerifyEquality(event1, event2,
                expectedDefault: true,
                expectedIncludeNullability: false
                );

            var prop1Type = event1.Type;
            var prop2Type = event2.Type;

            VerifyEquality(prop1Type, prop2Type,
                expectedDefault: true,
                expectedIncludeNullability: false
                );
        }

        private void VerifyEquality(ISymbol type1, ISymbol type2, bool expectedDefault, bool expectedIncludeNullability)
        {
            // Symbol.Equals
            Assert.True(type1.Equals(type1));
            Assert.True(type2.Equals(type2));
            Assert.Equal(expectedDefault, type1.Equals(type2));
            Assert.Equal(expectedDefault, type2.Equals(type1));

            // TypeSymbol.Equals - Default
            Assert.True(type1.Equals(type1, SymbolEqualityComparer.Default));
            Assert.True(type2.Equals(type2, SymbolEqualityComparer.Default));
            Assert.Equal(expectedDefault, type1.Equals(type2, SymbolEqualityComparer.Default));
            Assert.Equal(expectedDefault, type2.Equals(type1, SymbolEqualityComparer.Default));

            // TypeSymbol.Equals - IncludeNullability
            Assert.True(type1.Equals(type1, SymbolEqualityComparer.IncludeNullability));
            Assert.True(type2.Equals(type2, SymbolEqualityComparer.IncludeNullability));
            Assert.Equal(expectedIncludeNullability, type1.Equals(type2, SymbolEqualityComparer.IncludeNullability));
            Assert.Equal(expectedIncludeNullability, type2.Equals(type1, SymbolEqualityComparer.IncludeNullability));

            // GetHashCode
            Assert.Equal(type1.GetHashCode(), type2.GetHashCode());
            Assert.Equal(SymbolEqualityComparer.Default.GetHashCode(type1), SymbolEqualityComparer.Default.GetHashCode(type2));
            Assert.Equal(SymbolEqualityComparer.IncludeNullability.GetHashCode(type1), SymbolEqualityComparer.IncludeNullability.GetHashCode(type2));
        }
    }
}
