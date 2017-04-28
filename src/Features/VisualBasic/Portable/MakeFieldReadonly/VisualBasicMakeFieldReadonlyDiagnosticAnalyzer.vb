' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMakeFieldReadonlyDiagnosticAnalyzer
        Inherits AbstractMakeFieldReadonlyDiagnosticAnalyzer(Of IdentifierNameSyntax, ConstructorBlockSyntax, LambdaExpressionSyntax)

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeType, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.ModuleBlock)
        End Sub

        Protected Overrides Function IsWrittenTo(name As IdentifierNameSyntax, model As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Return name.IsWrittenTo(model, cancellationToken)
        End Function

        Protected Overrides Function IsMemberOfThisInstance(node As SyntaxNode) As Boolean
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