// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SemanticModelReuse;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SemanticModelReuse
{
    [ExportLanguageService(typeof(ISemanticModelReuseLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpSemanticModelReuseLanguageService : AbstractSemanticModelReuseLanguageService<
        MemberDeclarationSyntax,
        BasePropertyDeclarationSyntax,
        AccessorDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSemanticModelReuseLanguageService()
        {
        }

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override BasePropertyDeclarationSyntax GetBasePropertyDeclaration(AccessorDeclarationSyntax accessor)
        {
            Contract.ThrowIfFalse(accessor.Parent is AccessorListSyntax);
            Contract.ThrowIfFalse(accessor.Parent.Parent is BasePropertyDeclarationSyntax);
            return (BasePropertyDeclarationSyntax)accessor.Parent.Parent;
        }

        protected override SyntaxList<AccessorDeclarationSyntax> GetAccessors(BasePropertyDeclarationSyntax baseProperty)
            => baseProperty.AccessorList!.Accessors;

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

        protected override SemanticModel? TryGetSpeculativeSemanticModelWorker(SemanticModel previousSemanticModel, SyntaxNode previousBodyNode, SyntaxNode currentBodyNode)
        {
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
