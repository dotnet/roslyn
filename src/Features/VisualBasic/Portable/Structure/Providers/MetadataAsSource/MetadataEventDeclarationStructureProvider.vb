' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
    Friend Class MetadataEventDeclarationStructureProvider
        Inherits AbstractMetadataAsSourceStructureProvider(Of EventStatementSyntax)

        Protected Overrides Function GetEndToken(node As EventStatementSyntax) As SyntaxToken
            If node.Modifiers.Count > 0 Then
                Return node.Modifiers.First()
            ElseIf node.CustomKeyword.Kind <> SyntaxKind.None Then
                Return node.CustomKeyword
            Else
                Return node.DeclarationKeyword
            End If
        End Function
    End Class
End Namespace
