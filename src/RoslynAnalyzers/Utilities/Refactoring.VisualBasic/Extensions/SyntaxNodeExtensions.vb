' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.Analyzer.Utilities.Extensions

    Friend Module SyntaxNodeExtensions

        <Extension>
        Public Function GetAttributeLists(node As SyntaxNode) As SyntaxList(Of AttributeListSyntax)
            Select Case node.Kind
                Case SyntaxKind.CompilationUnit
                    Return SyntaxFactory.List(DirectCast(node, CompilationUnitSyntax).Attributes.SelectMany(Function(s) s.AttributeLists))
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.ClassStatement
                    Return DirectCast(node, ClassStatementSyntax).AttributeLists
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.StructureStatement
                    Return DirectCast(node, StructureStatementSyntax).AttributeLists
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(node, InterfaceStatementSyntax).AttributeLists
                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement.AttributeLists
                Case SyntaxKind.EnumStatement
                    Return DirectCast(node, EnumStatementSyntax).AttributeLists
                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(node, EnumMemberDeclarationSyntax).AttributeLists
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).AttributeLists
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(node, FieldDeclarationSyntax).AttributeLists
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.ConstructorBlock
                    Return DirectCast(node, MethodBlockBaseSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(node, MethodStatementSyntax).AttributeLists
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(node, SubNewStatementSyntax).AttributeLists
                Case SyntaxKind.Parameter
                    Return DirectCast(node, ParameterSyntax).AttributeLists
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement.AttributeLists
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).AttributeLists
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(node, OperatorBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(node, OperatorStatementSyntax).AttributeLists
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement.AttributeLists
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).AttributeLists
                Case SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock,
                    SyntaxKind.AddHandlerAccessorBlock,
                    SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(node, AccessorBlockSyntax).AccessorStatement.AttributeLists
                Case SyntaxKind.GetAccessorStatement,
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.AddHandlerAccessorStatement,
                    SyntaxKind.RemoveHandlerAccessorStatement,
                    SyntaxKind.RaiseEventAccessorStatement
                    Return DirectCast(node, AccessorStatementSyntax).AttributeLists
                Case Else
                    Return Nothing
            End Select
        End Function

    End Module

End Namespace

