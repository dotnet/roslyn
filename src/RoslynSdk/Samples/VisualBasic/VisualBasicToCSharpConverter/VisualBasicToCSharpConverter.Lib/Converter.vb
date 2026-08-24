' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis

Namespace VisualBasicToCSharpConverter
    Partial Public Class Converter
        Public Shared Function Convert(
                            tree As SyntaxTree,
                            Optional identifierMap As IDictionary(Of String, String) = Nothing,
                            Optional convertStrings As Boolean = False
                        ) As SyntaxNode

            Return ConvertTree(tree)
        End Function

        Public Shared Function ConvertTree(tree As SyntaxTree) As SyntaxNode
            Return New NodeConvertingVisitor().Visit(tree.GetRoot())
        End Function
    End Class
End Namespace
