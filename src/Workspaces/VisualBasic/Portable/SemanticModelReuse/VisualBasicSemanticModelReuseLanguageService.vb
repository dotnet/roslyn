' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.SemanticModelReuse

Namespace Microsoft.CodeAnalysis.VisualBasic.SemanticModelReuse
    Friend Class VisualBasicSemanticModelReuseLanguageService
        Implements ISemanticModelReuseLanguageService

        Public Function TryGetContainingMethodBodyForSpeculation(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing
                Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
                If methodBlock IsNot Nothing Then

                    Return Nothing
                End If

                node = node.Parent
            End While

            Return Nothing
            {
                // These are the exact types that SemanticModel.TryGetSpeculativeSemanticModelForMethodBody accepts.
                If (node Is BaseMethodDeclarationSyntax baseMethod && baseMethod.Body != null)
                    Return node

        If (node Is AccessorDeclarationSyntax accessor && accessor.Body != null)
                    Return node
            }

            Return null
        }

        Public Async Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(
            SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken)
        {
            var previousRoot = await previousSemanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            var currentRoot = await currentBodyNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)

            var previousBodyNode = GetPreviousBodyNode(previousRoot, currentRoot, currentBodyNode)

        If (previousBodyNode Is BaseMethodDeclarationSyntax previousBaseMethod &&
                currentBodyNode Is BaseMethodDeclarationSyntax currentBaseMethod &&
                previousBaseMethod.Body != null &&
                previousSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(previousBaseMethod.Body.SpanStart, currentBaseMethod, out var speculativeModel))
            {
                Return speculativeModel
            }

            If (previousBodyNode Is AccessorDeclarationSyntax previousAccessorDeclaration &&
                currentBodyNode Is AccessorDeclarationSyntax currentAccessorDeclaration &&
                previousAccessorDeclaration.Body != null &&
                previousSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(previousAccessorDeclaration.Body.SpanStart, currentAccessorDeclaration, out speculativeModel))
            {
                Return speculativeModel
            }

            Return null
        }

        Private Static SyntaxNode? GetPreviousBodyNode(SyntaxNode previousRoot, SyntaxNode currentRoot, SyntaxNode currentBodyNode)
        {
            var currentMembers = VisualBasicSyntaxFacts.Instance.GetMethodLevelMembers(currentRoot)
            var index = currentMembers.IndexOf(currentBodyNode Is AccessorDeclarationSyntax
                ? currentBodyNode.Parent!.Parent
                 currentBodyNode)
            If (index < 0)
            {
                Debug.Fail("Unhandled member type in GetPreviousBodyNode")
                Return null
            }

            var previousMembers = VisualBasicSyntaxFacts.Instance.GetMethodLevelMembers(previousRoot)
        If (currentMembers.Count!= previousMembers.Count)
            {
                Debug.Fail("Member count shouldn't have changed as there were no top level edits.")
                Return null
            }

            var previousBodyNode = previousMembers[index]
            If (!(currentBodyNode Is AccessorDeclarationSyntax currentAccessor))
        Return previousBodyNode

            // in the case of an accessor, have to find the previous accessor in the previous prop/event corresponding
            // to the current prop/event.
            var previousAccessorList = previousBodyNode switch
            {
                PropertyDeclarationSyntax previousProperty => previousProperty.AccessorList,
                EventDeclarationSyntax previousEvent => previousEvent.AccessorList,
                _ => null,
            }

            If (previousAccessorList == null)
            {
                Debug.Fail("Didn't find a corresponding accessor in the previous tree.")
                Return null
            }

            var accessorIndex = ((AccessorListSyntax)currentAccessor.Parent!).Accessors.IndexOf(currentAccessor)
            Return previousAccessorList.Accessors[accessorIndex]
        }

        //public int GetMethodLevelMemberId(SyntaxNode root, SyntaxNode node)
        //{
        //    Debug.Assert(root.SyntaxTree == node.SyntaxTree)

        //    var currentId = 0
        //    Contract.ThrowIfFalse(TryGetMethodLevelMember(root, (n, i) => n == node, ref currentId, out var currentNode))

        //    Contract.ThrowIfFalse(currentId >= 0)
        //    CheckMemberId(root, node, currentId)
        //    return currentId
        //}

        //public SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId)
        //{
        //    var currentId = 0
        //    if (!TryGetMethodLevelMember(root, (n, i) => i == memberId, ref currentId, out var currentNode))
        //    {
        //        return null
        //    }

        //    Contract.ThrowIfNull(currentNode)
        //    CheckMemberId(root, currentNode, memberId)
        //    return currentNode
        //}

        //private void GetMethodLevelMember(
        //    SyntaxNode node, Func<SyntaxNode, int, bool> predicate, out int resultId, out SyntaxNode? resultNode)
        //{
        //    resultId = 0
        //    resultNode = null

        //    GetMethodLevelMemberWorker(node, predicate, ref resultId, ref resultNode)

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
        //                return true
        //            }

        //            continue
        //        }

        //        if (IsMethodLevelMember(member))
        //        {
        //            if (predicate(member, currentId))
        //            {
        //                currentNode = member
        //                return true
        //            }

        //            currentId++
        //        }
        //    }

        //    currentNode = null
        //    return false
        //}
    }
}
