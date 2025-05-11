// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.CSharp.Rename;

[ExportLanguageService(typeof(IRenameIssuesService), LanguageNames.CSharp), Shared]
internal sealed class CSharpRenameIssuesService : IRenameIssuesService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpRenameIssuesService()
    {
    }

    public bool CheckLanguageSpecificIssues(
        SemanticModel semanticModel, ISymbol symbol, SyntaxToken triggerToken, [NotNullWhen(true)] out string? langError)
    {
        if (triggerToken.IsTypeNamedDynamic() &&
            symbol.Kind == SymbolKind.DynamicType)
        {
            langError = FeaturesResources.You_cannot_rename_this_element;
            return true;
        }

        if (IsTypeNamedVarInVariableOrFieldDeclaration(triggerToken))
        {
            // To check if var in this context is a real type, or the keyword, we need to 
            // speculatively bind the identifier "var". If it returns a symbol, it's a real type,
            // if not, it's the keyword.
            // see bugs 659683 (compiler API) and 659705 (rename/workspace api) for examples
            var symbolForVar = semanticModel.GetSpeculativeSymbolInfo(
                triggerToken.SpanStart,
                triggerToken.Parent!,
                SpeculativeBindingOption.BindAsTypeOrNamespace).Symbol;

            if (symbolForVar == null)
            {
                langError = FeaturesResources.You_cannot_rename_this_element;
                return true;
            }
        }

        langError = null;
        return false;
    }

    private static bool IsTypeNamedVarInVariableOrFieldDeclaration(SyntaxToken token)
    {
        var parent = token.Parent;
        if (parent.IsKind(SyntaxKind.IdentifierName))
        {
            TypeSyntax? declaredType = null;
            if (parent?.Parent is VariableDeclarationSyntax varDecl)
            {
                declaredType = varDecl.Type;
            }
            else if (parent?.Parent is FieldDeclarationSyntax fieldDecl)
            {
                declaredType = fieldDecl.Declaration.Type;
            }

            return declaredType == parent && token.Text == "var";
        }

        return false;
    }
}
