' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class ArgumentGenerator
        Inherits AbstractVisualBasicCodeGenerator

        Public Shared Function GenerateArgument(argument As SyntaxNode) As ArgumentSyntax
            If TypeOf argument Is ExpressionSyntax Then
                Return GenerateArgument(SyntaxFactory.SimpleArgument(DirectCast(argument, ExpressionSyntax)))
            End If

            Return DirectCast(argument, ArgumentSyntax)
        End Function

        Friend Shared Function GenerateArgumentList(arguments As IList(Of SyntaxNode)) As ArgumentListSyntax
            Return SyntaxFactory.ArgumentList(arguments:=SyntaxFactory.SeparatedList(arguments.Select(AddressOf GenerateArgument)))
        End Function
    End Class
End Namespace