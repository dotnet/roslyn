' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings.MoveType
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveType
    <ExportLanguageService(GetType(IMoveTypeService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMoveTypeService
        Inherits AbstractMoveTypeService(Of VisualBasicMoveTypeService, TypeBlockSyntax, NamespaceBlockSyntax, MethodBaseSyntax, CompilationUnitSyntax)

        ''' <summary>
        ''' Determines if the given TypeBlock definition has a partial identifier.
        ''' </summary>
        Protected Overrides Function IsPartial(typeDeclaration As TypeBlockSyntax) As Boolean
            Return typeDeclaration.BlockStatement.Modifiers.Any(SyntaxKind.PartialKeyword)
        End Function

        ''' <summary>
        ''' Gets the TypeBlock node to analyze
        ''' </summary>
        Protected Overrides Function GetNodetoAnalyze(root As SyntaxNode, span As TextSpan) As SyntaxNode
            Dim node = MyBase.GetNodetoAnalyze(root, span)
            If node.IsKind(SyntaxKind.ModuleStatement,
                           SyntaxKind.ClassStatement,
                           SyntaxKind.StructureStatement,
                           SyntaxKind.InterfaceStatement) Then
                Return node.Parent
            End If

            Return Nothing
        End Function
    End Class
End Namespace
