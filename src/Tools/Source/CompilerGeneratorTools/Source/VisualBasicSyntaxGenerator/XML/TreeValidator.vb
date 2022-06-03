' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
' Code to validate various constraints on a parse tree definition after it has been read in. This 
' validates certain constraints that are not validated when the tree is read in. Called after the tree
' is read, but before the output phase.
'-----------------------------------------------------------------------------------------------------------

Friend Module TreeValidator

    Public Const MaxSyntaxKinds = &H400

    ' Do validation on the parse tree and warn about issues.
    Public Sub ValidateTree(tree As ParseTree)
        CheckTokenChildren(tree)
        CheckAbstractness(tree)
        CheckForOrphanStructures(tree)
        CheckKindNames(tree)
    End Sub

    ' If a node has children, it can't be a token or trivia. And vice-versa, if it doesn't
    ' have children (and isn't abstract), it must be a token or trivia.
    Private Sub CheckTokenChildren(tree As ParseTree)
        For Each nodeStructure As ParseNodeStructure In tree.NodeStructures.Values
            Dim hasAnyChildren As Boolean = tree.HasAnyChildren(nodeStructure)

            If hasAnyChildren AndAlso nodeStructure.IsToken Then
                ' Trivia nodes can have children (but don't have to.) Tokens can never have children.
                tree.ReportError(nodeStructure.Element, "ERROR: structure '{0}' has children, but derives from Token", nodeStructure.Name)
            End If
            If Not hasAnyChildren AndAlso Not nodeStructure.Abstract AndAlso Not (nodeStructure.IsToken OrElse nodeStructure.IsTrivia) Then
                tree.ReportError(nodeStructure.Element, "ERROR: structure '{0}' has no children, but doesn't derive from Token or Trivia", nodeStructure.Name)
            End If
        Next
    End Sub

    ' Check that whether nodes are marked abstract matches if they are abstract
    Private Sub CheckAbstractness(tree As ParseTree)
        For Each nodeStructure As ParseNodeStructure In tree.NodeStructures.Values
            If tree.IsAbstract(nodeStructure) And Not nodeStructure.Abstract Then
                tree.ReportError(nodeStructure.Element, "ERROR: parse structure '{0}' has no node-kinds, but is not marked abstract=""true""", nodeStructure.Name)
            ElseIf Not tree.IsAbstract(nodeStructure) And nodeStructure.Abstract Then
                tree.ReportError(nodeStructure.Element, "ERROR: parse structure '{0}' is marked abstract=""true"", but has a node-kind", nodeStructure.Name)
            End If
        Next

    End Sub

    ' Check for structures that are not used at all.
    Private Sub CheckForOrphanStructures(tree As ParseTree)
        Dim referencedStructures As New List(Of ParseNodeStructure)

        For Each nodeKind In tree.NodeKinds.Values
            referencedStructures.Add(nodeKind.NodeStructure)
        Next

        For Each nodeStructure As ParseNodeStructure In tree.NodeStructures.Values
            If nodeStructure.ParentStructure IsNot Nothing Then
                referencedStructures.Add(nodeStructure.ParentStructure)
            End If
        Next

        For Each struct In tree.NodeStructures.Values
            If Not referencedStructures.Contains(struct) Then
                tree.ReportError(struct.Element, "WARNING: parse structure '{0}' has no node kind or derived structure that references it", struct.Name)
            End If
        Next
    End Sub

    ' If a node structure has a single node kind, warn if the node kind doesn't match the
    ' node structure name.
    Private Sub CheckKindNames(tree As ParseTree)
        Dim count = 0
        Dim nodeStructure As ParseNodeStructure
        For Each nodeStructure In tree.NodeStructures.Values
            count += nodeStructure.NodeKinds.Count
            If count > MaxSyntaxKinds Then
                tree.ReportError(nodeStructure.Element, "ERROR: too many node kinds.  Maximum kinds is {0}.", MaxSyntaxKinds)
            End If

            If nodeStructure.NodeKinds.Count = 1 Then
                If nodeStructure.NodeKinds(0).Name <> nodeStructure.Name AndAlso nodeStructure.NodeKinds(0).Name + "Syntax" <> nodeStructure.Name Then
                    tree.ReportError(nodeStructure.Element, "WARNING: node structure '{0}' has a single kind '{1}' with non-matching name", nodeStructure.Name, nodeStructure.NodeKinds(0).Name)
                End If
            End If

        Next
    End Sub
End Module
