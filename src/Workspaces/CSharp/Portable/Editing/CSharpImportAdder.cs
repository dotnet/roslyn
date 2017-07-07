﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Editing
{
    [ExportLanguageService(typeof(ImportAdderService), LanguageNames.CSharp), Shared]
    internal class CSharpImportAdder : ImportAdderService
    {
        protected override INamespaceSymbol GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model)
        {
            var name = node as QualifiedNameSyntax;
            if (name != null)
            {
                return GetExplicitNamespaceSymbol(name, name.Left, model);
            }

            var memberAccess = node as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                return GetExplicitNamespaceSymbol(memberAccess, memberAccess.Expression, model);
            }

            return null;
        }

        private INamespaceSymbol GetExplicitNamespaceSymbol(ExpressionSyntax fullName, ExpressionSyntax namespacePart, SemanticModel model)
        {
            // name must refer to something that is not a namespace, but be qualified with a namespace.
            var symbol = model.GetSymbolInfo(fullName).Symbol;
            var nsSymbol = model.GetSymbolInfo(namespacePart).Symbol as INamespaceSymbol;
            if (symbol != null && symbol.Kind != SymbolKind.Namespace && nsSymbol != null)
            {
                // use the symbols containing namespace, and not the potentially less than fully qualified namespace in the full name expression.
                var ns = symbol.ContainingNamespace;
                if (ns != null)
                {
                    return model.Compilation.GetCompilationNamespace(ns);
                }
            }

            return null;
        }
    }
}
