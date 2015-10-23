// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Progression
{
    [ExportLanguageService(typeof(IProgressionLanguageService), LanguageNames.CSharp), Shared]
    internal partial class CSharpProgressionLanguageService : IProgressionLanguageService
    {
        private static readonly SymbolDisplayFormat s_descriptionFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                           SymbolDisplayMemberOptions.IncludeContainingType,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                              SymbolDisplayParameterOptions.IncludeParamsRefOut |
                              SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_labelFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                           SymbolDisplayMemberOptions.IncludeExplicitInterface,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                              SymbolDisplayParameterOptions.IncludeParamsRefOut |
                              SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            delegateStyle: SymbolDisplayDelegateStyle.NameAndParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public IEnumerable<SyntaxNode> GetTopLevelNodesFromDocument(SyntaxNode root, CancellationToken cancellationToken)
        {
            // We implement this method lazily so we are able to abort as soon as we need to.
            if (!cancellationToken.IsCancellationRequested)
            {
                var nodes = new Stack<SyntaxNode>();
                nodes.Push(root);

                while (nodes.Count > 0)
                {
                    var node = nodes.Pop();
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (node.Kind() == SyntaxKind.ClassDeclaration ||
                            node.Kind() == SyntaxKind.DelegateDeclaration ||
                            node.Kind() == SyntaxKind.EnumDeclaration ||
                            node.Kind() == SyntaxKind.InterfaceDeclaration ||
                            node.Kind() == SyntaxKind.StructDeclaration ||
                            node.Kind() == SyntaxKind.VariableDeclarator ||
                            node.Kind() == SyntaxKind.MethodDeclaration ||
                            node.Kind() == SyntaxKind.PropertyDeclaration)
                        {
                            yield return node;
                        }
                        else
                        {
                            foreach (var child in node.ChildNodes())
                            {
                                nodes.Push(child);
                            }
                        }
                    }
                }
            }
        }

        public string GetDescriptionForSymbol(ISymbol symbol, bool includeContainingSymbol)
        {
            return GetSymbolText(symbol, includeContainingSymbol, s_descriptionFormat);
        }

        public string GetLabelForSymbol(ISymbol symbol, bool includeContainingSymbol)
        {
            return GetSymbolText(symbol, includeContainingSymbol, s_labelFormat);
        }

        private static string GetSymbolText(ISymbol symbol, bool includeContainingSymbol, SymbolDisplayFormat displayFormat)
        {
            var label = symbol.ToDisplayString(displayFormat);

            var typeToShow = GetType(symbol);

            if (typeToShow != null)
            {
                label += " : " + typeToShow.ToDisplayString(s_labelFormat);
            }

            if (includeContainingSymbol && symbol.ContainingSymbol != null)
            {
                label += " (" + symbol.ContainingSymbol.ToDisplayString(s_labelFormat) + ")";
            }

            return label;
        }

        private static ITypeSymbol GetType(ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IEventSymbol f) => f.Type,
                (IFieldSymbol f) => f.ContainingType.TypeKind == TypeKind.Enum ? null : f.Type,
                (IMethodSymbol m) => IncludeReturnType(m) ? m.ReturnType : null,
                (IPropertySymbol p) => p.Type,
                (INamedTypeSymbol n) => n.IsDelegateType() ? n.DelegateInvokeMethod.ReturnType : null,
                _ => null);
        }

        private static bool IncludeReturnType(IMethodSymbol f)
        {
            return f.MethodKind == MethodKind.Ordinary || f.MethodKind == MethodKind.ExplicitInterfaceImplementation;
        }
    }
}
