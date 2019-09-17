// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class UpdatedContainingSymbolAndNullableAnntotationTests : CSharpTestBase
    {
        [Fact]
        public void LocalSymbols()
        {
            var source = @"
class C
{
    void M()
    {
        object local1;
        object? local2;
    }

    void M2() {}
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);
            var varDeclarators = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();

            var local1 = (SourceLocalSymbol)model.GetDeclaredSymbol(varDeclarators.First());
            var local2 = (SourceLocalSymbol)model.GetDeclaredSymbol(varDeclarators.ElementAt(1));
            // Using a different method as the parent is an accurate enough simulation for these tests of equality.
            var m2 = (Symbol)model.GetDeclaredSymbol(root.DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1));

            var wrappedLocal1 = new UpdatedContainingSymbolAndNullableAnnotationLocal(local1, m2, TypeWithAnnotations.Create(local1.Type, NullableAnnotation.Annotated));
            var wrappedLocal1a = new UpdatedContainingSymbolAndNullableAnnotationLocal(local1, m2, TypeWithAnnotations.Create(local1.Type, NullableAnnotation.Annotated));
            var wrappedLocal2 = new UpdatedContainingSymbolAndNullableAnnotationLocal(local2, m2, TypeWithAnnotations.Create(local1.Type, NullableAnnotation.NotAnnotated));

            assertEquality(local1, local1, nullableIgnored: true, considerEverything: true);
            assertEquality(local1, wrappedLocal1, nullableIgnored: true, considerEverything: false);
            assertEquality(local1, local2, nullableIgnored: false, considerEverything: false);
            assertEquality(wrappedLocal1, local2, nullableIgnored: false, considerEverything: false);
            assertEquality(wrappedLocal1, wrappedLocal2, nullableIgnored: false, considerEverything: false);
            assertEquality(wrappedLocal1, wrappedLocal1, nullableIgnored: true, considerEverything: true);
            assertEquality(wrappedLocal1, wrappedLocal1a, nullableIgnored: true, considerEverything: true);

            void assertEquality(Symbol symbol1, Symbol symbol2, bool nullableIgnored, bool considerEverything)
            {
                Assert.Equal(nullableIgnored, symbol1.Equals(symbol2));
                Assert.Equal(nullableIgnored, symbol2.Equals(symbol1));

                if (nullableIgnored)
                {
                    Assert.Equal(symbol1.GetHashCode(), symbol2.GetHashCode());
                }

                Assert.Equal(nullableIgnored, symbol1.Equals(symbol2, TypeCompareKind.AllNullableIgnoreOptions));
                Assert.Equal(nullableIgnored, symbol2.Equals(symbol1, TypeCompareKind.AllNullableIgnoreOptions));

                Assert.Equal(considerEverything, symbol1.Equals(symbol2, TypeCompareKind.ConsiderEverything2));
                Assert.Equal(considerEverything, symbol2.Equals(symbol1, TypeCompareKind.ConsiderEverything2));
            }
        }
    }
}
