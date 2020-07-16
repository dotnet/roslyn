// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ConvertTypeOfToNameOf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertTypeOfToNameOfCodeFixProvider)), Shared]
    internal class CSharpConvertTypeOfToNameOfCodeFixProvider : AbstractConvertTypeOfToNameOfCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertTypeOfToNameOfCodeFixProvider()
        {
        }

        protected override ITypeSymbol? GetSymbolType(SyntaxNode node, SemanticModel model)
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var expression = memberAccess.Expression;
                if (expression is TypeOfExpressionSyntax typeOfExpresison)
                {
                    return model.GetSymbolInfo(typeOfExpresison.Type).Symbol.GetSymbolType();
                }

                return null;
            }
            return null;
        }
    }
}
