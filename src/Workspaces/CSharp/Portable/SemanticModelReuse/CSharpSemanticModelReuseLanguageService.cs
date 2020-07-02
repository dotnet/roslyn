// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SemanticModelReuse;

namespace Microsoft.CodeAnalysis.CSharp.SemanticModelReuse
{
    [ExportLanguageService(typeof(ISemanticModelReuseLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpSemanticModelReuseLanguageService : AbstractSemanticModelReuseLanguageService<
        BaseMethodDeclarationSyntax,
        AccessorDeclarationSyntax,
        PropertyDeclarationSyntax,
        EventDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSemanticModelReuseLanguageService()
        {
        }

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override SyntaxList<AccessorDeclarationSyntax> GetAccessors(EventDeclarationSyntax @event)
            => @event.AccessorList?.Accessors ?? default;

        protected override SyntaxList<AccessorDeclarationSyntax> GetAccessors(PropertyDeclarationSyntax property)
            => property.AccessorList?.Accessors ?? default;

        public override SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node)
        {
            for (SyntaxNode? previous = null, current = node; current != null; previous = current, current = current.Parent)
            {
                // These are the exact types that SemanticModel.TryGetSpeculativeSemanticModelForMethodBody accepts.
                if (current is BaseMethodDeclarationSyntax baseMethod)
                    return previous != null && baseMethod.Body == previous ? baseMethod : null;

                if (current is AccessorDeclarationSyntax accessor)
                    return previous != null && accessor.Body == previous ? accessor : null;
            }

            return null;
        }

        protected override async Task<SemanticModel?> TryGetSpeculativeSemanticModelWorkerAsync(
            SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken)
        {
            var previousRoot = await previousSemanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = await currentBodyNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var previousBodyNode = GetPreviousBodyNode(previousRoot, currentRoot, currentBodyNode);

            if (previousBodyNode is BaseMethodDeclarationSyntax previousBaseMethod &&
                currentBodyNode is BaseMethodDeclarationSyntax currentBaseMethod &&
                previousBaseMethod.Body != null &&
                previousSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(previousBaseMethod.Body.SpanStart, currentBaseMethod, out var speculativeModel))
            {
                return speculativeModel;
            }

            if (previousBodyNode is AccessorDeclarationSyntax previousAccessorDeclaration &&
                currentBodyNode is AccessorDeclarationSyntax currentAccessorDeclaration &&
                previousAccessorDeclaration.Body != null &&
                previousSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(previousAccessorDeclaration.Body.SpanStart, currentAccessorDeclaration, out speculativeModel))
            {
                return speculativeModel;
            }

            return null;
        }
    }
}
