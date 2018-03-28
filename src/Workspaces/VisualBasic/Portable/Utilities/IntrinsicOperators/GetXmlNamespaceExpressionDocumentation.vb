' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class GetXmlNamespaceExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides Function GetParameterDisplayParts(index As Integer) As IList(Of SymbolDisplayPart)
            Select Case index
                Case 0
                    Return {
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "["),
                        New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, GetParameterName(index)),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "]")
                   }
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

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 1
            End Get
        End Property

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.Returns_the_System_Xml_Linq_XNamespace_object_corresponding_to_the_specified_XML_namespace_prefix
            End Get
        End Property

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {
                    New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "GetXmlNamespace"),
                    New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")
                }
            End Get
        End Property

        Public Overrides ReadOnly Property IncludeAsType As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeMetadataName As String
            Get
                Return "System.Xml.Linq.XNamespace"
            End Get
        End Property
    End Class
End Namespace
