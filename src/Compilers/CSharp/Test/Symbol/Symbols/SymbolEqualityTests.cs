// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Test.Utilities;
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
            var comp = CreateCompilation(src, options: WithNullableEnable());
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
            var comp = CreateCompilation(src, options: WithNullableEnable());
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
            var comp = CreateCompilation(src, options: WithNullableEnable());
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
            var comp = CreateCompilation(src, options: WithNullableEnable());
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
                expectedIncludeNullability: true // We don't consider top-level nullability
                );

            VerifyEquality(type1, type2,
                expectedIncludeNullability: false
                );

            VerifyEquality(type1, type2.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None),
                expectedIncludeNullability: false
                );

            VerifyEquality(type1.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), type2,
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
                expectedIncludeNullability: true
                );

            VerifyEquality(type1comp1, type2,
                expectedIncludeNullability: true
                );

            VerifyEquality(type1comp2, type2,
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
                expectedIncludeNullability: true // We don't consider top-level nullability
                );

            VerifyEquality(type1, type2,
                expectedIncludeNullability: false
                );

            VerifyEquality(type1, type2.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None),
                expectedIncludeNullability: false
                );

            VerifyEquality(type1.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), type2,
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
                expectedIncludeNullability: false
                );

            var property1 = (IPropertySymbol)type1.GetMembers()[0];
            var property2 = (IPropertySymbol)type2.GetMembers()[0];

            VerifyEquality(property1, property2,
                expectedIncludeNullability: false
                );

            var prop1Type = property1.Type;
            var prop2Type = property2.Type;

            VerifyEquality(prop1Type, prop2Type,
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
                expectedIncludeNullability: false
                );

            var field1 = (IFieldSymbol)type1.GetMembers()[0];
            var field2 = (IFieldSymbol)type2.GetMembers()[0];

            VerifyEquality(field1, field2,
                expectedIncludeNullability: false
                );

            var prop1Type = field1.Type;
            var prop2Type = field2.Type;

            VerifyEquality(prop1Type, prop2Type,
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
                expectedIncludeNullability: false
                );

            var event1 = (IEventSymbol)type1.GetMembers()[2];
            var event2 = (IEventSymbol)type2.GetMembers()[2];

            VerifyEquality(event1, event2,
                expectedIncludeNullability: false
                );

            var prop1Type = event1.Type;
            var prop2Type = event2.Type;

            VerifyEquality(prop1Type, prop2Type,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        [WorkItem(8195, "https://github.com/dotnet/roslyn/issues/38195")]
        public void SemanticModel_SubstitutedField_Equality()
        {
            var source =
@"
#nullable enable
public class A<T> 
    where T : class //not necessary, but makes it easier to reason about the resulting fields
{
    public A<T> field = null!;
    public static void M(A<T> t)
    {
        _ = t.field;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();

            var member1Syntax = (ClassDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.ClassDeclaration);
            var member2Syntax = (IdentifierNameSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.IdentifierName);

            var model = comp.GetSemanticModel(syntaxTree);

            var field1 = (IFieldSymbol)((INamedTypeSymbol)model.GetDeclaredSymbol(member1Syntax)).GetMembers("field").Single(); // A<T!>! A<T>.field
            var field2 = (IFieldSymbol)model.GetSymbolInfo(member2Syntax).Symbol;                                               // A<T!>! A<T!>.field

            VerifyEquality(field1, field2,
                expectedIncludeNullability: false
                );

            var field1Type = field1.Type; // A<T!>
            var field2Type = field2.Type; // A<T!>

            VerifyEquality(field1Type, field2Type,
                expectedIncludeNullability: true
                );

            var field1ContainingType = field1.ContainingType; //A<T>
            var field2ContainingType = field2.ContainingType; //A<T!>

            VerifyEquality(field1ContainingType, field2ContainingType,
                expectedIncludeNullability: false
                );

        }

        [Fact]
        [WorkItem(8195, "https://github.com/dotnet/roslyn/issues/38195")]
        public void SemanticModel_SubstitutedMethod_Equality()
        {
            var source =
@"
#nullable enable
public class A<T> 
    where T : class //not necessary, but makes it easier to reason about the resulting fields
{
    public A<T> M(A<T> t)
    {
        t.M(t);
        return t;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();

            var member1Syntax = (ClassDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.ClassDeclaration);
            var member2Syntax = (IdentifierNameSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.SimpleMemberAccessExpression).DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.IdentifierName);

            var model = comp.GetSemanticModel(syntaxTree);

            var method1 = (IMethodSymbol)((INamedTypeSymbol)model.GetDeclaredSymbol(member1Syntax)).GetMembers("M").Single(); // A<T!>! A<T>.M(A<T!>! t)
            var method2 = (IMethodSymbol)model.GetSymbolInfo(member2Syntax).Symbol;                                           // A<T!>! A<T!>.M(A<T!>! t)

            VerifyEquality(method1, method2,
                expectedIncludeNullability: false
                );

            var method1ReturnType = method1.ReturnType; // A<T!>
            var method2ReturnType = method2.ReturnType; // A<T!>

            VerifyEquality(method1ReturnType, method2ReturnType,
                expectedIncludeNullability: true
                );

            var method1ParamType = method1.Parameters.First().Type; // A<T!>
            var method2ParamType = method2.Parameters.First().Type; // A<T!>

            VerifyEquality(method1ParamType, method2ParamType,
                expectedIncludeNullability: true
                );

            var method1ContainingType = method1.ContainingType; //A<T>
            var method2ContainingType = method2.ContainingType; //A<T!>

            VerifyEquality(method1ContainingType, method2ContainingType,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        [WorkItem(8195, "https://github.com/dotnet/roslyn/issues/38195")]
        public void SemanticModel_SubstitutedEvent_Equality()
        {
            var source =
@"
#nullable enable
public class A<T> 
    where T : class //not necessary, but makes it easier to reason about the resulting fields
{
    public event System.EventHandler<T> MyEvent;
    public static void M(A<T> t)
    {
        _  = t.MyEvent;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,41): warning CS8618: Non-nullable event 'MyEvent' is uninitialized. Consider declaring the event as nullable.
                //     public event System.EventHandler<T> MyEvent;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "MyEvent").WithArguments("event", "MyEvent", "Consider declaring").WithLocation(6, 41)
                );

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();

            var member1Syntax = (ClassDeclarationSyntax)root.DescendantNodes().First(sn => sn.Kind() == SyntaxKind.ClassDeclaration);
            var member2Syntax = (IdentifierNameSyntax)root.DescendantNodes().Last(sn => sn.Kind() == SyntaxKind.IdentifierName);

            var model = comp.GetSemanticModel(syntaxTree);

            var event1 = (IEventSymbol)((INamedTypeSymbol)model.GetDeclaredSymbol(member1Syntax)).GetMembers("MyEvent").Single(); // System.EventHandler<T!>! A<T>.MyEvent
            var event2 = (IEventSymbol)model.GetSymbolInfo(member2Syntax).Symbol;                                                 // System.EventHandler<T!>! A<T!>.MyEvent

            VerifyEquality(event1, event2,
                expectedIncludeNullability: false
                );

            var event1Type = event1.Type; // System.EventHandler<T!>
            var event2Type = event2.Type; // System.EventHandler<T!>

            VerifyEquality(event1Type, event2Type,
                expectedIncludeNullability: true
                );

            var event1ContainingType = event1.ContainingType; //A<T>
            var event2ContainingType = event2.ContainingType; //A<T!>

            VerifyEquality(event1ContainingType, event2ContainingType,
                expectedIncludeNullability: false
                );
        }

        [Fact]
        [WorkItem(58226, "https://github.com/dotnet/roslyn/issues/58226")]
        public void LambdaSymbol()
        {
            var source =
@"
#nullable enable

using System;
using System.Linq;

M1(args => string.Join("" "", args.Select(a => a!.ToString())));
void M1<TResult>(Func<object?[], TResult>? f) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();

            var lambdaSyntax = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().First();
            var semanticModel1 = comp.GetSemanticModel(syntaxTree);
            var semanticModel2 = comp.GetSemanticModel(syntaxTree);

            var lambdaSymbol = (IMethodSymbol)semanticModel1.GetSymbolInfo(lambdaSyntax).Symbol;
            var p1 = lambdaSymbol.Parameters.Single();

            var p2 = semanticModel2.GetDeclaredSymbol(lambdaSyntax.Parameter);
            VerifyEquality(p1, p2, expectedIncludeNullability: true);
        }

        [Fact]
        [WorkItem(58226, "https://github.com/dotnet/roslyn/issues/58226")]
        public void LambdaSymbol_02()
        {
            var source =
@"class Program
{
    static void Main()
    {
        var q = from i in new int[] { 4, 5 } where /*pos*/
    }
}";
            var comp = CreateCompilation(source);
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var syntaxNode = syntaxTree.GetRoot().DescendantNodes().
                OfType<QueryExpressionSyntax>().Single();
            var operation = model.GetOperation(syntaxNode);
            var lambdas = operation.Descendants().OfType<AnonymousFunctionOperation>().
                Select(op => op.Symbol.GetSymbol<LambdaSymbol>()).ToImmutableArray();
            Assert.Equal(2, lambdas.Length);
            Assert.Equal(lambdas[0].SyntaxRef.Span, lambdas[1].SyntaxRef.Span);
            Assert.NotEqual(lambdas[0], lambdas[1]);
        }

        private void VerifyEquality(ISymbol symbol1, ISymbol symbol2, bool expectedIncludeNullability)
        {
            // Symbol.Equals
            Assert.True(symbol1.Equals(symbol1));
            Assert.True(symbol2.Equals(symbol2));
            Assert.True(symbol1.Equals(symbol2));
            Assert.True(symbol2.Equals(symbol1));

            // TypeSymbol.Equals - Default
            Assert.True(symbol1.Equals(symbol1, SymbolEqualityComparer.Default));
            Assert.True(symbol2.Equals(symbol2, SymbolEqualityComparer.Default));
            Assert.True(symbol1.Equals(symbol2, SymbolEqualityComparer.Default));
            Assert.True(symbol2.Equals(symbol1, SymbolEqualityComparer.Default));

            // TypeSymbol.Equals - IncludeNullability
            Assert.True(symbol1.Equals(symbol1, SymbolEqualityComparer.IncludeNullability));
            Assert.True(symbol2.Equals(symbol2, SymbolEqualityComparer.IncludeNullability));
            Assert.Equal(expectedIncludeNullability, symbol1.Equals(symbol2, SymbolEqualityComparer.IncludeNullability));
            Assert.Equal(expectedIncludeNullability, symbol2.Equals(symbol1, SymbolEqualityComparer.IncludeNullability));

            // GetHashCode
            Assert.Equal(symbol1.GetHashCode(), symbol2.GetHashCode());
            Assert.Equal(SymbolEqualityComparer.Default.GetHashCode(symbol1), SymbolEqualityComparer.Default.GetHashCode(symbol2));
            Assert.Equal(SymbolEqualityComparer.IncludeNullability.GetHashCode(symbol1), SymbolEqualityComparer.IncludeNullability.GetHashCode(symbol2));
        }
    }
}
