' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    <ExportLanguageService(GetType(IInitializeParameterService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicInitializeParameterService
        Inherits AbstractInitializerParameterService(Of StatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub InsertStatement(editor As SyntaxEditor, functionDeclaration As SyntaxNode, returnsVoid As Boolean, statementToAddAfter As SyntaxNode, statement As StatementSyntax)
            InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, statementToAddAfter, statement)
        End Sub

        Public Overrides Function GetBody(methodNode As SyntaxNode) As SyntaxNode
            Return InitializeParameterHelpers.GetBody(methodNode)
        End Function

        Protected Overrides Function TryGetLastStatement(blockStatement As IBlockOperation) As SyntaxNode
            Return InitializeParameterHelpers.TryGetLastStatement(blockStatement)
        End Function

        Protected Overrides Function TryUpdateTupleAssignment(blockStatement As IBlockOperation, parameter As IParameterSymbol, fieldOrProperty As ISymbol, editor As SyntaxEditor) As Boolean
            ' Not supported in VB
            Return False
        End Function
    End Class
End Namespace
