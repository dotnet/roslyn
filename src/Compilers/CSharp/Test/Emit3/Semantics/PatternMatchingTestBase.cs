// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTestBase : CSharpTestBase
    {
        #region helpers
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
            return tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Parent.Kind() == SyntaxKind.DeclarationPattern || p.Parent.Kind() == SyntaxKind.VarPattern);
        }

        protected IEnumerable<VariableDeclaratorSyntax> GetVariableDeclarations(SyntaxTree tree, string v)
        {
            return GetVariableDeclarations(tree).Where(d => d.Identifier.ValueText == v);
        }

        protected IEnumerable<VariableDeclaratorSyntax> GetVariableDeclarations(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
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

        protected static void VerifyModelForDeclarationOrVarSimplePattern(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationOrVarSimplePattern(model, decl, false, references);
        }

        protected static void VerifyModelForDeclarationOrVarSimplePatternWithoutDataFlow(SemanticModel model, SingleVariableDesignationSyntax decl, params IdentifierNameSyntax[] references)
        {
            VerifyModelForDeclarationOrVarSimplePattern(model, decl, false, references);
        }

        protected static void VerifyModelForDeclarationOrVarSimplePattern(
            SemanticModel model,
            SingleVariableDesignationSyntax designation,
            bool isShadowed,
            params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(designation);
            Assert.Equal(designation.Identifier.ValueText, symbol.Name);
            Assert.Equal(designation, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(LocalDeclarationKind.PatternVariable, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
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

            switch (designation.Parent)
            {
                case DeclarationPatternSyntax decl:
                    {
                        var typeSyntax = decl.Type;
                        Assert.True(SyntaxFacts.IsInNamespaceOrTypeContext(typeSyntax));
                        Assert.True(SyntaxFacts.IsInTypeOnlyContext(typeSyntax));

                        var local = ((ILocalSymbol)symbol);
                        var type = local.Type;
                        if (type.IsErrorType())
                        {
                            Assert.Null(model.GetSymbolInfo(typeSyntax).Symbol);
                        }
                        else
                        {
                            Assert.Equal(type, model.GetSymbolInfo(typeSyntax).Symbol);
                        }

                        AssertTypeInfo(model, typeSyntax, type);
                        break;
                    }
            }

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: designation.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(designation.Identifier.ValueText));
            }
        }

        private static void AssertTypeInfo(SemanticModel model, TypeSyntax typeSyntax, ITypeSymbol expectedType)
        {
            TypeInfo typeInfo = model.GetTypeInfo(typeSyntax);
            Assert.Equal(expectedType, typeInfo.Type);
            Assert.Equal(expectedType, typeInfo.ConvertedType);
            Assert.Equal(typeInfo, ((CSharpSemanticModel)model).GetTypeInfo(typeSyntax));
            Assert.True(model.GetConversion(typeSyntax).IsIdentity);
        }

        protected static void VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(SemanticModel model, SingleVariableDesignationSyntax designation)
        {
            var symbol = model.GetDeclaredSymbol(designation);
            Assert.Equal(designation.Identifier.ValueText, symbol.Name);
            Assert.Equal(designation, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(LocalDeclarationKind.PatternVariable, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)designation));
            Assert.NotEqual(symbol, model.LookupSymbols(designation.SpanStart, name: designation.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(designation.SpanStart).Contains(designation.Identifier.ValueText));

            var type = ((ILocalSymbol)symbol).Type;
            switch (designation.Parent)
            {
                case DeclarationPatternSyntax decl:
                    if (!decl.Type.IsVar || !type.IsErrorType())
                    {
                        Assert.Equal(type, model.GetSymbolInfo(decl.Type).Symbol);
                    }
                    AssertTypeInfo(model, decl.Type, type);
                    break;
                case var parent:
                    Assert.True(parent is VarPatternSyntax);
                    break;
            }
        }

        protected static void VerifyModelForDuplicateVariableDeclarationInSameScope(SemanticModel model, VariableDeclaratorSyntax declarator)
        {
            var symbol = model.GetDeclaredSymbol(declarator);
            Assert.Equal(declarator.Identifier.ValueText, symbol.Name);
            Assert.Equal(declarator, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(LocalDeclarationKind.RegularVariable, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)declarator));
            Assert.NotEqual(symbol, model.LookupSymbols(declarator.SpanStart, name: declarator.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(declarator.SpanStart).Contains(declarator.Identifier.ValueText));
        }

        internal static void VerifyModelForDuplicateVariableDeclarationInSameScope(
            SemanticModel model,
            SingleVariableDesignationSyntax designation,
            LocalDeclarationKind kind = LocalDeclarationKind.PatternVariable)
        {
            var symbol = model.GetDeclaredSymbol(designation);
            Assert.Equal(designation.Identifier.ValueText, symbol.Name);
            Assert.Equal(designation, symbol.DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(kind, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)designation));
            Assert.NotEqual(symbol, model.LookupSymbols(designation.SpanStart, name: designation.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(designation.SpanStart).Contains(designation.Identifier.ValueText));
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
                Assert.NotEqual(LocalDeclarationKind.PatternVariable, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
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

            var local = (IFieldSymbol)symbol;
            switch (designation.Parent)
            {
                case DeclarationPatternSyntax decl:
                    var typeSyntax = decl.Type;

                    Assert.True(SyntaxFacts.IsInNamespaceOrTypeContext(typeSyntax));
                    Assert.True(SyntaxFacts.IsInTypeOnlyContext(typeSyntax));

                    var type = local.Type;
                    if (typeSyntax.IsVar && type.IsErrorType())
                    {
                        Assert.Null(model.GetSymbolInfo(typeSyntax).Symbol);
                    }
                    else
                    {
                        Assert.Equal(type, model.GetSymbolInfo(typeSyntax).Symbol);
                    }

                    AssertTypeInfo(model, decl.Type, type);
                    break;
                case var parent:
                    Assert.True(parent is VarPatternSyntax);
                    break;
            }

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
                    Assert.Equal(local.Type, model.GetTypeInfo(reference).Type);
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

        protected static void AssertNotContainedInDeclaratorArguments(SingleVariableDesignationSyntax decl)
            => Assert.Empty(decl.Ancestors().OfType<VariableDeclaratorSyntax>());

        protected static void AssertContainedInDeclaratorArguments(params SingleVariableDesignationSyntax[] decls)
        {
            foreach (var decl in decls)
            {
                AssertContainedInDeclaratorArguments(decl);
            }
        }

        protected static void AssertNotContainedInDeclaratorArguments(params SingleVariableDesignationSyntax[] decls)
        {
            foreach (var decl in decls)
                AssertNotContainedInDeclaratorArguments(decl);
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
            Assert.Null(model.GetSymbolInfo(designation).Symbol);
            Assert.Null(model.GetTypeInfo(designation).Type);
            Assert.Null(model.GetDeclaredSymbol(designation));

            var symbol = (ISymbol)model.GetDeclaredSymbol(designation);

            if (designation.Parent is DeclarationPatternSyntax decl)
            {
                Assert.Null(model.GetSymbolInfo(decl.Type).Symbol);

                TypeInfo typeInfo = model.GetTypeInfo(decl.Type);

                if ((object)symbol != null)
                {
                    var type = symbol.GetTypeOrReturnType();
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
            }
            else if (designation.Parent is VarPatternSyntax varp)
            {
                // Do we want to add any tests for the var pattern?
            }

            VerifyModelNotSupported(model, references);
        }

        protected static void VerifyModelNotSupported(SemanticModel model, params IdentifierNameSyntax[] references)
        {
            foreach (var reference in references)
            {
                Assert.Null(model.GetSymbolInfo(reference).Symbol);
                Assert.False(model.LookupSymbols(reference.SpanStart, name: reference.Identifier.ValueText).Any());
                Assert.DoesNotContain(reference.Identifier.ValueText, model.LookupNames(reference.SpanStart));
                Assert.True(model.GetTypeInfo(reference).Type.IsErrorType());
            }
        }

        protected static void AssertNoGlobalStatements(SyntaxTree tree)
        {
            Assert.Empty(tree.GetRoot().DescendantNodes().OfType<GlobalStatementSyntax>());
        }

        protected CSharpCompilation CreatePatternCompilation(string source, CSharpCompilationOptions options = null)
        {
            return CreateCompilation(new[] { source, _iTupleSource }, options: options ?? TestOptions.DebugExe, parseOptions: TestOptions.RegularWithPatternCombinators);
        }

        protected const string _iTupleSource = @"
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
";
        protected static void AssertEmpty(SymbolInfo info)
        {
            Assert.Equal(default, info);
            Assert.Empty(info.CandidateSymbols);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
        }

        protected static void VerifyDecisionDagDump<T>(Compilation comp, string expectedDecisionDag, int index = 0)
            where T : CSharpSyntaxNode
        {
#if DEBUG
            var tree = comp.SyntaxTrees.First();
            var node = tree.GetRoot().DescendantNodes().OfType<T>().ElementAt(index);
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(node.SpanStart);
            var decisionDag = node switch
            {
                SwitchStatementSyntax n => ((BoundSwitchStatement)binder.BindStatement(n, BindingDiagnosticBag.Discarded)).ReachabilityDecisionDag,
                SwitchExpressionSyntax n => ((BoundSwitchExpression)binder.BindExpression(n, BindingDiagnosticBag.Discarded)).ReachabilityDecisionDag,
                IsPatternExpressionSyntax n => ((BoundIsPatternExpression)binder.BindExpression(n, BindingDiagnosticBag.Discarded)).ReachabilityDecisionDag,
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            };

            AssertEx.Equal(expectedDecisionDag, decisionDag.Dump());
#endif
        }
        #endregion helpers
    }
}
