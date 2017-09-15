// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.TupleElementNames
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddTupleElementNames), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal partial class TupleElementNamesCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.TupleElementNamesMatchBaseDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    CSharpFeaturesResources.Add_tuple_element_names,
                    cancellationToken => AddTupleElementNamesAsync(context.Document, context.Span, cancellationToken)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        private static async Task<Document> AddTupleElementNamesAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declaration = root.FindNode(span, getInnermostNodeForTie: true);
            var generator = SyntaxGenerator.GetGenerator(document);
            var editor = new SyntaxEditor(root, generator);

            ISymbol symbol;
            TypeSyntax typeDeclaration;
            BaseParameterListSyntax parameterList;
            switch (declaration)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    symbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
                    typeDeclaration = methodDeclaration.ReturnType;
                    parameterList = methodDeclaration.ParameterList;
                    break;

                case IndexerDeclarationSyntax indexerDeclaration:
                    symbol = semanticModel.GetDeclaredSymbol(indexerDeclaration, cancellationToken);
                    typeDeclaration = indexerDeclaration.Type;
                    parameterList = indexerDeclaration.ParameterList;
                    break;

                case BasePropertyDeclarationSyntax basePropertyDeclaration:
                    symbol = semanticModel.GetDeclaredSymbol(basePropertyDeclaration, cancellationToken);
                    typeDeclaration = basePropertyDeclaration.Type;
                    parameterList = null;
                    break;

                case IdentifierNameSyntax identifierName:
                    var variableDeclarator = identifierName.Parent as VariableDeclaratorSyntax;
                    var variableDeclaration = variableDeclarator?.Parent as VariableDeclarationSyntax;
                    if (variableDeclaration == null)
                    {
                        return null;
                    }

                    symbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
                    typeDeclaration = variableDeclaration.Type;
                    parameterList = null;
                    break;

                default:
                    return null;
            }

            var originalSymbol = GetMismatchedDefinition(symbol);
            GetSymbolInformation(originalSymbol, out var originalType, out var originalParameterTypes);

            if (CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(originalType))
            {
                editor.ReplaceNode(typeDeclaration, generator.TypeExpression(originalType));
            }

            for (var i = 0; i < originalParameterTypes.Length; i++)
            {
                if (CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(originalParameterTypes[i]))
                {
                    editor.ReplaceNode(parameterList.Parameters[i].Type, generator.TypeExpression(originalParameterTypes[i]));
                }
            }

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private static ISymbol GetMismatchedDefinition(ISymbol symbol)
        {
            // For this code fix, the mismatched symbol is the first one with any named tuple element
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    var overriddenMethod = ((IMethodSymbol)symbol).OverriddenMethod;
                    if (overriddenMethod != null && CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(overriddenMethod))
                    {
                        return overriddenMethod;
                    }

                    foreach (var implementedInterface in symbol.ContainingType.Interfaces)
                    {
                        foreach (var member in implementedInterface.GetMembers(symbol.Name).OfType<IMethodSymbol>())
                        {
                            if (symbol.ContainingType.FindImplementationForInterfaceMember(member) == symbol)
                            {
                                if (CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(member))
                                {
                                    return member;
                                }
                            }
                        }
                    }

                    return null;

                case SymbolKind.Property:
                    var overriddenProperty = ((IPropertySymbol)symbol).OverriddenProperty;
                    if (overriddenProperty != null && CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(overriddenProperty))
                    {
                        return overriddenProperty;
                    }

                    foreach (var implementedInterface in symbol.ContainingType.Interfaces)
                    {
                        foreach (var member in implementedInterface.GetMembers(symbol.Name).OfType<IPropertySymbol>())
                        {
                            if (symbol.ContainingType.FindImplementationForInterfaceMember(member) == symbol)
                            {
                                if (CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(member))
                                {
                                    return member;
                                }
                            }
                        }
                    }

                    return null;

                case SymbolKind.Event:
                    var overriddenEvent = ((IEventSymbol)symbol).OverriddenEvent;
                    if (overriddenEvent != null && CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(overriddenEvent))
                    {
                        return overriddenEvent;
                    }

                    foreach (var implementedInterface in symbol.ContainingType.Interfaces)
                    {
                        foreach (var member in implementedInterface.GetMembers(symbol.Name).OfType<IEventSymbol>())
                        {
                            if (symbol.ContainingType.FindImplementationForInterfaceMember(member) == symbol)
                            {
                                if (CSharpTupleElementNameDiagnosticAnalyzer.ContainsTupleTypeWithNames(member))
                                {
                                    return member;
                                }
                            }
                        }
                    }

                    return null;

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        private static void GetSymbolInformation(ISymbol symbol, out ITypeSymbol type, out ImmutableArray<ITypeSymbol> parameterTypes)
        {
            switch (symbol)
            {
                case IMethodSymbol methodSymbol:
                    type = methodSymbol.ReturnType;
                    parameterTypes = methodSymbol.Parameters.SelectAsArray(parameter => parameter.Type);
                    return;

                case IPropertySymbol propertySymbol:
                    type = propertySymbol.Type;
                    parameterTypes = propertySymbol.Parameters.SelectAsArray(parameter => parameter.Type);
                    return;

                case IEventSymbol eventSymbol:
                    type = eventSymbol.Type;
                    parameterTypes = ImmutableArray<ITypeSymbol>.Empty;
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
