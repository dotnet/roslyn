' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCall
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryCallDiagnosticAnalyzer
        Inherits AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(
                diagnosticId:=IDEDiagnosticIds.RemoveUnnecessaryCallDiagnosticId,
                enforceOnBuild:=EnforceOnBuildValues.RemoveUnnecessaryCall,
                [option]:=Nothing,
                fadingOption:=Nothing,
                title:=New LocalizableResourceString(NameOf(VisualBasicAnalyzersResources.Remove_Call), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources)))
        End Sub

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(
                Sub(syntaxContext As SyntaxNodeAnalysisContext)
                    If ShouldSkipAnalysis(syntaxContext, notification:=Nothing) Then
                        Return
                    End If

                    Dim callSyntax = DirectCast(syntaxContext.Node, CallStatementSyntax)
                    Dim root = callSyntax.Invocation
                    Dim rootAsInvocation = TryCast(root, InvocationExpressionSyntax)
                    Dim rootAsMemberAccess As MemberAccessExpressionSyntax
                    Dim rootAsConditionalAccess As ConditionalAccessExpressionSyntax
                    If rootAsInvocation IsNot Nothing Then
                        root = rootAsInvocation.Expression
                    Else
                        rootAsMemberAccess = TryCast(root, MemberAccessExpressionSyntax)
                        If rootAsmemberAccess IsNot Nothing Then
                            root = rootAsMemberAccess.Expression
                        Else
                            rootAsConditionalAccess = TryCast(root, ConditionalAccessExpressionSyntax)
                            If rootAsConditionalAccess IsNot Nothing Then
                                root = rootAsConditionalAccess.Expression
                            Else
                                Debug.Fail(
                                    "Invocation expression node (type: " & root.GetTypeDisplayName() & ") is not " &
                                    NameOf(InvocationExpressionSyntax) & ", " &
                                    NameOf(MemberAccessExpressionSyntax) & ", or " &
                                    NameOf(ConditionalAccessExpressionSyntax) & "."
                                )
                            End If
                        End If
                    End If
                    While True
                        rootAsMemberAccess = TryCast(root, MemberAccessExpressionSyntax)
                        rootAsInvocation = TryCast(root, InvocationExpressionSyntax)
                        rootAsConditionalAccess = TryCast(root, ConditionalAccessExpressionSyntax)
                        If rootAsMemberAccess Is Nothing AndAlso
                            rootAsInvocation Is Nothing AndAlso
                            rootAsConditionalAccess Is Nothing Then
                            Exit While
                        End If
                        root = If(
                            rootAsMemberAccess?.Expression,
                            If(
                                rootAsInvocation?.Expression,
                                rootAsConditionalAccess?.Expression
                            )
                        )
                    End While

                    ' Refer to ParseAssignmentOrInvocationStatement() calls in src\Compilers\VisualBasic\Portable\Parser\Parser.vb
                    ' Adopt syntax kind types that wrap them.
                    If Not root.IsKind(
                        SyntaxKind.IdentifierName, ' Appended
                        SyntaxKind.MyBaseExpression,
                        SyntaxKind.MyClassExpression,
                        SyntaxKind.MeExpression,
                        SyntaxKind.GlobalName,
                        SyntaxKind.PredefinedType,
                        SyntaxKind.PredefinedCastExpression,
                        SyntaxKind.DirectCastExpression,
                        SyntaxKind.TryCastExpression,
                        SyntaxKind.CTypeExpression,
                        SyntaxKind.GetTypeExpression,
                        SyntaxKind.GetXmlNamespaceExpression
                    ) Then
                        Return
                    End If
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptor, callSyntax.CallKeyword.GetLocation(), additionalLocations:={callSyntax.GetLocation()}))
                End Sub, SyntaxKind.CallStatement)
        End Sub

        Public Overrides Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory
            Return DiagnosticAnalyzerCategory.SemanticSpanAnalysis
        End Function
    End Class
End Namespace
