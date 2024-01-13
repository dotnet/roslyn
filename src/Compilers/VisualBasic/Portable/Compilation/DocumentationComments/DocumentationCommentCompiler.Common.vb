' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Xml
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation

        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            ''' <summary>
            ''' For semantic model scenarios we continue processing documentation comment even in presence
            ''' of some errors. Now, we detect semantic model context from '_isForSingleSymbol' flag,
            ''' later we might consider introducing an explicit flag
            ''' </summary>
            Private ReadOnly Property IsInSemanticModelMode As Boolean
                Get
                    Return Me._isForSingleSymbol
                End Get
            End Property

            Private Function ShouldSkipSymbol(symbol As Symbol) As Boolean
                Return Me._filterSyntaxTree IsNot Nothing AndAlso Not symbol.IsDefinedInSourceTree(Me._filterSyntaxTree, Me._filterSpanWithinTree, Me._cancellationToken)
            End Function

            <Flags>
            Friend Enum WellKnownTag
                None = 0
                C = 1 << 0
                Code = 1 << 1
                Example = 1 << 2
                Exception = 1 << 3
                Include = 1 << 4
                List = 1 << 5
                Para = 1 << 6
                Param = 1 << 7
                ParamRef = 1 << 8
                Permission = 1 << 9
                Remarks = 1 << 10
                Returns = 1 << 11
                See = 1 << 12
                SeeAlso = 1 << 13
                Summary = 1 << 14
                TypeParam = 1 << 15
                TypeParamRef = 1 << 16
                Value = 1 << 17

                AllCollectable = Exception Or
                                 Include Or
                                 Param Or
                                 ParamRef Or
                                 Permission Or
                                 Remarks Or
                                 Returns Or
                                 Summary Or
                                 TypeParam Or
                                 TypeParamRef Or
                                 Value
            End Enum

            Private Shared Function GetElementNameOfWellKnownTag(tag As WellKnownTag) As String
                Select Case tag
                    Case WellKnownTag.C
                        Return DocumentationCommentXmlNames.CElementName
                    Case WellKnownTag.Code
                        Return DocumentationCommentXmlNames.CodeElementName
                    Case WellKnownTag.Example
                        Return DocumentationCommentXmlNames.ExampleElementName
                    Case WellKnownTag.Exception
                        Return DocumentationCommentXmlNames.ExceptionElementName
                    Case WellKnownTag.Include
                        Return DocumentationCommentXmlNames.IncludeElementName
                    Case WellKnownTag.List
                        Return DocumentationCommentXmlNames.ListElementName
                    Case WellKnownTag.Para
                        Return DocumentationCommentXmlNames.ParaElementName
                    Case WellKnownTag.Param
                        Return DocumentationCommentXmlNames.ParameterElementName
                    Case WellKnownTag.ParamRef
                        Return DocumentationCommentXmlNames.ParameterReferenceElementName
                    Case WellKnownTag.Permission
                        Return DocumentationCommentXmlNames.PermissionElementName
                    Case WellKnownTag.Remarks
                        Return DocumentationCommentXmlNames.RemarksElementName
                    Case WellKnownTag.Returns
                        Return DocumentationCommentXmlNames.ReturnsElementName
                    Case WellKnownTag.See
                        Return DocumentationCommentXmlNames.SeeElementName
                    Case WellKnownTag.SeeAlso
                        Return DocumentationCommentXmlNames.SeeAlsoElementName
                    Case WellKnownTag.Summary
                        Return DocumentationCommentXmlNames.SummaryElementName
                    Case WellKnownTag.TypeParam
                        Return DocumentationCommentXmlNames.TypeParameterElementName
                    Case WellKnownTag.TypeParamRef
                        Return DocumentationCommentXmlNames.TypeParameterReferenceElementName
                    Case WellKnownTag.Value
                        Return DocumentationCommentXmlNames.ValueElementName
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(tag)
                End Select
            End Function

            Private Shared Function GetWellKnownTag(elementName As String) As WellKnownTag
                If String.IsNullOrEmpty(elementName) Then
                    Return WellKnownTag.None
                End If

                Select Case elementName
                    Case DocumentationCommentXmlNames.CElementName
                        Return WellKnownTag.C
                    Case DocumentationCommentXmlNames.CodeElementName
                        Return WellKnownTag.Code
                    Case DocumentationCommentXmlNames.ExampleElementName
                        Return WellKnownTag.Example
                    Case DocumentationCommentXmlNames.ExceptionElementName
                        Return WellKnownTag.Exception
                    Case DocumentationCommentXmlNames.IncludeElementName
                        Return WellKnownTag.Include
                    Case DocumentationCommentXmlNames.ListElementName
                        Return WellKnownTag.List
                    Case DocumentationCommentXmlNames.ParaElementName
                        Return WellKnownTag.Para
                    Case DocumentationCommentXmlNames.ParameterElementName
                        Return WellKnownTag.Param
                    Case DocumentationCommentXmlNames.ParameterReferenceElementName
                        Return WellKnownTag.ParamRef
                    Case DocumentationCommentXmlNames.PermissionElementName
                        Return WellKnownTag.Permission
                    Case DocumentationCommentXmlNames.RemarksElementName
                        Return WellKnownTag.Remarks
                    Case DocumentationCommentXmlNames.ReturnsElementName
                        Return WellKnownTag.Returns
                    Case DocumentationCommentXmlNames.SeeElementName
                        Return WellKnownTag.See
                    Case DocumentationCommentXmlNames.SeeAlsoElementName
                        Return WellKnownTag.SeeAlso
                    Case DocumentationCommentXmlNames.SummaryElementName
                        Return WellKnownTag.Summary
                    Case DocumentationCommentXmlNames.TypeParameterElementName
                        Return WellKnownTag.TypeParam
                    Case DocumentationCommentXmlNames.TypeParameterReferenceElementName
                        Return WellKnownTag.TypeParamRef
                    Case DocumentationCommentXmlNames.ValueElementName
                        Return WellKnownTag.Value
                End Select

                Return WellKnownTag.None
            End Function

            Private Sub ReportIllegalWellKnownTagIfAny(tag As WellKnownTag,
                                                       wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                                       symbolName As String)

                ReportIllegalWellKnownTagIfAny(tag, ERRID.WRN_XMLDocIllegalTagOnElement2,
                                               wellKnownElementNodes, GetElementNameOfWellKnownTag(tag), symbolName)
            End Sub

            Private Sub ReportIllegalWellKnownTagIfAny(tag As WellKnownTag,
                                                       errorId As ERRID,
                                                       wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                                       ParamArray args() As Object)

                Dim builder As ArrayBuilder(Of XmlNodeSyntax) = Nothing
                If Not wellKnownElementNodes.TryGetValue(tag, builder) Then
                    Return
                End If

                For Each node In builder
                    If node.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                        Me._diagnostics.Add(errorId, node.GetLocation(), args)
                    End If
                Next
            End Sub

            Private Sub ReportWarningsForDuplicatedTags(wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)), Optional isEvent As Boolean = False)
                Dim nodes As ArrayBuilder(Of XmlNodeSyntax) = Nothing

                ' NOTE: only few well-known tags are being checked for duplications by Dev11

                If wellKnownElementNodes.TryGetValue(WellKnownTag.Include, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.IncludeElementName)
                End If

                ' For events, it is legal to have two or more parameters with the same name,
                ' so we have to allow this. (e.g. Event E1(ByVal x As Integer, ByVal x As Double))
                If Not isEvent AndAlso wellKnownElementNodes.TryGetValue(WellKnownTag.Param, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.ParameterElementName)
                End If

                If wellKnownElementNodes.TryGetValue(WellKnownTag.Permission, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.PermissionElementName)
                End If

                If wellKnownElementNodes.TryGetValue(WellKnownTag.Remarks, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.RemarksElementName)
                End If

                If wellKnownElementNodes.TryGetValue(WellKnownTag.Returns, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.ReturnsElementName)
                End If

                If wellKnownElementNodes.TryGetValue(WellKnownTag.Summary, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.SummaryElementName)
                End If

                If wellKnownElementNodes.TryGetValue(WellKnownTag.TypeParam, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.TypeParameterElementName)
                End If

                If wellKnownElementNodes.TryGetValue(WellKnownTag.Value, nodes) Then
                    ReportWarningsForDuplicatedTags(nodes, DocumentationCommentXmlNames.ValueElementName)
                End If
            End Sub

            Private Structure XmlNodeWithAttributes
                Implements IComparable(Of XmlNodeWithAttributes)

                Public ReadOnly Node As XmlNodeSyntax
                Public ReadOnly Attributes As SortedDictionary(Of String, String)

                Public Sub New(node As XmlNodeSyntax)
                    Me.Node = node
                    Me.Attributes = GetElementAttributes(node)
                End Sub

                Public Shared Function CompareAttributes(a As SortedDictionary(Of String, String), b As SortedDictionary(Of String, String)) As Integer
                    Dim attrCount As Integer = a.Count
                    Dim result As Integer = attrCount.CompareTo(b.Count)
                    If result <> 0 Then
                        Return result
                    End If

                    If attrCount > 0 Then

                        Dim myAttributes = a.GetEnumerator
                        Dim otherAttributes = b.GetEnumerator

                        While myAttributes.MoveNext() AndAlso otherAttributes.MoveNext
                            result = myAttributes.Current.Key.CompareTo(otherAttributes.Current.Key)
                            If result <> 0 Then
                                Return result
                            End If

                            result = myAttributes.Current.Value.CompareTo(otherAttributes.Current.Value)
                            If result <> 0 Then
                                Return result
                            End If
                        End While
                    End If

                    Return 0
                End Function

                Public Function CompareTo(other As XmlNodeWithAttributes) As Integer Implements IComparable(Of XmlNodeWithAttributes).CompareTo
                    Dim result As Integer = CompareAttributes(Me.Attributes, other.Attributes)
                    If result <> 0 Then
                        Return result
                    End If

                    Return If(Me.Node.SpanStart > other.Node.SpanStart, 1, -1)
                End Function
            End Structure

            Private Sub ReportWarningsForDuplicatedTags(nodes As ArrayBuilder(Of XmlNodeSyntax), tagName As String)
                If nodes Is Nothing OrElse nodes.Count < 2 Then
                    Return
                End If

                Dim reportErrors As Boolean = nodes(0).SyntaxTree.ReportDocumentationCommentDiagnostics()

                Dim array = ArrayBuilder(Of XmlNodeWithAttributes).GetInstance
                For i = 0 To nodes.Count - 1
                    array.Add(New XmlNodeWithAttributes(nodes(i)))
                Next
                array.Sort()

                For i = 0 To array.Count - 2
                    Dim node1 As XmlNodeWithAttributes = array(i)
                    Dim node2 As XmlNodeWithAttributes = array(i + 1)

                    If XmlNodeWithAttributes.CompareAttributes(node1.Attributes, node2.Attributes) = 0 Then
                        If reportErrors Then
                            Me._diagnostics.Add(ERRID.WRN_XMLDocDuplicateXMLNode1, node2.Node.GetLocation(), tagName)
                        End If
                    End If
                Next

                array.Free()
            End Sub

            Private Shared Function GetElementAttributes(element As XmlNodeSyntax) As SortedDictionary(Of String, String)
                Dim result As New SortedDictionary(Of String, String)()

                Dim attributes As SyntaxList(Of XmlNodeSyntax) = GetXmlElementAttributes(element)
                For Each node In attributes
                    Dim name As String = Nothing
                    Dim value As String = Nothing

                    Select Case node.Kind
                        Case SyntaxKind.XmlAttribute
                            Dim xmlAttr = DirectCast(node, XmlAttributeSyntax)
                            If xmlAttr.Name.Kind <> SyntaxKind.XmlName OrElse xmlAttr.Value.Kind <> SyntaxKind.XmlString Then
                                Continue For
                            End If

                            name = DirectCast(xmlAttr.Name, XmlNameSyntax).LocalName.ValueText
                            value = Binder.GetXmlString(DirectCast(xmlAttr.Value, XmlStringSyntax).TextTokens)

                        Case SyntaxKind.XmlCrefAttribute
                            Dim crefAttr = DirectCast(node, XmlCrefAttributeSyntax)
                            name = DocumentationCommentXmlNames.CrefAttributeName
                            value = crefAttr.Reference.ToFullString().Trim() ' TODO: revise

                        Case SyntaxKind.XmlNameAttribute
                            Dim nameAttr = DirectCast(node, XmlNameAttributeSyntax)
                            name = DocumentationCommentXmlNames.NameAttributeName
                            value = nameAttr.Reference.Identifier.ToString() ' TODO: revise

                        Case Else
                            Continue For
                    End Select

                    If name IsNot Nothing AndAlso value IsNot Nothing AndAlso Not result.ContainsKey(name) Then
                        result.Add(name, value.Trim())
                    End If
                Next

                Return result
            End Function

            Private Sub ReportWarningsForExceptionTags(wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)))
                Dim builder As ArrayBuilder(Of XmlNodeSyntax) = Nothing
                If wellKnownElementNodes.TryGetValue(WellKnownTag.Exception, builder) Then
                    For Each element In builder
                        Dim crefAttributeFound As Boolean = False

                        Dim attributes As SyntaxList(Of XmlNodeSyntax) = GetXmlElementAttributes(element)
                        For Each attr In attributes
                            Select Case attr.Kind
                                Case SyntaxKind.XmlCrefAttribute
                                    crefAttributeFound = True
                                    Exit For

                                Case SyntaxKind.XmlAttribute
                                    Dim nameAttrName As XmlNodeSyntax = DirectCast(attr, XmlAttributeSyntax).Name
                                    If nameAttrName.Kind = SyntaxKind.XmlName AndAlso
                                        DocumentationCommentXmlNames.AttributeEquals(
                                            DirectCast(nameAttrName, XmlNameSyntax).LocalName.ValueText,
                                            DocumentationCommentXmlNames.CrefAttributeName) Then

                                        crefAttributeFound = True
                                        Exit For
                                    End If
                            End Select
                        Next

                        If Not crefAttributeFound Then
                            If element.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                                Me._diagnostics.Add(ERRID.WRN_XMLDocExceptionTagWithoutCRef, element.GetLocation())
                            End If
                        End If
                    Next
                End If
            End Sub

            Private Sub ReportWarningsForParamAndParamRefTags(wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                                                   symbolName As String, parameters As ImmutableArray(Of ParameterSymbol))

                ' Both references from 'param' and 'paramref' are searched in the symbol only, 
                ' so we may avoid creating a binder and just search in parameters collection itself
                ReportWarningsForParamOrTypeParamTags(wellKnownElementNodes,
                                                      WellKnownTag.Param,
                                                      WellKnownTag.ParamRef,
                                                      symbolName,
                                                      ERRID.WRN_XMLDocBadParamTag2,
                                                      ERRID.WRN_XMLDocParamTagWithoutName,
                                                      parameters)
            End Sub

            Private Sub ReportWarningsForTypeParamTags(wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                                            symbolName As String, typeParameters As ImmutableArray(Of TypeParameterSymbol))

                ' References from 'typeparam' are searched in the symbol, so we may avoid 
                ' creating a binder and just search in type parameters collection itself
                ReportWarningsForParamOrTypeParamTags(wellKnownElementNodes,
                                                      WellKnownTag.TypeParam,
                                                      WellKnownTag.None,
                                                      symbolName,
                                                      ERRID.WRN_XMLDocBadGenericParamTag2,
                                                      ERRID.WRN_XMLDocGenericParamTagWithoutName,
                                                      typeParameters)
            End Sub

            Private Sub ReportWarningsForTypeParamRefTags(wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                                          symbolName As String, symbol As Symbol, tree As SyntaxTree)

                ' In case we have any 'typeparamref' tags, we have to go long way and 
                ' create binder to be able to find containing type's type parameters
                Dim builder As ArrayBuilder(Of XmlNodeSyntax) = Nothing
                If wellKnownElementNodes.TryGetValue(WellKnownTag.TypeParamRef, builder) Then
                    Dim binder = CreateDocumentationCommentBinderForSymbol(Me.Module, symbol, tree, DocumentationCommentBinder.BinderType.NameInTypeParamRef)

                    For Each node In builder
                        Dim nameAttribute As XmlNameAttributeSyntax =
                            GetFirstNameAttributeValue(node,
                                                       symbolName,
                                                       ERRID.WRN_XMLDocBadGenericParamTag2,
                                                       ERRID.ERR_None)

                        If nameAttribute IsNot Nothing Then
                            Dim useSiteInfo = binder.GetNewCompoundUseSiteInfo(Me._diagnostics)
                            Dim bindResult As ImmutableArray(Of Symbol) = binder.BindXmlNameAttributeValue(nameAttribute.Reference, useSiteInfo)

                            If node.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                                Me._diagnostics.Add(node, useSiteInfo)
                            Else
                                Me._diagnostics.AddDependencies(useSiteInfo)
                            End If

                            Dim needDiagnostic As Boolean = True

                            If Not bindResult.IsDefault AndAlso bindResult.Length = 1 Then
                                needDiagnostic = bindResult(0).Kind <> SymbolKind.TypeParameter
                            End If

                            If needDiagnostic AndAlso node.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                                Me._diagnostics.Add(ERRID.WRN_XMLDocBadGenericParamTag2,
                                                    node.GetLocation(),
                                                    nameAttribute.Reference.Identifier.ValueText,
                                                    symbolName)
                            End If
                        End If
                    Next
                End If
            End Sub

            Private Sub ReportWarningsForParamOrTypeParamTags(Of TSymbol As Symbol)(
                                  wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                  tag As WellKnownTag,
                                  tagRef As WellKnownTag,
                                  symbolName As String,
                                  badNameValueError As ERRID,
                                  missingNameValueError As ERRID,
                                  allowedSymbols As ImmutableArray(Of TSymbol))

                Dim builder As ArrayBuilder(Of XmlNodeSyntax) = Nothing
                wellKnownElementNodes.TryGetValue(tag, builder)

                Dim builderRef As ArrayBuilder(Of XmlNodeSyntax) = Nothing
                If tagRef <> WellKnownTag.None Then
                    wellKnownElementNodes.TryGetValue(tagRef, builderRef)
                End If

                If builder Is Nothing AndAlso builderRef Is Nothing Then
                    Return
                End If

                Dim [set] As HashSet(Of String) = Nothing
                If allowedSymbols.Length > 10 Then
                    [set] = New HashSet(Of String)(IdentifierComparison.Comparer)
                    For Each symbol In allowedSymbols
                        [set].Add(symbol.Name)
                    Next
                End If

                If builder IsNot Nothing Then
                    ReportWarningsForParamOrTypeParamTags(builder, symbolName, badNameValueError, missingNameValueError, allowedSymbols, [set])
                End If

                If builderRef IsNot Nothing Then
                    ReportWarningsForParamOrTypeParamTags(builderRef, symbolName, badNameValueError, ERRID.ERR_None, allowedSymbols, [set])
                End If
            End Sub

            Private Sub ReportWarningsForParamOrTypeParamTags(Of TSymbol As Symbol)(
                                  builder As ArrayBuilder(Of XmlNodeSyntax),
                                  symbolName As String,
                                  badNameValueError As ERRID,
                                  missingNameValueError As ERRID,
                                  allowedSymbols As ImmutableArray(Of TSymbol),
                                  [set] As HashSet(Of String))

                For Each node In builder
                    Dim nameAttribute As XmlNameAttributeSyntax = GetFirstNameAttributeValue(node, symbolName, badNameValueError, missingNameValueError)

                    If nameAttribute IsNot Nothing Then
                        Dim nameValue As String = nameAttribute.Reference.Identifier.ValueText

                        Dim needDiagnostic As Boolean = True
                        If [set] Is Nothing Then
                            For Each symbol In allowedSymbols
                                If IdentifierComparison.Equals(nameValue, symbol.Name) Then
                                    needDiagnostic = False
                                    Exit For
                                End If
                            Next
                        Else
                            needDiagnostic = Not [set].Contains(nameValue)
                        End If

                        If needDiagnostic Then
                            If node.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                                Me._diagnostics.Add(badNameValueError, node.GetLocation(), nameValue.Trim(), symbolName)
                            End If
                        End If
                    End If
                Next
            End Sub

            Private Shared Sub FreeWellKnownElementNodes(wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)))
                For Each builder In wellKnownElementNodes.Values
                    builder.Free()
                Next
            End Sub

            ''' <summary>
            ''' Given a DocumentationCommentTriviaSyntax and the symbol, return the 
            ''' full documentation comment text.
            ''' </summary>
            Private Function GetDocumentationCommentForSymbol(symbol As Symbol,
                                                              trivia As DocumentationCommentTriviaSyntax,
                                                              wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax))) As String

                If Not Me.IsInSemanticModelMode Then
                    If trivia.ContainsDiagnostics Then
                        Return Nothing
                    End If

                    ' In case documentation comment was parsed with 'ParseOnly' mode, the trivia itself may not have 
                    ' ContainsDiagnostics flag set, but its children may, check for this case
                    For Each child In trivia.ChildNodes
                        If child.ContainsDiagnostics Then
                            Return Nothing
                        End If
                    Next
                End If

                Dim substitutedText As String =
                    DocumentationCommentWalker.GetSubstitutedText(
                        symbol, trivia, wellKnownElementNodes, Me._diagnostics)

                If substitutedText Is Nothing Then
                    Return Nothing
                End If

                Dim formattedXml As String = FormatComment(substitutedText)

                Dim formattedAfterIncludes As String = Nothing
                If Me._processIncludes Then

                    Dim includeNodes As ArrayBuilder(Of XmlNodeSyntax) = Nothing
                    wellKnownElementNodes.TryGetValue(WellKnownTag.Include, includeNodes)

                    formattedAfterIncludes =
                        IncludeElementExpander.ProcessIncludes(
                            formattedXml,
                            symbol,
                            includeNodes,
                            Me._compilation,
                            Me._filterSyntaxTree,
                            Me._filterSpanWithinTree,
                            Me._includedFileCache,
                            Me._diagnostics,
                            Me._cancellationToken)

                Else
                    formattedAfterIncludes = formattedXml
                End If

                If Me.IsInSemanticModelMode Then
                    ' If we are in semantic model mode we are ignoring any diagnostics anyway, 
                    ' so we can skip parse/check the XML for errors
                    Return formattedAfterIncludes
                End If

                ' We parse/check the XML for errors only if we do want to react on xml errors

                Dim ex As XmlException = XmlDocumentationCommentTextReader.ParseAndGetException(formattedAfterIncludes)
                If ex IsNot Nothing Then
                    If trivia.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                        Me._diagnostics.Add(ERRID.WRN_XMLDocParseError1, trivia.GetLocation(), GetDescription(ex))
                    End If

                    Return Nothing
                End If

                Return formattedAfterIncludes
            End Function

            Private Sub WriteDocumentationCommentForSymbol(xmlDocComment As String)
                If Not Me._isForSingleSymbol Then
                    Write(xmlDocComment)
                Else
                    ' Write just document comment without optional trailing new line
                    Dim [end] As Integer = xmlDocComment.Length
                    If [end] - 1 > 0 AndAlso xmlDocComment([end] - 1) = vbLf Then
                        [end] -= 1
                        If [end] - 1 > 0 AndAlso xmlDocComment([end] - 1) = vbCr Then
                            [end] -= 1
                        End If
                    End If

                    Me._writer.WriteSubString(xmlDocComment, 0, [end], appendNewLine:=False)
                End If
            End Sub

            Private Shared Function GetXmlElementAttributes(element As XmlNodeSyntax) As SyntaxList(Of XmlNodeSyntax)
                Select Case element.Kind
                    Case SyntaxKind.XmlEmptyElement
                        Return DirectCast(element, XmlEmptyElementSyntax).Attributes

                    Case SyntaxKind.XmlElement
                        Return GetXmlElementAttributes(DirectCast(element, XmlElementSyntax).StartTag)

                    Case SyntaxKind.XmlElementStartTag
                        Return DirectCast(element, XmlElementStartTagSyntax).Attributes

                End Select
                Return Nothing ' works as an empty list
            End Function

            ''' <summary>
            ''' Gets the value of the first 'name' attribute on the element, returns Nothing in case 
            ''' the attribute was not found or has an invalid value, reports necessary diagnostics in 
            ''' the latest case
            ''' </summary>
            Private Function GetFirstNameAttributeValue(element As XmlNodeSyntax, symbolName As String,
                                                        badNameValueError As ERRID, missingNameValueError As ERRID) As XmlNameAttributeSyntax

                Dim attributes As SyntaxList(Of XmlNodeSyntax) = GetXmlElementAttributes(element)
                For Each attr In attributes
                    If attr.Kind = SyntaxKind.XmlNameAttribute Then
                        Return DirectCast(attr, XmlNameAttributeSyntax)

                    ElseIf attr.Kind = SyntaxKind.XmlAttribute Then
                        Dim nameAttr = DirectCast(attr, XmlAttributeSyntax)
                        Dim nameAttrName As XmlNodeSyntax = nameAttr.Name

                        If nameAttrName.Kind = SyntaxKind.XmlName AndAlso
                            DocumentationCommentXmlNames.AttributeEquals(
                                DirectCast(nameAttrName, XmlNameSyntax).LocalName.ValueText,
                                DocumentationCommentXmlNames.NameAttributeName) Then

                            ' The 'name' attribute we found was not parsed, 
                            ' generate a warning and return Nothing
                            If element.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                                Dim value As XmlNodeSyntax = nameAttr.Value
                                Dim attributeValue As String =
                                    If(value.Kind = SyntaxKind.XmlString,
                                       Binder.GetXmlString(DirectCast(value, XmlStringSyntax).TextTokens),
                                       value.ToString())

                                Me._diagnostics.Add(badNameValueError, attr.GetLocation(), attributeValue, symbolName)
                            End If

                            Return Nothing
                        End If
                    End If
                Next

                ' 'name' attribute was not found, generate a warning and return Nothing
                If missingNameValueError <> ERRID.ERR_None AndAlso element.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                    Me._diagnostics.Add(missingNameValueError, element.GetLocation())
                End If
                Return Nothing
            End Function

            Private Function TryGetDocCommentTriviaAndGenerateDiagnostics(syntaxNode As SyntaxNode) As DocumentationCommentTriviaSyntax
                Dim theOnlyDocCommentTrivia As DocumentationCommentTriviaSyntax = Nothing
                Dim lastCommentTrivia As Boolean = False

                For Each trivia In syntaxNode.GetLeadingTrivia()
                    Select Case trivia.Kind

                        Case SyntaxKind.DocumentationCommentTrivia
                            If theOnlyDocCommentTrivia IsNot Nothing Then
                                If DirectCast(trivia.SyntaxTree, VisualBasicSyntaxTree).ReportDocumentationCommentDiagnostics() Then
                                    Me._diagnostics.Add(ERRID.WRN_XMLDocMoreThanOneCommentBlock, theOnlyDocCommentTrivia.GetLocation())
                                End If
                            End If
                            theOnlyDocCommentTrivia = DirectCast(trivia.GetStructure(), DocumentationCommentTriviaSyntax)
                            lastCommentTrivia = False

                        Case SyntaxKind.CommentTrivia
                            lastCommentTrivia = True

                    End Select
                Next

                If theOnlyDocCommentTrivia Is Nothing Then
                    Return Nothing
                End If

                If lastCommentTrivia Then
                    If theOnlyDocCommentTrivia.SyntaxTree.ReportDocumentationCommentDiagnostics() Then
                        Me._diagnostics.Add(ERRID.WRN_XMLDocBadXMLLine, theOnlyDocCommentTrivia.GetLocation())
                    End If
                    Return Nothing
                End If

                Return theOnlyDocCommentTrivia
            End Function

            ''' <summary>
            ''' Return symbol text name the way Dev11 does it
            ''' </summary>
            Private Shared Function GetSymbolName(symbol As Symbol) As String
                Select Case symbol.Kind
                    Case SymbolKind.Field
                        Dim associatedSymbol As Symbol = DirectCast(symbol, FieldSymbol).AssociatedSymbol
                        Return If(associatedSymbol IsNot Nothing AndAlso associatedSymbol.IsWithEventsProperty,
                                  "WithEvents variable",
                                  "variable")

                    Case SymbolKind.Method
                        Dim method = DirectCast(symbol, MethodSymbol)
                        Return If(method.MethodKind = MethodKind.DeclareMethod, "declare",
                                  If(method.MethodKind = MethodKind.UserDefinedOperator OrElse method.MethodKind = MethodKind.Conversion, "operator",
                                     If(DirectCast(symbol, MethodSymbol).IsSub, "sub", "function")))

                    Case SymbolKind.Property
                        Return "property"

                    Case SymbolKind.Event
                        Return "event"

                    Case SymbolKind.NamedType
                        Select Case DirectCast(symbol, NamedTypeSymbol).TypeKind
                            Case TypeKind.Class
                                Return "class"
                            Case TypeKind.Delegate
                                Return "delegate"
                            Case TypeKind.Enum
                                Return "enum"
                            Case TypeKind.Interface
                                Return "interface"
                            Case TypeKind.Module
                                Return "module"
                            Case TypeKind.Structure
                                Return "structure"
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(DirectCast(symbol, NamedTypeSymbol).TypeKind)
                        End Select

                End Select
                Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            End Function

            Private Shared Function CreateDocumentationCommentBinderForSymbol(
                    [module] As SourceModuleSymbol,
                    sym As Symbol,
                    tree As SyntaxTree,
                    binderType As DocumentationCommentBinder.BinderType) As Binder

                Dim containingBinder As Binder

                Select Case sym.Kind
                    Case SymbolKind.NamedType
                        Dim namedType = DirectCast(sym, NamedTypeSymbol)
                        If namedType.TypeKind <> TypeKind.Delegate Then
                            containingBinder = BinderBuilder.CreateBinderForType([module], tree, namedType)
                        Else
                            ' Delegates don't have user-defined members, so it makes more sense to treat
                            ' them like methods.
                            Dim containingNamespaceOrType = namedType.ContainingNamespaceOrType
                            containingBinder = If(containingNamespaceOrType.IsNamespace,
                                BinderBuilder.CreateBinderForNamespace([module], tree, DirectCast(containingNamespaceOrType, NamespaceSymbol)),
                                BinderBuilder.CreateBinderForType([module], tree, DirectCast(containingNamespaceOrType, NamedTypeSymbol)))
                        End If

                    Case SymbolKind.Field,
                         SymbolKind.Event,
                         SymbolKind.Property,
                         SymbolKind.Method
                        containingBinder = BinderBuilder.CreateBinderForType([module], tree, sym.ContainingType)

                    Case Else
                        Return Nothing
                End Select

                Return BinderBuilder.CreateBinderForDocumentationComment(containingBinder, sym, binderType)
            End Function
        End Class

    End Class
End Namespace
