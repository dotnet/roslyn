' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
