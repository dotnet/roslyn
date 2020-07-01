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
using Microsoft.CodeAnalysis.SemanticModelReuse;

namespace Microsoft.CodeAnalysis.CSharp.SemanticModelReuse
{
    [ExportLanguageService(typeof(ISemanticModelReuseLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpSemanticModelReuseLanguageService : ISemanticModelReuseLanguageService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSemanticModelReuseLanguageService()
        {
        }

        public SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node)
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

        public async Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(
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

        private static SyntaxNode? GetPreviousBodyNode(SyntaxNode previousRoot, SyntaxNode currentRoot, SyntaxNode currentBodyNode)
        {
            var currentMembers = CSharpSyntaxFacts.Instance.GetMethodLevelMembers(currentRoot);
            var index = currentMembers.IndexOf(currentBodyNode is AccessorDeclarationSyntax
                ? currentBodyNode.Parent!.Parent
                : currentBodyNode);
            if (index < 0)
            {
                Debug.Fail("Unhandled member type in GetPreviousBodyNode");
                return null;
            }

            var previousMembers = CSharpSyntaxFacts.Instance.GetMethodLevelMembers(previousRoot);
            if (currentMembers.Count != previousMembers.Count)
            {
                Debug.Fail("Member count shouldn't have changed as there were no top level edits.");
                return null;
            }

            var previousBodyNode = previousMembers[index];
            if (!(currentBodyNode is AccessorDeclarationSyntax currentAccessor))
                return previousBodyNode;

            // in the case of an accessor, have to find the previous accessor in the previous prop/event corresponding
            // to the current prop/event.
            var previousAccessorList = previousBodyNode switch
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

            var accessorIndex = ((AccessorListSyntax)currentAccessor.Parent!).Accessors.IndexOf(currentAccessor);
            return previousAccessorList.Accessors[accessorIndex];
        }

        //public int GetMethodLevelMemberId(SyntaxNode root, SyntaxNode node)
        //{
        //    Debug.Assert(root.SyntaxTree == node.SyntaxTree);

        //    var currentId = 0;
        //    Contract.ThrowIfFalse(TryGetMethodLevelMember(root, (n, i) => n == node, ref currentId, out var currentNode));

        //    Contract.ThrowIfFalse(currentId >= 0);
        //    CheckMemberId(root, node, currentId);
        //    return currentId;
        //}

        //public SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId)
        //{
        //    var currentId = 0;
        //    if (!TryGetMethodLevelMember(root, (n, i) => i == memberId, ref currentId, out var currentNode))
        //    {
        //        return null;
        //    }

        //    Contract.ThrowIfNull(currentNode);
        //    CheckMemberId(root, currentNode, memberId);
        //    return currentNode;
        //}

        //private void GetMethodLevelMember(
        //    SyntaxNode node, Func<SyntaxNode, int, bool> predicate, out int resultId, out SyntaxNode? resultNode)
        //{
        //    resultId = 0;
        //    resultNode = null;

        //    GetMethodLevelMemberWorker(node, predicate, ref resultId, ref resultNode);

        //    static GetMethodLevelMember(SyntaxNode node, Func<SyntaxNode, int, bool> predicate, ref int resultId, ref SyntaxNode? resultNode)
        //    {
        //        foreach (var member in node.)
        //    }

        //    foreach (var member in node.GetMembers())
        //    {
        //        if (IsTopLevelNodeWithMembers(member))
        //        {
        //            if (TryGetMethodLevelMember(member, predicate, ref currentId, out currentNode))
        //            {
        //                return true;
        //            }

        //            continue;
        //        }

        //        if (IsMethodLevelMember(member))
        //        {
        //            if (predicate(member, currentId))
        //            {
        //                currentNode = member;
        //                return true;
        //            }

        //            currentId++;
        //        }
        //    }

        //    currentNode = null;
        //    return false;
        //}
    }
}
