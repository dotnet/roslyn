' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddExplicitCast
    Partial Friend NotInheritable Class VisualBasicAddExplicitCastCodeFixProvider
        Private Class ArgumentFixer
            Inherits Fixer(Of ArgumentSyntax, ArgumentListSyntax, SyntaxNode)

            Protected Overrides Function GetExpressionOfArgument(argument As ArgumentSyntax) As ExpressionSyntax
                Return argument.GetArgumentExpression()
            End Function

            Protected Overrides Function GenerateNewArgument(oldArgument As ArgumentSyntax, conversionType As ITypeSymbol) As ArgumentSyntax
                Select Case oldArgument.Kind
                    Case SyntaxKind.SimpleArgument
                        Dim simpleArgument = DirectCast(oldArgument, SimpleArgumentSyntax)
                        Return simpleArgument.WithExpression(
                        simpleArgument.GetExpression().Cast(conversionType, Nothing))
                    Case Else
                        Return oldArgument
                End Select
            End Function

            Protected Overrides Function GenerateNewArgumentList(oldArgumentList As ArgumentListSyntax, newArguments As ArrayBuilder(Of ArgumentSyntax)) As ArgumentListSyntax
                Return oldArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments))
            End Function

            Protected Overrides Function GetArgumentsOfArgumentList(argumentList As ArgumentListSyntax) As SeparatedSyntaxList(Of ArgumentSyntax)
                Return argumentList.Arguments
            End Function

            Protected Overrides Function GetSpeculativeSymbolInfo(semanticModel As SemanticModel, newArgumentList As ArgumentListSyntax) As SymbolInfo
                Dim parent = newArgumentList.Parent
                If TypeOf parent Is AttributeSyntax Then
                    Return semanticModel.GetSpeculativeSymbolInfo(parent.SpanStart, DirectCast(parent, AttributeSyntax))
                Else
                    Return semanticModel.GetSpeculativeSymbolInfo(parent.SpanStart, parent, SpeculativeBindingOption.BindAsExpression)
                End If
            End Function
        End Class
    End Class
End Namespace
