' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMakeFieldReadonlyDiagnosticAnalyzer
        Inherits AbstractMakeFieldReadonlyDiagnosticAnalyzer(Of ConstructorBlockSyntax)

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeType, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.ModuleBlock)
        End Sub

        Friend Overrides Function CanBeReadonly(semanticModel As SemanticModel, node As SyntaxNode) As Boolean
            Dim assignmentNode As AssignmentStatementSyntax = TryCast(node.Parent, AssignmentStatementSyntax)
            If (assignmentNode IsNot Nothing AndAlso assignmentNode.Left Is node) Then
                Return False
            End If

            Dim argumentNode As ArgumentSyntax = TryCast(node.Parent, ArgumentSyntax)
            If (argumentNode IsNot Nothing AndAlso argumentNode.DetermineParameter(semanticModel).IsRefOrOut()) Then
                Return False
            End If

            Return True
        End Function
    End Class
End Namespace