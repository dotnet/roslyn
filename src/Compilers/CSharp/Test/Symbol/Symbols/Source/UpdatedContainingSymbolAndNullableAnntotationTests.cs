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

            var local1 = model.GetDeclaredSymbol(varDeclarators.First()).GetSymbol<SourceLocalSymbol>();
            var local2 = model.GetDeclaredSymbol(varDeclarators.ElementAt(1)).GetSymbol<SourceLocalSymbol>();
            // Using a different method as the parent is an accurate enough simulation for these tests of equality.
            Symbol m2 = model.GetDeclaredSymbol(root.DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1)).GetSymbol();

            var wrappedLocal1 = UpdatedContainingSymbolAndNullableAnnotationLocal.CreateForTest(local1, m2, TypeWithAnnotations.Create(local1.Type, NullableAnnotation.Annotated));
            var wrappedLocal1a = UpdatedContainingSymbolAndNullableAnnotationLocal.CreateForTest(local1, m2, TypeWithAnnotations.Create(local1.Type, NullableAnnotation.Annotated));
            var wrappedLocal2 = UpdatedContainingSymbolAndNullableAnnotationLocal.CreateForTest(local2, m2, TypeWithAnnotations.Create(local1.Type, NullableAnnotation.NotAnnotated));

            assertEquality(local1, local1, nullableIgnored: true, considerEverything: true);
            assertEquality(local1, wrappedLocal1, nullableIgnored: true, considerEverything: false);
            assertEquality(local1, local2, nullableIgnored: false, considerEverything: false);
            assertEquality(wrappedLocal1, local2, nullableIgnored: false, considerEverything: false);
            assertEquality(wrappedLocal1, wrappedLocal2, nullableIgnored: false, considerEverything: false);
            assertEquality(wrappedLocal1, wrappedLocal1, nullableIgnored: true, considerEverything: true);
            assertEquality(wrappedLocal1, wrappedLocal1a, nullableIgnored: true, considerEverything: true);

            void assertEquality(Symbol symbol1, Symbol symbol2, bool nullableIgnored, bool considerEverything)
            {
                if (considerEverything)
                {
                    Assert.True(nullableIgnored, "If considerEverything is true, nullableIgnored should be true as well.");
                }
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
