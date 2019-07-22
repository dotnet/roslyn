// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class SymbolEqualityTests : CSharpTestBase
    {
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
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((FieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((FieldSymbol)comp.GetMember("A.field2")).Type;

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
public class A
{
    public static A field1;
    public static A? field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((FieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((FieldSymbol)comp.GetMember("A.field2")).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true // We don't consider top-level nullability
                );

        }

        [Fact]
        public void Internal_Type_Equality_With_Nested_Nullability()
        {
            var source =
@"
#nullable enable
public class A<T>
{
    public static A<object> field1;
    public static A<object?> field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((FieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((FieldSymbol)comp.GetMember("A.field2")).Type;

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
public class A<T>
{
    public static A<object?> field1;
    public static A<object?> field2;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type1 = ((FieldSymbol)comp.GetMember("A.field1")).Type;
            var type2 = ((FieldSymbol)comp.GetMember("A.field2")).Type;

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
public class A<T>
{
    public static A<object?> field1;
}
";
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();

            var source2 =
@"
#nullable enable
public class B
{
    public static A<object?> field2;
}
";
            var comp2 = CreateCompilation(source2, new[] { new CSharpCompilationReference(comp1) });
            comp2.VerifyDiagnostics();


            var type1comp1 = ((FieldSymbol)comp1.GetMember("A.field1")).Type;
            var type1comp2 = ((FieldSymbol)comp2.GetMember("A.field1")).Type;
            var type2 = ((FieldSymbol)comp2.GetMember("B.field2")).Type;

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
public class A<T>
{
    public static A<object?> field1;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var symbol1 = ((FieldSymbol)comp.GetMember("A.field1")).Type;
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

            Assert.Equal(true, member1.Equals(member1));
            Assert.Equal(true, member2.Equals(member2));
            Assert.Equal(false, member1.Equals(member2));
            Assert.Equal(false, member2.Equals(member1));

            var field1 = (FieldSymbol)member1;
            var field2 = (FieldSymbol)member2;

            Assert.Equal(true, field1.Equals(field1));
            Assert.Equal(true, field2.Equals(field2));
            Assert.Equal(false, field1.Equals(field2));
            Assert.Equal(false, field2.Equals(field1));
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

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true // We don't consider top-level nullability
                );

        }

        [Fact]
        public void SemanticModel_Type_Equality_With_Nested_Nullability()
        {
            var source =
@"
#nullable enable
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


        private void VerifyEquality(ISymbol type1, ISymbol type2, bool expectedDefault, bool expectedIncludeNullability)
        {
            // Symbol.Equals
            Assert.Equal(true, type1.Equals(type1));
            Assert.Equal(true, type2.Equals(type2));
            Assert.Equal(expectedDefault, type1.Equals(type2));
            Assert.Equal(expectedDefault, type2.Equals(type1));

            // TypeSymbol.Equals - Default
            Assert.Equal(true, type1.Equals(type1, SymbolEqualityComparer.Default));
            Assert.Equal(true, type2.Equals(type2, SymbolEqualityComparer.Default));
            Assert.Equal(expectedDefault, type1.Equals(type2, SymbolEqualityComparer.Default));
            Assert.Equal(expectedDefault, type2.Equals(type1, SymbolEqualityComparer.Default));

            // TypeSymbol.Equals - IncludeNullability
            Assert.Equal(true, type1.Equals(type1, SymbolEqualityComparer.IncludeNullability));
            Assert.Equal(true, type2.Equals(type2, SymbolEqualityComparer.IncludeNullability));
            Assert.Equal(expectedIncludeNullability, type1.Equals(type2, SymbolEqualityComparer.IncludeNullability));
            Assert.Equal(expectedIncludeNullability, type2.Equals(type1, SymbolEqualityComparer.IncludeNullability));

            // GetHashCode
            Assert.Equal(type1.GetHashCode(), type2.GetHashCode());
            Assert.Equal(SymbolEqualityComparer.Default.GetHashCode(type1), SymbolEqualityComparer.Default.GetHashCode(type2));
            Assert.Equal(SymbolEqualityComparer.IncludeNullability.GetHashCode(type1), SymbolEqualityComparer.IncludeNullability.GetHashCode(type2));
        }
    }

}
