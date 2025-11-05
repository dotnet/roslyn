' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Xml
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation

        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            Private Class IncludeElementExpander

                Private ReadOnly _symbol As Symbol
                Private ReadOnly _tagsSupport As WellKnownTagsSupport
                Private ReadOnly _sourceIncludeElementNodes As ArrayBuilder(Of XmlNodeSyntax)
                Private ReadOnly _compilation As VisualBasicCompilation
                Private ReadOnly _tree As SyntaxTree
                Private ReadOnly _onlyDiagnosticsFromTree As SyntaxTree
                Private ReadOnly _filterSpanWithinTree As TextSpan?
                Private ReadOnly _diagnostics As BindingDiagnosticBag
                Private ReadOnly _cancellationToken As CancellationToken

                Private _binders As Dictionary(Of DocumentationCommentBinder.BinderType, Binder) = Nothing

                Private _nextSourceIncludeElementIndex As Integer
                Private _inProgressIncludeElementNodes As HashSet(Of Location)
                Private _includedFileCache As DocumentationCommentIncludeCache

                Private Sub New(symbol As Symbol,
                                sourceIncludeElementNodes As ArrayBuilder(Of XmlNodeSyntax),
                                compilation As VisualBasicCompilation,
                                includedFileCache As DocumentationCommentIncludeCache,
                                onlyDiagnosticsFromTree As SyntaxTree,
                                filterSpanWithinTree As TextSpan?,
                                diagnostics As BindingDiagnosticBag,
                                cancellationToken As CancellationToken)

                    Me._symbol = symbol
                    Me._tagsSupport = New WellKnownTagsSupport(symbol)
                    Me._sourceIncludeElementNodes = sourceIncludeElementNodes
                    Me._compilation = compilation
                    Me._onlyDiagnosticsFromTree = onlyDiagnosticsFromTree
                    Me._filterSpanWithinTree = filterSpanWithinTree
                    Me._diagnostics = diagnostics
                    Me._cancellationToken = cancellationToken

                    Me._tree = If(sourceIncludeElementNodes Is Nothing OrElse
                                    sourceIncludeElementNodes.Count = 0,
                                  Nothing,
                                  sourceIncludeElementNodes(0).SyntaxTree)

                    Me._includedFileCache = includedFileCache

                    Me._nextSourceIncludeElementIndex = 0
                End Sub

                Private Structure WellKnownTagsSupport
                    Public ReadOnly ExceptionSupported As Boolean
                    Public ReadOnly ReturnsSupported As Boolean
                    Public ReadOnly ParamAndParamRefSupported As Boolean
                    Public ReadOnly ValueSupported As Boolean
                    Public ReadOnly TypeParamSupported As Boolean
                    Public ReadOnly TypeParamRefSupported As Boolean

                    Public ReadOnly IsDeclareMethod As Boolean
                    Public ReadOnly IsWriteOnlyProperty As Boolean

                    Public ReadOnly SymbolName As String

                    Public Sub New(symbol As Symbol)
                        Me.ExceptionSupported = False
                        Me.ReturnsSupported = False
                        Me.ParamAndParamRefSupported = False
                        Me.ValueSupported = False
                        Me.TypeParamSupported = False
                        Me.TypeParamRefSupported = False

                        Me.IsDeclareMethod = False
                        Me.IsWriteOnlyProperty = False

                        Me.SymbolName = GetSymbolName(symbol)

                        Select Case symbol.Kind
                            Case SymbolKind.Field
                                Me.TypeParamRefSupported = True

                            Case SymbolKind.Event
                                Me.ExceptionSupported = True
                                Me.ParamAndParamRefSupported = True
                                Me.TypeParamRefSupported = True

                            Case SymbolKind.Method
                                Dim method = DirectCast(symbol, MethodSymbol)
                                Me.IsDeclareMethod = method.MethodKind = MethodKind.DeclareMethod

                                Me.ExceptionSupported = True
                                Me.ParamAndParamRefSupported = True
                                Me.TypeParamSupported = Not Me.IsDeclareMethod AndAlso method.MethodKind <> MethodKind.UserDefinedOperator
                                Me.TypeParamRefSupported = True

                                If Not method.IsSub Then
                                    Me.ReturnsSupported = True
                                End If

                            Case SymbolKind.NamedType
                                Dim namedType = DirectCast(symbol, NamedTypeSymbol)
                                Dim invokeMethod As MethodSymbol = namedType.DelegateInvokeMethod

                                If namedType.TypeKind = TYPEKIND.Delegate Then
                                    If invokeMethod IsNot Nothing AndAlso Not invokeMethod.IsSub Then
                                        Me.ReturnsSupported = True
                                    Else
                                        Me.SymbolName = "delegate sub"
                                    End If
                                End If

                                Me.ParamAndParamRefSupported = namedType.TypeKind = TYPEKIND.Delegate
                                Me.TypeParamSupported = namedType.TypeKind <> TYPEKIND.Enum AndAlso namedType.TypeKind <> TYPEKIND.Module
                                Me.TypeParamRefSupported = namedType.TypeKind <> TYPEKIND.Module

                            Case SymbolKind.Property
                                Dim prop = DirectCast(symbol, PropertySymbol)

                                Me.ExceptionSupported = True
                                Me.ParamAndParamRefSupported = True
                                Me.TypeParamRefSupported = True
                                Me.ValueSupported = True

                                Me.IsWriteOnlyProperty = prop.IsWriteOnly
                                Me.ReturnsSupported = Not Me.IsWriteOnlyProperty

                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
                        End Select
                    End Sub
                End Structure

                Friend Shared Function ProcessIncludes(unprocessed As String,
                                                       memberSymbol As Symbol,
                                                       sourceIncludeElementNodes As ArrayBuilder(Of XmlNodeSyntax),
                                                       compilation As VisualBasicCompilation,
                                                       onlyDiagnosticsFromTree As SyntaxTree,
                                                       filterSpanWithinTree As TextSpan?,
                                                       ByRef includedFileCache As DocumentationCommentIncludeCache,
                                                       diagnostics As BindingDiagnosticBag,
                                                       cancellationToken As CancellationToken) As String

                    ' If there are no include elements, then there's nothing to expand.
                    '
                    ' NOTE: Following C# implementation we avoid parsing/printing of the xml
                    '       in this case, which might differ in terms of printed whitespaces 
                    '       if we compare to the result of parse/print scenario
                    If sourceIncludeElementNodes Is Nothing Then
                        Return unprocessed
                    End If

                    Debug.Assert(sourceIncludeElementNodes.Count > 0)

                    Dim doc As XDocument

                    Try
                        ' NOTE: XDocument.Parse seems to do a better job of preserving whitespace than XElement.Parse.
                        doc = XDocument.Parse(unprocessed, LoadOptions.PreserveWhitespace)

                    Catch ex As XmlException
                        Return unprocessed
                    End Try

                    Dim pooled As PooledStringBuilder = PooledStringBuilder.GetInstance()

                    Using writer As New StringWriter(pooled.Builder, CultureInfo.InvariantCulture)
                        cancellationToken.ThrowIfCancellationRequested()

                        Dim expander = New IncludeElementExpander(memberSymbol,
                                                                  sourceIncludeElementNodes,
                                                                  compilation,
                                                                  includedFileCache,
                                                                  onlyDiagnosticsFromTree,
                                                                  filterSpanWithinTree,
                                                                  diagnostics,
                                                                  cancellationToken)

                        For Each node In expander.Rewrite(doc, currentXmlFilePath:=Nothing, originatingSyntax:=Nothing)
                            cancellationToken.ThrowIfCancellationRequested()
                            writer.Write(node)
                        Next

                        Debug.Assert(expander._nextSourceIncludeElementIndex = expander._sourceIncludeElementNodes.Count)
                        includedFileCache = expander._includedFileCache
                    End Using

                    Return pooled.ToStringAndFree()
                End Function

                Private ReadOnly Property ProduceDiagnostics As Boolean
                    Get
                        Return Me._tree.ReportDocumentationCommentDiagnostics
                    End Get
                End Property

                Private ReadOnly Property ProduceXmlDiagnostics As Boolean
                    Get
                        Return Me._tree.ReportDocumentationCommentDiagnostics AndAlso Me._onlyDiagnosticsFromTree Is Nothing
                    End Get
                End Property

                Private ReadOnly Property [Module] As SourceModuleSymbol
                    Get
                        Return DirectCast(Me._compilation.SourceModule, SourceModuleSymbol)
                    End Get
                End Property

                Private Function GetOrCreateBinder(type As DocumentationCommentBinder.BinderType) As Binder
                    If Me._binders Is Nothing Then
                        Me._binders = New Dictionary(Of DocumentationCommentBinder.BinderType, Binder)()
                    End If

                    Dim result As Binder = Nothing
                    If Not Me._binders.TryGetValue(type, result) Then

                        Debug.Assert(Me._tree IsNot Nothing)
                        result = CreateDocumentationCommentBinderForSymbol(Me.Module, Me._symbol, Me._tree, type)
                        Me._binders.Add(type, result)
                    End If

                    Return result
                End Function

                ''' <remarks>
                ''' Rewrites nodes in <paramref name="nodes"/>, which Is a snapshot of nodes from the original document.
                ''' We're mutating the tree as we rewrite, so it's important to grab a snapshot of the
                ''' nodes that we're going to reparent before we enumerate them.
                ''' </remarks>
                Private Function RewriteMany(nodes As XNode(), currentXmlFilePath As String, originatingSyntax As XmlNodeSyntax) As XNode()
                    Debug.Assert(nodes IsNot Nothing)

                    Dim builder As ArrayBuilder(Of XNode) = Nothing
                    For Each child In nodes
                        If builder Is Nothing Then
                            builder = ArrayBuilder(Of XNode).GetInstance()
                        End If
                        builder.AddRange(Rewrite(child, currentXmlFilePath, originatingSyntax))
                    Next

                    ' Nodes returned by this method are going to be attached to a new parent, so it's
                    ' important that they don't already have parents.  If a node with a parent is
                    ' attached to a new parent, it is copied and its annotations are dropped.
                    Debug.Assert(builder Is Nothing OrElse builder.All(Function(node) node.Parent Is Nothing))

                    Return If(builder Is Nothing, Array.Empty(Of XNode)(), builder.ToArrayAndFree())
                End Function

                Private Function Rewrite(node As XNode, currentXmlFilePath As String, originatingSyntax As XmlNodeSyntax) As XNode()
                    Me._cancellationToken.ThrowIfCancellationRequested()

                    Dim commentMessage As String = Nothing

                    If node.NodeType = XmlNodeType.Element Then
                        Dim element = DirectCast(node, XElement)
                        If ElementNameIs(element, DocumentationCommentXmlNames.IncludeElementName) Then
                            Dim rewritten As XNode() = RewriteIncludeElement(element, currentXmlFilePath, originatingSyntax, commentMessage)
                            If rewritten IsNot Nothing Then
                                Return rewritten
                            End If
                        End If
                    End If

                    Dim container = TryCast(node, XContainer)
                    If container Is Nothing Then
                        Debug.Assert(commentMessage Is Nothing, "How did we get an error comment for a non-container?")
                        Return New XNode() {node.Copy(copyAttributeAnnotations:=True)}
                    End If

                    Dim oldNodes As IEnumerable(Of XNode) = container.Nodes

                    ' Do this after grabbing the nodes, so we don't see copies of them.
                    container = container.Copy(copyAttributeAnnotations:=True)

                    ' WARN: don't use node after this point - use container since it's already been copied.

                    If oldNodes IsNot Nothing Then
                        Dim rewritten As XNode() = RewriteMany(oldNodes.ToArray(), currentXmlFilePath, originatingSyntax)
                        container.ReplaceNodes(rewritten)
                    End If

                    ' NOTE: we only care if we're included text - otherwise we've already processed the cref/name.
                    If container.NodeType = XmlNodeType.Element AndAlso originatingSyntax IsNot Nothing Then
                        Debug.Assert(currentXmlFilePath IsNot Nothing)

                        Dim element = DirectCast(container, XElement)
                        Dim elementName As XName = element.Name

                        Dim binderType As DocumentationCommentBinder.BinderType = DocumentationCommentBinder.BinderType.None

                        Dim elementIsException As Boolean = False ' To support WRN_XMLDocExceptionTagWithoutCRef

                        ' Check element first for well-known names
                        If ElementNameIs(element, DocumentationCommentXmlNames.ExceptionElementName) Then
                            If Not Me._tagsSupport.ExceptionSupported Then
                                commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath),
                                                                    ERRID.WRN_XMLDocIllegalTagOnElement2,
                                                                    elementName.LocalName,
                                                                    Me._tagsSupport.SymbolName)

                            Else
                                elementIsException = True
                            End If

                        ElseIf ElementNameIs(element, DocumentationCommentXmlNames.ReturnsElementName) Then
                            If Not Me._tagsSupport.ReturnsSupported Then

                                ' NOTE: different messages in two cases:
                                If Me._tagsSupport.IsDeclareMethod Then
                                    commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath), ERRID.WRN_XMLDocReturnsOnADeclareSub)

                                ElseIf Me._tagsSupport.IsWriteOnlyProperty Then
                                    commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath), ERRID.WRN_XMLDocReturnsOnWriteOnlyProperty)

                                Else
                                    commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath),
                                                                        ERRID.WRN_XMLDocIllegalTagOnElement2,
                                                                        elementName.LocalName,
                                                                        Me._tagsSupport.SymbolName)
                                End If
                            End If

                        ElseIf ElementNameIs(element, DocumentationCommentXmlNames.ParameterElementName) OrElse
                                    ElementNameIs(element, DocumentationCommentXmlNames.ParameterReferenceElementName) Then

                            binderType = DocumentationCommentBinder.BinderType.NameInParamOrParamRef
                            If Not Me._tagsSupport.ParamAndParamRefSupported Then
                                commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath),
                                                                    ERRID.WRN_XMLDocIllegalTagOnElement2,
                                                                    elementName.LocalName,
                                                                    Me._tagsSupport.SymbolName)
                            End If

                        ElseIf ElementNameIs(element, DocumentationCommentXmlNames.ValueElementName) Then
                            If Not Me._tagsSupport.ValueSupported Then
                                commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath),
                                                                    ERRID.WRN_XMLDocIllegalTagOnElement2,
                                                                    elementName.LocalName,
                                                                    Me._tagsSupport.SymbolName)
                            End If

                        ElseIf ElementNameIs(element, DocumentationCommentXmlNames.TypeParameterElementName) Then
                            binderType = DocumentationCommentBinder.BinderType.NameInTypeParam
                            If Not Me._tagsSupport.TypeParamSupported Then
                                commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath),
                                                                    ERRID.WRN_XMLDocIllegalTagOnElement2,
                                                                    elementName.LocalName,
                                                                    Me._tagsSupport.SymbolName)
                            End If

                        ElseIf ElementNameIs(element, DocumentationCommentXmlNames.TypeParameterReferenceElementName) Then
                            binderType = DocumentationCommentBinder.BinderType.NameInTypeParamRef
                            If Not Me._tagsSupport.TypeParamRefSupported Then
                                commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath),
                                                                    ERRID.WRN_XMLDocIllegalTagOnElement2,
                                                                    elementName.LocalName,
                                                                    Me._tagsSupport.SymbolName)
                            End If
                        End If

                        If commentMessage Is Nothing Then

                            Dim nameAttribute As XAttribute = Nothing
                            Dim seenCref As Boolean = False

                            For Each attribute In element.Attributes
                                If AttributeNameIs(attribute, DocumentationCommentXmlNames.CrefAttributeName) Then
                                    ' NOTE: 'cref=' errors are ignored, because the reference is marked with "?:..."
                                    BindAndReplaceCref(attribute, currentXmlFilePath)
                                    seenCref = True

                                ElseIf AttributeNameIs(attribute, DocumentationCommentXmlNames.NameAttributeName) Then
                                    nameAttribute = attribute
                                End If
                            Next

                            ' After processing 'special' attributes, we need to either bind 'name' 
                            ' attribute value or, if the element was 'exception', and 'cref' was not found,
                            ' report WRN_XMLDocExceptionTagWithoutCRef
                            If elementIsException Then
                                If Not seenCref Then
                                    commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath), ERRID.WRN_XMLDocExceptionTagWithoutCRef)
                                End If

                            ElseIf binderType <> DocumentationCommentBinder.BinderType.None Then
                                Debug.Assert(binderType <> DocumentationCommentBinder.BinderType.Cref)

                                If nameAttribute Is Nothing Then
                                    ' Report missing 'name' attribute
                                    commentMessage = GenerateDiagnostic(XmlLocation.Create(element, currentXmlFilePath),
                                                                        If(binderType = DocumentationCommentBinder.BinderType.NameInParamOrParamRef,
                                                                           ERRID.WRN_XMLDocParamTagWithoutName,
                                                                           ERRID.WRN_XMLDocGenericParamTagWithoutName))
                                Else
                                    ' Bind the value of 'name' attribute
                                    commentMessage = BindName(nameAttribute,
                                                              elementName.LocalName,
                                                              binderType,
                                                              If(binderType = DocumentationCommentBinder.BinderType.NameInParamOrParamRef, ERRID.WRN_XMLDocBadParamTag2, ERRID.WRN_XMLDocBadGenericParamTag2),
                                                              currentXmlFilePath)
                                End If
                            End If
                        End If
                    End If

                    If commentMessage Is Nothing Then
                        Return New XNode() {container}
                    Else
                        Return New XNode() {New XComment(commentMessage), container}
                    End If
                End Function

                Private Shared Function ElementNameIs(element As XElement, name As String) As Boolean
                    Return String.IsNullOrEmpty(element.Name.NamespaceName) AndAlso
                           DocumentationCommentXmlNames.ElementEquals(element.Name.LocalName, name, True)
                End Function

                Private Shared Function AttributeNameIs(attribute As XAttribute, name As String) As Boolean
                    Return String.IsNullOrEmpty(attribute.Name.NamespaceName) AndAlso
                           DocumentationCommentXmlNames.AttributeEquals(attribute.Name.LocalName, name)
                End Function

                Private Function RewriteIncludeElement(includeElement As XElement, currentXmlFilePath As String, originatingSyntax As XmlNodeSyntax, <Out> ByRef commentMessage As String) As XNode()
                    Dim location As location = GetIncludeElementLocation(includeElement, currentXmlFilePath, originatingSyntax)
                    Debug.Assert(originatingSyntax IsNot Nothing)

                    If Not AddIncludeElementLocation(location) Then

                        ' NOTE: these must exist since we're already processed this node elsewhere in the call stack.
                        Dim fileAttr As XAttribute = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.FileAttributeName))
                        Dim pathAttr As XAttribute = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.PathAttributeName))

                        commentMessage = GenerateDiagnostic(location, ERRID.WRN_XMLDocInvalidXMLFragment, fileAttr.Value, pathAttr.Value)

                        ' Don't inspect the children - we're already in a cycle.
                        Return New XNode() {New XComment(commentMessage)}
                    End If

                    Try
                        Dim fileAttr As XAttribute = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.FileAttributeName))
                        Dim pathAttr As XAttribute = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.PathAttributeName))

                        Dim hasFileAttribute As Boolean = fileAttr IsNot Nothing
                        Dim hasPathAttribute As Boolean = pathAttr IsNot Nothing

                        If Not hasFileAttribute OrElse Not hasPathAttribute Then
                            ' 'file' or 'path' attribute missing
                            If Not hasFileAttribute Then
                                commentMessage = GenerateDiagnostic(location, ERRID.WRN_XMLMissingFileOrPathAttribute1, DocumentationCommentXmlNames.FileAttributeName)
                            End If

                            If Not hasPathAttribute Then
                                commentMessage = If(commentMessage Is Nothing, "", commentMessage & " ") &
                                                     GenerateDiagnostic(location, ERRID.WRN_XMLMissingFileOrPathAttribute1, DocumentationCommentXmlNames.PathAttributeName)
                            End If

                            Return New XNode() {New XComment(commentMessage)}
                        End If

                        Dim xpathValue As String = pathAttr.Value
                        Dim filePathValue As String = fileAttr.Value

                        Dim resolver = _compilation.Options.XmlReferenceResolver
                        If resolver Is Nothing Then
                            commentMessage = GenerateDiagnostic(True, location, ERRID.WRN_XMLDocBadFormedXML, filePathValue, xpathValue, New CodeAnalysisResourcesLocalizableErrorArgument(NameOf(CodeAnalysisResources.XmlReferencesNotSupported)))
                            Return New XNode() {New XComment(commentMessage)}
                        End If

                        Dim resolvedFilePath As String = resolver.ResolveReference(filePathValue, currentXmlFilePath)
                        If resolvedFilePath Is Nothing Then
                            commentMessage = GenerateDiagnostic(True, location, ERRID.WRN_XMLDocBadFormedXML, filePathValue, xpathValue, New CodeAnalysisResourcesLocalizableErrorArgument(NameOf(CodeAnalysisResources.FileNotFound)))
                            Return New XNode() {New XComment(commentMessage)}
                        End If

                        If _includedFileCache Is Nothing Then
                            _includedFileCache = New DocumentationCommentIncludeCache(_compilation.Options.XmlReferenceResolver)
                        End If

                        Try
                            Dim doc As XDocument

                            Try
                                doc = _includedFileCache.GetOrMakeDocument(resolvedFilePath)
                            Catch e As IOException
                                commentMessage = GenerateDiagnostic(True, location, ERRID.WRN_XMLDocBadFormedXML, filePathValue, xpathValue, e.Message)
                                Return New XNode() {New XComment(commentMessage)}
                            End Try

                            Debug.Assert(doc IsNot Nothing)

                            Dim errorMessage As String = Nothing
                            Dim invalidXPath As Boolean = False
                            Dim loadedElements As XElement() = XmlUtilities.TrySelectElements(doc, xpathValue, errorMessage, invalidXPath)

                            If loadedElements Is Nothing Then
                                commentMessage = GenerateDiagnostic(True, location, ERRID.WRN_XMLDocInvalidXMLFragment, xpathValue, filePathValue)
                                Return New XNode() {New XComment(commentMessage)}
                            End If

                            If loadedElements IsNot Nothing AndAlso loadedElements.Length > 0 Then
                                ' change the current XML file path for nodes contained in the document
                                Dim result As XNode() = RewriteMany(loadedElements, resolvedFilePath, originatingSyntax)

                                ' The elements could be rewritten away if they are includes that refer to invalid
                                ' (but existing and accessible) XML files. If this occurs, behave as if we
                                ' had failed to find any XPath results.
                                If result.Length > 0 Then
                                    ' NOTE: in this case, we do NOT visit the children of the include element -
                                    ' they are dropped.
                                    commentMessage = Nothing
                                    Return result
                                End If
                            End If

                            ' Nothing was found
                            commentMessage = GenerateDiagnostic(True, location, ERRID.WRN_XMLDocInvalidXMLFragment, xpathValue, filePathValue)
                            Return New XNode() {New XComment(commentMessage)}

                        Catch ex As XmlException
                            commentMessage = GenerateDiagnostic(True, location, ERRID.WRN_XMLDocInvalidXMLFragment, xpathValue, filePathValue)
                            Return New XNode() {New XComment(commentMessage)}
                        End Try
                    Finally
                        RemoveIncludeElementLocation(location)
                    End Try
                End Function

                Private Function ShouldProcessLocation(loc As Location) As Boolean
                    Return Me._onlyDiagnosticsFromTree Is Nothing OrElse
                        loc.Kind = LocationKind.SourceFile AndAlso DirectCast(loc, SourceLocation).SourceTree Is Me._onlyDiagnosticsFromTree AndAlso
                        (Not Me._filterSpanWithinTree.HasValue OrElse Me._filterSpanWithinTree.Value.Contains(loc.SourceSpan))
                End Function

                Private Function GenerateDiagnostic(suppressDiagnostic As Boolean, loc As Location, id As ERRID, ParamArray arguments As Object()) As String
                    Dim info As DiagnosticInfo = ErrorFactory.ErrorInfo(id, arguments)
                    If Not suppressDiagnostic AndAlso Me.ProduceDiagnostics AndAlso ShouldProcessLocation(loc) Then
                        Me._diagnostics.Add(New VBDiagnostic(info, loc))
                    End If
                    Return info.ToString()
                End Function

                Private Function GenerateDiagnostic(loc As Location, id As ERRID, ParamArray arguments As Object()) As String
                    Return GenerateDiagnostic(False, loc, id, arguments)
                End Function

                Private Function AddIncludeElementLocation(location As Location) As Boolean
                    If Me._inProgressIncludeElementNodes Is Nothing Then
                        Me._inProgressIncludeElementNodes = New HashSet(Of location)()
                    End If

                    Return Me._inProgressIncludeElementNodes.Add(location)
                End Function

                Private Function RemoveIncludeElementLocation(location As Location) As Boolean
                    Debug.Assert(Me._inProgressIncludeElementNodes IsNot Nothing)
                    Dim result As Boolean = Me._inProgressIncludeElementNodes.Remove(location)
                    Debug.Assert(result)
                    Return result
                End Function

                Private Function GetIncludeElementLocation(includeElement As XElement, ByRef currentXmlFilePath As String, ByRef originatingSyntax As XmlNodeSyntax) As Location
                    Dim location As location = includeElement.Annotation(Of location)()

                    If location IsNot Nothing Then
                        Return location
                    End If

                    ' If we are not in an XML file, then we must be in a source file.  Since we're traversing the XML tree in the same
                    ' order as the DocumentationCommentWalker, we can access the elements of includeElementNodes in order.
                    If currentXmlFilePath Is Nothing Then
                        Debug.Assert(_nextSourceIncludeElementIndex < _sourceIncludeElementNodes.Count)
                        Debug.Assert(originatingSyntax Is Nothing)

                        originatingSyntax = _sourceIncludeElementNodes(_nextSourceIncludeElementIndex)
                        location = originatingSyntax.GetLocation()
                        Me._nextSourceIncludeElementIndex += 1
                        includeElement.AddAnnotation(location)

                        currentXmlFilePath = location.GetLineSpan().Path
                    Else
                        location = XmlLocation.Create(includeElement, currentXmlFilePath)
                    End If

                    Debug.Assert(location IsNot Nothing)
                    Return location
                End Function

                Private Sub BindAndReplaceCref(attribute As XAttribute, currentXmlFilePath As String)
                    Debug.Assert(currentXmlFilePath IsNot Nothing)

                    Dim attributeText As String = attribute.ToString()

                    ' note, the parent element name does not matter
                    Dim attr As BaseXmlAttributeSyntax = SyntaxFactory.ParseDocCommentAttributeAsStandAloneEntity(attributeText, parentElementName:="")

                    ' NOTE: we don't expect any *syntax* errors on the parsed xml 
                    '       attribute, or otherwise why xml parsed didn't throw?
                    Debug.Assert(attr IsNot Nothing)

                    Select Case attr.Kind

                        Case SyntaxKind.XmlCrefAttribute
                            Dim binder As binder = Me.GetOrCreateBinder(DocumentationCommentBinder.BinderType.Cref)
                            Dim reference As CrefReferenceSyntax = DirectCast(attr, XmlCrefAttributeSyntax).Reference
                            Dim useSiteInfo = binder.GetNewCompoundUseSiteInfo(_diagnostics)
                            Dim diagnostics = BindingDiagnosticBag.GetInstance(_diagnostics)
                            Dim bindResult As ImmutableArray(Of Symbol) = binder.BindInsideCrefAttributeValue(reference, preserveAliases:=False,
                                                                                                              diagnosticBag:=diagnostics, useSiteInfo:=useSiteInfo)
                            _diagnostics.AddDependencies(diagnostics)
                            _diagnostics.AddDependencies(useSiteInfo)

                            Dim errorLocations = diagnostics.DiagnosticBag.ToReadOnly().SelectAsArray(Function(x) x.Location).WhereAsArray(Function(x) x IsNot Nothing)
                            diagnostics.Free()

                            If Me.ProduceXmlDiagnostics AndAlso Not useSiteInfo.Diagnostics.IsNullOrEmpty Then
                                ProcessErrorLocations(XmlLocation.Create(attribute, currentXmlFilePath), Nothing, useSiteInfo, errorLocations, Nothing)
                            End If

                            If bindResult.IsDefaultOrEmpty Then
                                If Me.ProduceXmlDiagnostics Then
                                    ProcessErrorLocations(XmlLocation.Create(attribute, currentXmlFilePath), reference.ToFullString().TrimEnd(), useSiteInfo, errorLocations, ERRID.WRN_XMLDocCrefAttributeNotFound1)
                                End If
                                attribute.Value = "?:" + attribute.Value

                            Else
                                ' The following mimics handling 'cref' attributes in source documentation 
                                ' comment, see DocumentationCommentWalker for details

                                ' Some symbols found may not support doc-comment-ids, we just filter 
                                ' those out, from the rest we take the symbol with 'smallest' location
                                Dim symbolCommentId As String = Nothing
                                Dim smallestSymbol As Symbol = Nothing
                                Dim errid As ERRID = errid.WRN_XMLDocCrefAttributeNotFound1

                                For Each symbol In bindResult
                                    If symbol.Kind = SymbolKind.TypeParameter Then
                                        errid = errid.WRN_XMLDocCrefToTypeParameter
                                        Continue For
                                    End If

                                    Dim id As String = symbol.OriginalDefinition.GetDocumentationCommentId()
                                    If id IsNot Nothing Then

                                        ' Override only if this is the first id or the new symbol's location wins; 
                                        ' note that we want to ignore the cases when there are more than one symbol 
                                        ' can be found by the name, just deterministically choose which one to use
                                        If symbolCommentId Is Nothing OrElse
                                                Me._compilation.CompareSourceLocations(
                                                    smallestSymbol.GetFirstLocation(), symbol.GetFirstLocation()) > 0 Then

                                            symbolCommentId = id
                                            smallestSymbol = symbol
                                        End If
                                    End If
                                Next

                                If symbolCommentId Is Nothing Then
                                    If Me.ProduceXmlDiagnostics Then
                                        ProcessErrorLocations(XmlLocation.Create(attribute, currentXmlFilePath), reference.ToString(), Nothing, errorLocations, errid)
                                    End If
                                    attribute.Value = "?:" + attribute.Value

                                Else
                                    ' Replace value with id 
                                    attribute.Value = symbolCommentId
                                    _diagnostics.AddAssembliesUsedByCrefTarget(smallestSymbol.OriginalDefinition)
                                End If

                            End If

                        Case SyntaxKind.XmlAttribute
                            ' 'cref=' attribute can land here for two reasons: 
                            '   (a) the value is represented in a form "X:SOME-ID-STRING", or
                            '   (b) the value between '"' is not a valid NameSyntax
                            '
                            ' in both cases we want just to put the result into documentation XML.
                            '
                            ' In the second case we also generate a diagnostic and add '!:' in from 
                            ' of the value indicating wrong id, and generate a diagnostic
                            Dim value As String = attribute.Value.Trim()
                            If value.Length < 2 OrElse value(0) = ":"c OrElse value(1) <> ":"c Then
                                ' Case (b) from above
                                If Me.ProduceXmlDiagnostics Then
                                    Me._diagnostics.Add(ERRID.WRN_XMLDocCrefAttributeNotFound1, XmlLocation.Create(attribute, currentXmlFilePath), value)
                                End If
                                attribute.Value = "?:" + value
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(attr.Kind)

                    End Select
                End Sub

                Private Sub ProcessErrorLocations(currentXmlLocation As XmlLocation, referenceName As String, useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol), errorLocations As ImmutableArray(Of Location), errid As Nullable(Of ERRID))
                    If errorLocations.Length = 0 Then
                        If useSiteInfo.Diagnostics IsNot Nothing Then
                            Me._diagnostics.AddDiagnostics(currentXmlLocation, useSiteInfo)
                        ElseIf errid.HasValue Then
                            Me._diagnostics.Add(errid.Value, currentXmlLocation, referenceName)
                        End If
                    ElseIf errid.HasValue Then
                        For Each location In errorLocations
                            Me._diagnostics.Add(errid.Value, location, referenceName)
                        Next
                    Else
                        For Each location In errorLocations
                            Me._diagnostics.AddDiagnostics(location, useSiteInfo)
                        Next
                    End If
                End Sub

                Private Function BindName(attribute As XAttribute,
                                          elementName As String,
                                          type As DocumentationCommentBinder.BinderType,
                                          badNameValueError As ERRID,
                                          currentXmlFilePath As String) As String

                    Debug.Assert(type = DocumentationCommentBinder.BinderType.NameInParamOrParamRef OrElse
                                 type = DocumentationCommentBinder.BinderType.NameInTypeParamRef OrElse
                                 type = DocumentationCommentBinder.BinderType.NameInTypeParam)

                    Debug.Assert(currentXmlFilePath IsNot Nothing)

                    Dim commentMessage As String = Nothing

                    Dim attributeText As String = attribute.ToString()
                    Dim attributeValue As String = attribute.Value.Trim()
                    Dim attr As BaseXmlAttributeSyntax =
                        SyntaxFactory.ParseDocCommentAttributeAsStandAloneEntity(
                            attributeText, elementName) ' note, the element name does not matter

                    ' NOTE: we don't expect any *syntax* errors on the parsed xml 
                    '       attribute, or otherwise why xml parsed didn't throw?
                    Debug.Assert(attr IsNot Nothing)
                    Debug.Assert(Not attr.ContainsDiagnostics)

                    Select Case attr.Kind

                        Case SyntaxKind.XmlNameAttribute
                            Dim binder As binder = Me.GetOrCreateBinder(type)
                            Dim identifier As IdentifierNameSyntax = DirectCast(attr, XmlNameAttributeSyntax).Reference
                            Dim useSiteInfo = binder.GetNewCompoundUseSiteInfo(Me._diagnostics)
                            Dim bindResult As ImmutableArray(Of Symbol) = binder.BindXmlNameAttributeValue(identifier, useSiteInfo)

                            Me._diagnostics.AddDependencies(useSiteInfo)

                            If Me.ProduceDiagnostics AndAlso Not useSiteInfo.Diagnostics.IsNullOrEmpty Then
                                Dim loc As Location = XmlLocation.Create(attribute, currentXmlFilePath)
                                If ShouldProcessLocation(loc) Then
                                    Me._diagnostics.AddDiagnostics(loc, useSiteInfo)
                                End If
                            End If

                            If bindResult.IsDefaultOrEmpty Then
                                commentMessage = GenerateDiagnostic(XmlLocation.Create(attribute, currentXmlFilePath), badNameValueError, attributeValue, Me._tagsSupport.SymbolName)
                            End If

                        Case SyntaxKind.XmlAttribute
                            ' 'name=' attribute can get here if parsing of identifier went wrong, we need to generate a diagnostic
                            commentMessage = GenerateDiagnostic(XmlLocation.Create(attribute, currentXmlFilePath), badNameValueError, attributeValue, Me._tagsSupport.SymbolName)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(attr.Kind)
                    End Select

                    Return commentMessage
                End Function
            End Class

        End Class

    End Class
End Namespace
