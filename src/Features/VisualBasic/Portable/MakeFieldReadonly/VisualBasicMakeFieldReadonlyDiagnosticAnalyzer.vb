' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMakeFieldReadonlyDiagnosticAnalyzer
        Inherits AbstractMakeFieldReadonlyDiagnosticAnalyzer(Of ConstructorBlockSyntax, LambdaExpressionSyntax)

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeType, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.ModuleBlock)
        End Sub

        Friend Overrides Function CanBeReadonly(semanticModel As SemanticModel, node As SyntaxNode) As Boolean
            Dim assignmentNode = TryCast(node.Parent, AssignmentStatementSyntax)
            If (assignmentNode IsNot Nothing AndAlso assignmentNode.Left Is node) Then
                Return False
            End If

            Dim argumentNode As ArgumentSyntax = TryCast(node.Parent, ArgumentSyntax)
            If (argumentNode IsNot Nothing AndAlso argumentNode.DetermineParameter(semanticModel).IsRefOrOut()) Then
                Return False
            End If

            Return True
        End Function

        Friend Overrides Function IsMemberOfThisInstance(node As SyntaxNode) As Boolean
            ' if it is a qualified name, make sure it is `Me.name`
            Dim memberAccess = TryCast(node.Parent, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return TryCast(memberAccess.Expression, MeExpressionSyntax) IsNot Nothing
            End If

            ' make sure it isn't in an object initializer
            If TryCast(node.Parent.Parent, ObjectCreationInitializerSyntax) IsNot Nothing Then
                Return False
            End If

            Return True
        End Function
    End Class
End Namespace