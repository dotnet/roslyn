// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTestBase : CSharpTestBase
    {
        #region helpers
        protected IEnumerable<DeclarationPatternSyntax> GetPatternDeclarations(SyntaxTree tree, string v)
        {
            return GetPatternDeclarations(tree).Where(p => p.Identifier.ValueText == v);
        }

        protected IEnumerable<DeclarationPatternSyntax> GetPatternDeclarations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>();
        }

        protected static IEnumerable<IdentifierNameSyntax> GetReferences(SyntaxTree tree, string name)
        {
            return tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == name);
        }


        protected static void VerifyModelForDeclarationPattern(SemanticModel model, DeclarationPatternSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationPattern(model, decl, false, references);
        }

        protected static void VerifyModelForDeclarationPattern(
            SemanticModel model,
            DeclarationPatternSyntax decl,
            bool isShadowed,
            params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(decl, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));

            if (isShadowed)
            {
                Assert.NotEqual(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            }
            else
            {
                Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            }

            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            Assert.True(SyntaxFacts.IsInNamespaceOrTypeContext(decl.Type));
            Assert.True(SyntaxFacts.IsInTypeOnlyContext(decl.Type));

            var type = ((LocalSymbol)symbol).Type;
            if (!decl.Type.IsVar || !type.IsErrorType())
            {
                Assert.Equal(type, model.GetSymbolInfo(decl.Type).Symbol);
            }

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
            }
        }

        protected static void VerifyModelForDeclarationPatternDuplicateInSameScope(SemanticModel model, DeclarationPatternSyntax decl)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(decl, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.NotEqual(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            var type = ((LocalSymbol)symbol).Type;
            if (!decl.Type.IsVar || !type.IsErrorType())
            {
                Assert.Equal(type, model.GetSymbolInfo(decl.Type).Symbol);
            }
        }

        protected static void VerifyNotAPatternLocal(SemanticModel model, IdentifierNameSyntax reference)
        {
            var symbol = model.GetSymbolInfo(reference).Symbol;

            if (symbol.Kind == SymbolKind.Local)
            {
                Assert.NotEqual(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            }

            Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        protected static void VerifyNotInScope(SemanticModel model, IdentifierNameSyntax reference)
        {
            Assert.Null(model.GetSymbolInfo(reference).Symbol);
            Assert.False(model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Any());
            Assert.False(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        protected static void VerifyModelForDeclarationField(
            SemanticModel model,
            DeclarationPatternSyntax decl,
            params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationField(model, decl, false, references);
        }

        protected static void VerifyModelForDeclarationFieldDuplicate(
            SemanticModel model,
            DeclarationPatternSyntax decl,
            params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationField(model, decl, true, references);
        }

        protected static void VerifyModelForDeclarationField(
            SemanticModel model,
            DeclarationPatternSyntax decl,
            bool duplicate,
            params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(SymbolKind.Field, symbol.Kind);
            Assert.Equal(decl, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));

            var symbols = model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText);
            var names = model.LookupNames(decl.SpanStart);

            if (duplicate)
            {
                Assert.True(symbols.Count() > 1);
                Assert.Contains(symbol, symbols);
            }
            else
            {
                Assert.Same(symbol, symbols.Single());
            }

            Assert.Contains(decl.Identifier.ValueText, names);

            var local = (FieldSymbol)symbol;
            var typeSyntax = decl.Type;

            Assert.True(SyntaxFacts.IsInNamespaceOrTypeContext(typeSyntax));
            Assert.True(SyntaxFacts.IsInTypeOnlyContext(typeSyntax));

            if (typeSyntax.IsVar && local.Type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(typeSyntax).Symbol);
            }
            else
            {
                Assert.Equal(local.Type, model.GetSymbolInfo(typeSyntax).Symbol);
            }

            var declarator = decl.Ancestors().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            var inFieldDeclaratorArgumentlist = declarator != null && declarator.Parent.Parent.Kind() != SyntaxKind.LocalDeclarationStatement &&
                                           (declarator.ArgumentList?.Contains(decl)).GetValueOrDefault();

            // this is a declaration site, not a use site.
            Assert.Null(model.GetSymbolInfo(decl).Symbol);
            Assert.Null(model.GetSymbolInfo(decl).Symbol);

            foreach (var reference in references)
            {
                var referenceInfo = model.GetSymbolInfo(reference);
                symbols = model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText);

                if (duplicate)
                {
                    Assert.Null(referenceInfo.Symbol);
                    Assert.Contains(symbol, referenceInfo.CandidateSymbols);
                    Assert.True(symbols.Count() > 1);
                    Assert.Contains(symbol, symbols);
                }
                else
                {
                    Assert.Same(symbol, referenceInfo.Symbol);
                    Assert.Same(symbol, symbols.Single());
                    Assert.Equal(local.Type, model.GetTypeInfo(reference).Type);
                }

                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
            }

            if (!inFieldDeclaratorArgumentlist)
            {
                var dataFlowParent = decl.FirstAncestorOrSelf<ExpressionSyntax>();

                if (model.IsSpeculativeSemanticModel)
                {
                    Assert.Throws<NotSupportedException>(() => model.AnalyzeDataFlow(dataFlowParent));
                }
                else
                {
                    var dataFlow = model.AnalyzeDataFlow(dataFlowParent);

                    if (dataFlow.Succeeded)
                    {
                        Assert.False(dataFlow.VariablesDeclared.Contains(symbol, ReferenceEqualityComparer.Instance));
                        Assert.False(dataFlow.AlwaysAssigned.Contains(symbol, ReferenceEqualityComparer.Instance));
                        Assert.False(dataFlow.WrittenInside.Contains(symbol, ReferenceEqualityComparer.Instance));
                        Assert.False(dataFlow.DataFlowsIn.Contains(symbol, ReferenceEqualityComparer.Instance));
                        Assert.False(dataFlow.ReadInside.Contains(symbol, ReferenceEqualityComparer.Instance));
                        Assert.False(dataFlow.DataFlowsOut.Contains(symbol, ReferenceEqualityComparer.Instance));
                        Assert.False(dataFlow.ReadOutside.Contains(symbol, ReferenceEqualityComparer.Instance));
                        Assert.False(dataFlow.WrittenOutside.Contains(symbol, ReferenceEqualityComparer.Instance));
                    }
                }
            }
        }

        protected static void AssertContainedInDeclaratorArguments(DeclarationPatternSyntax decl)
        {
            Assert.True(decl.Ancestors().OfType<VariableDeclaratorSyntax>().First().ArgumentList.Contains(decl));
        }

        protected static void AssertContainedInDeclaratorArguments(params DeclarationPatternSyntax[] decls)
        {
            foreach (var decl in decls)
            {
                AssertContainedInDeclaratorArguments(decl);
            }
        }

        protected static void VerifyModelNotSupported(
            SemanticModel model,
            DeclarationPatternSyntax decl,
            params IdentifierNameSyntax[] references)
        {
            Assert.Null(model.GetDeclaredSymbol(decl));
            var identifierText = decl.Identifier.ValueText;
            Assert.False(model.LookupSymbols(decl.SpanStart, name: identifierText).Any());

            Assert.False(model.LookupNames(decl.SpanStart).Contains(identifierText));
            Assert.Null(model.GetSymbolInfo(decl.Type).Symbol);

            Assert.Null(model.GetSymbolInfo(decl).Symbol);
            Assert.Null(model.GetTypeInfo(decl).Type);
            Assert.Null(model.GetDeclaredSymbol(decl));
            VerifyModelNotSupported(model, references);
        }

        protected static void VerifyModelNotSupported(SemanticModel model, params IdentifierNameSyntax[] references)
        {
            foreach (var reference in references)
            {
                Assert.Null(model.GetSymbolInfo(reference).Symbol);
                Assert.False(model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Any());
                Assert.DoesNotContain(reference.Identifier.ValueText, model.LookupNames(reference.SpanStart));
                Assert.True(((TypeSymbol)model.GetTypeInfo(reference).Type).IsErrorType());
            }
        }

        #endregion helpers
    }
}
