// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ConvertTypeOfToNameOf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertTypeOfToNameOf), Shared]
    internal class CSharpConvertTypeOfToNameOfCodeFixProvider : AbstractConvertTypeOfToNameOfCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertTypeOfToNameOfCodeFixProvider()
        {
        }

        protected override string GetCodeFixTitle()
            => CSharpCodeFixesResources.Convert_typeof_to_nameof;

        protected override SyntaxNode? GetSymbolTypeExpression(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is MemberAccessExpressionSyntax { Expression: TypeOfExpressionSyntax typeOfExpression })
            {
                var typeSymbol = model.GetSymbolInfo(typeOfExpression.Type, cancellationToken).Symbol.GetSymbolType();
                return typeSymbol?.GenerateTypeSyntax();
            }

            return null;
        }
    }
}
