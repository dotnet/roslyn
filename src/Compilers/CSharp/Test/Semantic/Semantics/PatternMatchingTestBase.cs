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
        protected static readonly MetadataReference[] s_valueTupleRefs = new[] { SystemRuntimeFacadeRef, ValueTupleRef };

        protected IEnumerable<SingleVariableDesignationSyntax> GetPatternDeclarations(SyntaxTree tree, string v)
        {
            return GetPatternDeclarations(tree).Where(d => d.Identifier.ValueText == v);
        }

        protected SingleVariableDesignationSyntax GetPatternDeclaration(SyntaxTree tree, string v)
        {
            return GetPatternDeclarations(tree, v).Single();
        }

        protected IEnumerable<SingleVariableDesignationSyntax> GetPatternDeclarations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Parent.Kind() == SyntaxKind.DeclarationPattern);
        }

        protected static IEnumerable<DiscardDesignationSyntax> GetDiscardDesignations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<DiscardDesignationSyntax>();
        }

        protected static IdentifierNameSyntax GetReference(SyntaxTree tree, string name)
        {
            return GetReferences(tree, name).Single();
        }

        protected static IEnumerable<IdentifierNameSyntax> GetReferences(SyntaxTree tree, string name)
        {
            return tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == name);
        }

        protected static void VerifyModelForDeclarationPattern(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationPattern(model, decl, false, references);
        }

        protected static void VerifyModelForDeclarationPatternWithoutDataFlow(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationPattern(model, decl, false, references);
        }

        protected static void VerifyModelForDeclarationPattern(
            SemanticModel model,
            SingleVariableDesignationSyntax designation,
            bool isShadowed,
            params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(designation);
            Assert.Equal(designation.Identifier.ValueText, symbol.Name);
            Assert.Equal(designation, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)designation));

            var other = model.LookupSymbols(designation.SpanStart, name: designation.Identifier.ValueText).Single();
            if (isShadowed)
            {
                Assert.NotEqual(symbol, other);
            }
            else
            {
                Assert.Same(symbol, other);
            }

            Assert.True(model.LookupNames(designation.SpanStart).Contains(designation.Identifier.ValueText));

            var decl = (DeclarationPatternSyntax)designation.Parent;
            Assert.True(SyntaxFacts.IsInNamespaceOrTypeContext(decl.Type));
            Assert.True(SyntaxFacts.IsInTypeOnlyContext(decl.Type));

            var local = ((SourceLocalSymbol)symbol);
            var type = local.Type.TypeSymbol;
            if (type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(decl.Type).Symbol);
            }
            else
            {
                Assert.Equal(type, model.GetSymbolInfo(decl.Type).Symbol);
            }

            AssertTypeInfo(model, decl.Type, type);

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: designation.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(designation.Identifier.ValueText));
            }
        }

        private static void AssertTypeInfo(SemanticModel model, TypeSyntax typeSyntax, TypeSymbol expectedType)
        {
            TypeInfo typeInfo = model.GetTypeInfo(typeSyntax);
            Assert.Equal(expectedType, typeInfo.Type);
            Assert.Equal(expectedType, typeInfo.ConvertedType);
            Assert.Equal(typeInfo, ((CSharpSemanticModel)model).GetTypeInfo(typeSyntax));
            Assert.True(model.GetConversion(typeSyntax).IsIdentity);
        }

        protected static void VerifyModelForDeclarationPatternDuplicateInSameScope(SemanticModel model, SingleVariableDesignationSyntax designation)
        {
            var symbol = model.GetDeclaredSymbol(designation);
            Assert.Equal(designation.Identifier.ValueText, symbol.Name);
            Assert.Equal(designation, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)designation));
            Assert.NotEqual(symbol, model.LookupSymbols(designation.SpanStart, name: designation.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(designation.SpanStart).Contains(designation.Identifier.ValueText));

            var type = ((LocalSymbol)symbol).Type.TypeSymbol;
            var decl = (DeclarationPatternSyntax)designation.Parent;
            if (!decl.Type.IsVar || !type.IsErrorType())
            {
                Assert.Equal(type, model.GetSymbolInfo(decl.Type).Symbol);
            }

            AssertTypeInfo(model, decl.Type, type);
        }

        protected static void VerifyNotAPatternField(SemanticModel model, IdentifierNameSyntax reference)
        {
            var symbol = model.GetSymbolInfo(reference).Symbol;

            Assert.NotEqual(SymbolKind.Field, symbol.Kind);

            Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(reference.SpanStart).Contains(reference.Identifier.ValueText));
        }

        protected static void VerifyNotAPatternLocal(SemanticModel model, IdentifierNameSyntax reference)
        {
            var symbol = model.GetSymbolInfo(reference).Symbol;

            if (symbol.Kind == SymbolKind.Local)
            {
                Assert.NotEqual(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            }

            var other = model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Single();
            Assert.Same(symbol, other);
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
            SingleVariableDesignationSyntax decl,
            params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationField(model, decl, false, references);
        }

        protected static void VerifyModelForDeclarationFieldDuplicate(
            SemanticModel model,
            SingleVariableDesignationSyntax decl,
            params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationField(model, decl, true, references);
        }

        protected static void VerifyModelForDeclarationField(
            SemanticModel model,
            SingleVariableDesignationSyntax designation,
            bool duplicate,
            params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(designation);
            Assert.Equal(designation.Identifier.ValueText, symbol.Name);
            Assert.Equal(SymbolKind.Field, symbol.Kind);
            Assert.Equal(designation, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)designation));

            var symbols = model.LookupSymbols(designation.SpanStart, name: designation.Identifier.ValueText);
            var names = model.LookupNames(designation.SpanStart);

            if (duplicate)
            {
                Assert.True(symbols.Count() > 1);
                Assert.Contains(symbol, symbols);
            }
            else
            {
                Assert.Same(symbol, symbols.Single());
            }

            Assert.Contains(designation.Identifier.ValueText, names);

            DeclarationPatternSyntax decl = (DeclarationPatternSyntax)designation.Parent;
            var local = (FieldSymbol)symbol;
            var typeSyntax = decl.Type;

            Assert.True(SyntaxFacts.IsInNamespaceOrTypeContext(typeSyntax));
            Assert.True(SyntaxFacts.IsInTypeOnlyContext(typeSyntax));

            var type = local.Type.TypeSymbol;
            if (typeSyntax.IsVar && type.IsErrorType())
            {
                Assert.Null(model.GetSymbolInfo(typeSyntax).Symbol);
            }
            else
            {
                Assert.Equal(type, model.GetSymbolInfo(typeSyntax).Symbol);
            }

            AssertTypeInfo(model, decl.Type, type);

            var declarator = designation.Ancestors().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            var inFieldDeclaratorArgumentlist = declarator != null && declarator.Parent.Parent.Kind() != SyntaxKind.LocalDeclarationStatement &&
                                           (declarator.ArgumentList?.Contains(designation)).GetValueOrDefault();

            // this is a declaration site, not a use site.
            Assert.Null(model.GetSymbolInfo(designation).Symbol);
            Assert.Null(model.GetSymbolInfo(designation).Symbol);

            foreach (var reference in references)
            {
                var referenceInfo = model.GetSymbolInfo(reference);
                symbols = model.LookupSymbols(reference.SpanStart, name: designation.Identifier.ValueText);

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
                    Assert.Equal(type, model.GetTypeInfo(reference).Type);
                }

                Assert.True(model.LookupNames(reference.SpanStart).Contains(designation.Identifier.ValueText));
            }

            if (!inFieldDeclaratorArgumentlist)
            {
                var dataFlowParent = designation.FirstAncestorOrSelf<ExpressionSyntax>();

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

        protected static void AssertContainedInDeclaratorArguments(SingleVariableDesignationSyntax decl)
        {
            Assert.True(decl.Ancestors().OfType<VariableDeclaratorSyntax>().First().ArgumentList.Contains(decl));
        }

        protected static void AssertContainedInDeclaratorArguments(params SingleVariableDesignationSyntax[] decls)
        {
            foreach (var decl in decls)
            {
                AssertContainedInDeclaratorArguments(decl);
            }
        }

        protected static void VerifyModelNotSupported(
            SemanticModel model,
            SingleVariableDesignationSyntax designation,
            params IdentifierNameSyntax[] references)
        {
            Assert.Null(model.GetDeclaredSymbol(designation));
            var identifierText = designation.Identifier.ValueText;
            Assert.False(model.LookupSymbols(designation.SpanStart, name: identifierText).Any());

            Assert.False(model.LookupNames(designation.SpanStart).Contains(identifierText));
            var decl = (DeclarationPatternSyntax)designation.Parent;
            Assert.Null(model.GetSymbolInfo(decl.Type).Symbol);

            Assert.Null(model.GetSymbolInfo(designation).Symbol);
            Assert.Null(model.GetTypeInfo(designation).Type);
            Assert.Null(model.GetDeclaredSymbol(designation));

            var symbol = (Symbol)model.GetDeclaredSymbol(designation);

            TypeInfo typeInfo = model.GetTypeInfo(decl.Type);

            if ((object)symbol != null)
            {
                var type = symbol.GetTypeOrReturnType().TypeSymbol;
                Assert.Equal(type, typeInfo.Type);
                Assert.Equal(type, typeInfo.ConvertedType);
            }
            else
            {
                Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
                Assert.Equal(SymbolKind.ErrorType, typeInfo.ConvertedType.Kind);
            }

            Assert.Equal(typeInfo, ((CSharpSemanticModel)model).GetTypeInfo(decl.Type));
            Assert.True(model.GetConversion(decl.Type).IsIdentity);

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

        protected static void AssertNoGlobalStatements(SyntaxTree tree)
        {
            Assert.Empty(tree.GetRoot().DescendantNodes().OfType<GlobalStatementSyntax>());
        }

        #endregion helpers
    }
}
