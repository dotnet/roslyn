' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AliasAmbiguousType
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AliasAmbiguousType

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AliasAmbiguousType), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.FullyQualify)>
    Friend Class VisualBasicAliasAmbiguousTypeCodeFixProvider
        Inherits AbstractAliasAmbiguousTypeCodeFixProvider

        'BC30561: '<name1>' is ambiguous, imported from the namespaces or types '<name2>'
        Private Const BC30561 As String = NameOf(BC30561)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30561)

        Protected Overrides Function GetTextPreviewOfChange(aliasNode As SyntaxNode) As String
            Debug.Assert(TypeOf aliasNode Is ImportsStatementSyntax)
            ' A poor man's name simplifier. For the preview of the context menu text the likely change is predicted by 
            ' removing the global. namespace alias if present. For the majority Of cases this should be the same result
            ' as what the real Simplifier produces in the preview pane And when the fix is applied.
            ' The real Simplifier also removes Module names from the import which is not supported here.
            aliasNode = RemoveGlobalNamespaceAliasIfPresent(aliasNode)
            Return aliasNode.NormalizeWhitespace().ToFullString()
        End Function

        Private Function RemoveGlobalNamespaceAliasIfPresent(aliasNode As SyntaxNode) As SyntaxNode
            Dim importsStatement = CType(aliasNode, ImportsStatementSyntax)
            If importsStatement.ImportsClauses.Count = 1 AndAlso
               TypeOf importsStatement.ImportsClauses(0) Is SimpleImportsClauseSyntax Then
                Dim importsName = CType(importsStatement.ImportsClauses(0), SimpleImportsClauseSyntax).Name
                Dim leftMostNameSyntax = GetLeftmostName(importsName)
                If TypeOf leftMostNameSyntax Is GlobalNameSyntax Then
                    If TypeOf leftMostNameSyntax.Parent Is QualifiedNameSyntax Then
                        Dim parentOfGlobal = CType(leftMostNameSyntax.Parent, QualifiedNameSyntax)
                        Dim replacement = SyntaxFactory.IdentifierName(parentOfGlobal.Right.Identifier)
                        importsStatement = importsStatement.ReplaceNode(parentOfGlobal, replacement)
                    End If
                End If
            End If

            Return importsStatement
        End Function

        Private Function GetLeftmostName(name As NameSyntax) As NameSyntax
            While TypeOf name Is QualifiedNameSyntax
                name = CType(name, QualifiedNameSyntax).Left
            End While

            Return name
        End Function
    End Class
End Namespace
