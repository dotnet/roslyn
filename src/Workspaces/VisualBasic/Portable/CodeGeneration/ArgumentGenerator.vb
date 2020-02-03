' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module ArgumentGenerator
        Public Function GenerateArgument(argument As SyntaxNode) As ArgumentSyntax
            If TypeOf argument Is ExpressionSyntax Then
                Return GenerateArgument(SyntaxFactory.SimpleArgument(DirectCast(argument, ExpressionSyntax)))
            End If

            Return DirectCast(argument, ArgumentSyntax)
        End Function

        Friend Function GenerateArgumentList(arguments As IList(Of SyntaxNode)) As ArgumentListSyntax
            Return SyntaxFactory.ArgumentList(arguments:=SyntaxFactory.SeparatedList(arguments.Select(AddressOf GenerateArgument)))
        End Function
    End Module
End Namespace
