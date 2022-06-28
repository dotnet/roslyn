' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    Friend Module NodeSelectionHelpers
        Friend Async Function GetSelectedMemberDeclarationAsync(context As CodeRefactoringContext) As Task(Of ImmutableArray(Of SyntaxNode))
            ' Members are either methods (or properties with get and set) or fields
            Dim methodMemberDeclarations = Await context.GetRelevantNodesAsync(Of MethodBaseSyntax)().ConfigureAwait(False)
            ' Gets field variable declarations (not including the keywords like Public/Shared, etc), which are not methods
            Dim varDeclarators = Await context.GetRelevantNodesAsync(Of ModifiedIdentifierSyntax)().ConfigureAwait(False)
            ' Field declaration nodes include the keywords, and contain potentially multiple declared members
            Dim fieldMemberDeclarations = Await context.GetRelevantNodesAsync(Of FieldDeclarationSyntax)().ConfigureAwait(False)
            ' put them all together, expanding the field declaration into all possible ModifiedIdentifiers
            Return methodMemberDeclarations.Cast(Of SyntaxNode).
                Concat(varDeclarators.Cast(Of SyntaxNode)).
                Concat(fieldMemberDeclarations.
                    SelectMany(Function(field) field.Declarators).
                    SelectMany(Function(variableDeclarator) variableDeclarator.Names.Cast(Of SyntaxNode))).
                Distinct(). ' GetRelevantNodesAsync can produce duplicates, so we make sure we only have unique ones
                AsImmutable()
        End Function
    End Module
End Namespace

