' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class GetXmlNamespaceExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides Function GetParameterDisplayParts(index As Integer) As ImmutableArray(Of SymbolDisplayPart)
            Select Case index
                Case 0
                    Return ImmutableArray.Create(
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "["),
                        New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, GetParameterName(index)),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "]"))
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_XML_namespace_prefix_to_return_a_System_Xml_Linq_XNamespace_object_for_If_this_is_omitted_the_object_for_the_default_XML_namespace_is_returned
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.xmlNamespacePrefix
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property ParameterCount As Integer = 1

        Public Overrides ReadOnly Property DocumentationText As String =
            VBWorkspaceResources.Returns_the_System_Xml_Linq_XNamespace_object_corresponding_to_the_specified_XML_namespace_prefix

        Public Overrides ReadOnly Property PrefixParts As ImmutableArray(Of SymbolDisplayPart) = ImmutableArray.Create(
            New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "GetXmlNamespace"),
            New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "("))

        Public Overrides ReadOnly Property IncludeAsType As Boolean = True

        Public Overrides ReadOnly Property ReturnTypeMetadataName As String = "System.Xml.Linq.XNamespace"
    End Class
End Namespace
