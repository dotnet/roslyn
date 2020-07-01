// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics;
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
    internal class CSharpSemanticModelReuseLanguageService : AbstractSemanticModelReuseLanguageService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSemanticModelReuseLanguageService()
        {
        }

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        public override SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                // These are the exact types that SemanticModel.TryGetSpeculativeSemanticModelForMethodBody accepts.
                if (current is BaseMethodDeclarationSyntax baseMethod && baseMethod.Body != null)
                    return current;

                if (current is AccessorDeclarationSyntax accessor && accessor.Body != null)
                    return current;
            }

            return null;
        }

        public override async Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(
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

        protected override SyntaxNode? GetPreviousBodyNode(SyntaxNode previousRoot, SyntaxNode currentRoot, SyntaxNode currentBodyNode)
        {
            if (!(currentBodyNode is AccessorDeclarationSyntax currentAccessor))
                return base.GetPreviousBodyNode(previousRoot, currentRoot, currentBodyNode);

            // for an accessor, we need to find the containing prop/event, find the corresponding member for that,
            // then find the corresponding accessor in that prop/even.
            var currentAccessorList = (AccessorListSyntax)currentAccessor.Parent!;
            var currentPropOrEvent = currentAccessorList.Parent!;

            Debug.Assert(currentPropOrEvent is PropertyDeclarationSyntax || currentPropOrEvent is EventDeclarationSyntax);
            var previousPropOrEvent = base.GetPreviousBodyNode(previousRoot, currentRoot, currentPropOrEvent);

            // in the case of an accessor, have to find the previous accessor in the previous prop/event corresponding
            // to the current prop/event.
            var previousAccessorList = previousPropOrEvent switch
            {
                PropertyDeclarationSyntax previousProperty => previousProperty.AccessorList,
                EventDeclarationSyntax previousEvent => previousEvent.AccessorList,
                _ => null,
            };

            if (previousAccessorList == null)
            {
                Debug.Fail("Didn't find a corresponding accessor in the previous tree.");
                return null;
            }

            var accessorIndex = currentAccessorList.Accessors.IndexOf(currentAccessor);
            return previousAccessorList.Accessors[accessorIndex];
        }
    }
}
