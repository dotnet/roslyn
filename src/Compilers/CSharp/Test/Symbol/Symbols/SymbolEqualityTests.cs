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

            Assert.Equal(true, member1.Equals(member1));
            Assert.Equal(true, member2.Equals(member2));
            Assert.Equal(false, member1.Equals(member2));
            Assert.Equal(false, member2.Equals(member1));

            var field1 = member1 as FieldSymbol;
            var field2 = member2 as FieldSymbol;

            Assert.Equal(true, field1.Equals(field1));
            Assert.Equal(true, field2.Equals(field2));
            Assert.Equal(false, field1.Equals(field2));
            Assert.Equal(false, field2.Equals(field1));
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

            var type1 = (comp.GetMember("A.field1") as FieldSymbol).Type;
            var type2 = (comp.GetMember("A.field2") as FieldSymbol).Type;

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

            var type1 = (comp.GetMember("A.field1") as FieldSymbol).Type;
            var type2 = (comp.GetMember("A.field2") as FieldSymbol).Type;

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

            var type1 = (comp.GetMember("A.field1") as FieldSymbol).Type;
            var type2 = (comp.GetMember("A.field2") as FieldSymbol).Type;

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

            var type1 = (comp.GetMember("A.field1") as FieldSymbol).Type;
            var type2 = (comp.GetMember("A.field2") as FieldSymbol).Type;

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


            var type1comp1 = (comp1.GetMember("A.field1") as FieldSymbol).Type;
            var type1comp2 = (comp2.GetMember("A.field1") as FieldSymbol).Type;
            var type2 = (comp2.GetMember("B.field2") as FieldSymbol).Type;

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

            var field1 = member1 as FieldSymbol;
            var field2 = member2 as FieldSymbol;

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

            var type1 = (model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;
            var type2 = (model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;

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

            var type1 = (model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;
            var type2 = (model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;

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

            var type1 = (model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;
            var type2 = (model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;

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

            var type1 = (model.GetDeclaredSymbol(member1Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;
            var type2 = (model.GetDeclaredSymbol(member2Syntax.Declaration.Variables[0]) as IFieldSymbol).Type;

            VerifyEquality(type1, type2,
                expectedDefault: true,
                expectedIncludeNullability: true // nested nullability is the same
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
        }
    }

}
