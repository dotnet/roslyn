' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Performance
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA1821DiagnosticAnalyzer
        Inherits CA1821DiagnosticAnalyzer(Of SyntaxKind)

        Protected Overrides Function GetCodeBlockEndedAnalyzer() As AbstractCodeBlockEndedAnalyzer
            Return New CodeBlockEndedAnalyzer()
        End Function

        Private NotInheritable Class CodeBlockEndedAnalyzer
            Inherits AbstractCodeBlockEndedAnalyzer

            Protected Overrides Function IsEmptyFinalizer(node As SyntaxNode, model As SemanticModel) As Boolean
                Dim symbol = model.GetDeclaredSymbol(node)
                Dim method As IMethodSymbol = TryCast(symbol, IMethodSymbol)

                ' First, validate the signature
                If (symbol.IsStatic OrElse method.IsAbstract OrElse method.ReturnType.SpecialType <> SpecialType.System_Void OrElse
                method.Parameters.Length > 0 OrElse Not CaseInsensitiveComparison.Equals(method.Name, "Finalize")) Then
                    Return False
                End If

                ' Second, validate that the method overrides Object.Finalize
                ' Otherwise, it is not a genuine finalizer.
                While Not (method.OverriddenMethod Is Nothing)
                    method = method.OverriddenMethod
                End While

                If method.ContainingType.SpecialType = SpecialType.System_Object Then
                    For Each exp In node.DescendantNodes(descendIntoTrivia:=False).OfType(Of StatementSyntax)() _
                        .Where(Function(n) Not n.IsKind(SyntaxKind.SubStatement) AndAlso Not n.IsKind(SyntaxKind.EndSubStatement))

                        ' NOTE: FxCop only checks if there Is any method call within a given destructor to decide an empty finalizer.
                        ' Here in order to minimize false negatives, we conservatively treat it as non-empty finalizer if its body contains any statements.
                        ' But, still conditional methods Like Debug.Fail() will be considered as being empty as FxCop currently does.

                        ' If a method has conditional attributes (e.g., Debug.Assert), Continue.
                        Dim stmt As ExpressionStatementSyntax = TryCast(exp, ExpressionStatementSyntax)
                        If stmt IsNot Nothing AndAlso HasConditionalAttribute(stmt.Expression, model) Then
                            Continue For
                        End If

                        ' TODO: check if a method is reachable.
                        Return False
                    Next

                    Return True
                End If

                Return False
            End Function

            Protected Function HasConditionalAttribute(root As SyntaxNode, model As SemanticModel) As Boolean
                Dim node = TryCast(root, InvocationExpressionSyntax)
                If node IsNot Nothing Then
                    Dim exp = TryCast(node.Expression, MemberAccessExpressionSyntax)
                    If exp IsNot Nothing Then
                        Dim symbol = model.GetSymbolInfo(exp.Name).Symbol
                        If symbol IsNot Nothing AndAlso symbol.GetAttributes().Any(Function(n) n.AttributeClass.Equals(WellKnownTypes.ConditionalAttribute(model.Compilation))) Then
                            Return True
                        End If
                    End If
                End If

                Return False
            End Function
        End Class
    End Class
End Namespace
