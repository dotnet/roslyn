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
        ''' Gets the TypeBlock node to analyze
        ''' </summary>
        Protected Overrides Function GetNodeToAnalyze(root As SyntaxNode, span As TextSpan) As SyntaxNode
            Dim node = MyBase.GetNodeToAnalyze(root, span)
            If node.IsKind(SyntaxKind.ModuleStatement,
                           SyntaxKind.ClassStatement,
                           SyntaxKind.StructureStatement,
                           SyntaxKind.InterfaceStatement,
                           SyntaxKind.EnumStatement) Then
                Return node.Parent
            End If

            Return Nothing
        End Function
    End Class
End Namespace
