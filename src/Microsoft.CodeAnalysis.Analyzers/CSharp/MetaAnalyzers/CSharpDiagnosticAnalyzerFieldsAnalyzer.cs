// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpDiagnosticAnalyzerFieldsAnalyzer : DiagnosticAnalyzerFieldsAnalyzer<ClassDeclarationSyntax, StructDeclarationSyntax, FieldDeclarationSyntax, TypeSyntax, VariableDeclarationSyntax>
    {
        protected override bool IsContainedInFuncOrAction(TypeSyntax typeSyntax, SemanticModel model, ImmutableArray<INamedTypeSymbol> funcs, ImmutableArray<INamedTypeSymbol> actions)
        {
            var current = typeSyntax.Parent;
            while (current is TypeArgumentListSyntax or GenericNameSyntax)
            {
                INamedTypeSymbol? currentSymbol;
                if (current is GenericNameSyntax
                    && (currentSymbol = model.GetSymbolInfo(current).Symbol as INamedTypeSymbol) is not null
                    && (funcs.Contains(currentSymbol.OriginalDefinition, SymbolEqualityComparer.Default) || actions.Contains(currentSymbol.OriginalDefinition, SymbolEqualityComparer.Default)))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }
    }
}
