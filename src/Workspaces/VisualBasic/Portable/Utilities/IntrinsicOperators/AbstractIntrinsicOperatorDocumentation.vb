' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend MustInherit Class AbstractIntrinsicOperatorDocumentation
        Public MustOverride ReadOnly Property DocumentationText As String
        Public MustOverride ReadOnly Property ParameterCount As Integer

        Public MustOverride ReadOnly Property IncludeAsType As Boolean

        Public Overridable ReadOnly Property ReturnTypeMetadataName As String
            Get
                Return Nothing
            End Get
        End Property

        Public MustOverride ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
        Public MustOverride Function GetParameterName(index As Integer) As String
        Public MustOverride Function GetParameterDocumentation(index As Integer) As String

        Public Overridable Function GetParameterDisplayParts(index As Integer) As IList(Of SymbolDisplayPart)
            Return {New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, GetParameterName(index))}
        End Function

        Public Overridable Function TryGetTypeNameParameter(syntaxNode As SyntaxNode, index As Integer) As TypeSyntax
            Return Nothing
        End Function

        Public Overridable Function GetSuffix(semanticModel As SemanticModel, position As Integer, nodeToBind As SyntaxNode, cancellationToken As CancellationToken) As IList(Of SymbolDisplayPart)
            Dim suffixParts As New List(Of SymbolDisplayPart)

            If IncludeAsType Then
                suffixParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, ")"))
                suffixParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
                suffixParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "As"))
                suffixParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))

                If nodeToBind IsNot Nothing Then
                    Dim typeInfo = semanticModel.GetTypeInfo(nodeToBind, cancellationToken)
                    If typeInfo.Type IsNot Nothing Then
                        suffixParts.AddRange(typeInfo.Type.ToMinimalDisplayParts(semanticModel, position))
                        Return suffixParts
                    End If
                End If

                If ReturnTypeMetadataName IsNot Nothing Then
                    ' Try getting the return type from the compilation
                    Dim returnType = semanticModel.Compilation.GetTypeByMetadataName(ReturnTypeMetadataName)

                    If returnType IsNot Nothing Then
                        suffixParts.AddRange(returnType.ToMinimalDisplayParts(semanticModel, position))
                    Else
                        suffixParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, ReturnTypeMetadataName))
                    End If

                    Return suffixParts
                End If

                suffixParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, VBWorkspaceResources.result))
            End If

            Return suffixParts
        End Function
    End Class
End Namespace
