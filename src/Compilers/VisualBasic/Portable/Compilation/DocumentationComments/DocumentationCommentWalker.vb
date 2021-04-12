' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation
        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            ''' <summary>
            ''' Walks a DocumentationCommentTriviaSyntax, binding the semantically meaningful parts 
            ''' to produce diagnostics and to replace source crefs with documentation comment IDs.
            ''' </summary>
            Private Class DocumentationCommentWalker
                Inherits VisualBasicSyntaxWalker

                Private ReadOnly _symbol As Symbol
                Private ReadOnly _syntaxTree As SyntaxTree
                Private ReadOnly _wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax))
                Private ReadOnly _reportDiagnostics As Boolean
                Private ReadOnly _writer As TextWriter
                Private ReadOnly _diagnostics As BindingDiagnosticBag

                Private Sub New(symbol As Symbol,
                                syntaxTree As SyntaxTree,
                                wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                writer As TextWriter,
                                diagnostics As BindingDiagnosticBag)

                    MyBase.New(SyntaxWalkerDepth.Token)

                    Debug.Assert(symbol IsNot Nothing)
                    Debug.Assert(syntaxTree IsNot Nothing)
                    Debug.Assert(diagnostics IsNot Nothing)

                    Me._symbol = symbol
                    Me._syntaxTree = syntaxTree
                    Me._wellKnownElementNodes = wellKnownElementNodes
                    Me._writer = writer
                    Me._diagnostics = diagnostics
                    Me._reportDiagnostics = syntaxTree.ReportDocumentationCommentDiagnostics()
                End Sub

                Private Sub CaptureWellKnownTagNode(node As XmlNodeSyntax, name As XmlNodeSyntax)
                    Debug.Assert(node IsNot Nothing)
                    Debug.Assert(name IsNot Nothing)

                    If Me._wellKnownElementNodes Is Nothing Then
                        Return
                    End If

                    If name.Kind <> SyntaxKind.XmlName Then
                        Return
                    End If

                    Dim xmlName = DirectCast(name, XmlNameSyntax).LocalName.ValueText
                    Dim tag As WellKnownTag = GetWellKnownTag(xmlName)
                    If (tag And WellKnownTag.AllCollectable) = 0 Then
                        Return
                    End If

                    Dim builder As ArrayBuilder(Of XmlNodeSyntax) = Nothing
                    If Not Me._wellKnownElementNodes.TryGetValue(tag, builder) Then
                        builder = ArrayBuilder(Of XmlNodeSyntax).GetInstance()
                        Me._wellKnownElementNodes.Add(tag, builder)
                    End If

                    builder.Add(node)
                End Sub

                Public Overrides Sub VisitXmlEmptyElement(node As XmlEmptyElementSyntax)
                    CaptureWellKnownTagNode(node, node.Name)
                    MyBase.VisitXmlEmptyElement(node)
                End Sub

                Public Overrides Sub VisitXmlElement(node As XmlElementSyntax)
                    CaptureWellKnownTagNode(node, node.StartTag.Name)
                    MyBase.VisitXmlElement(node)
                End Sub

                Private Sub WriteHeaderAndVisit(symbol As Symbol, trivia As DocumentationCommentTriviaSyntax)
                    Me._writer.Write("<member name=""")
                    Me._writer.Write(symbol.GetDocumentationCommentId())
                    Me._writer.WriteLine(""">")

                    Visit(trivia)

                    Me._writer.WriteLine("</member>")
                End Sub

                ''' <summary>
                ''' Given a DocumentationCommentTriviaSyntax, return the full text, but with
                ''' documentation comment IDs substituted into crefs.
                ''' </summary>
                Friend Shared Function GetSubstitutedText(symbol As Symbol,
                                                          trivia As DocumentationCommentTriviaSyntax,
                                                          wellKnownElementNodes As Dictionary(Of WellKnownTag, ArrayBuilder(Of XmlNodeSyntax)),
                                                          diagnostics As BindingDiagnosticBag) As String

                    Dim pooled As PooledStringBuilder = PooledStringBuilder.GetInstance()

                    Using writer As New StringWriter(pooled.Builder)
                        Dim walker = New DocumentationCommentWalker(symbol, trivia.SyntaxTree, wellKnownElementNodes, writer, diagnostics)
                        walker.WriteHeaderAndVisit(symbol, trivia)
                    End Using

                    Return pooled.ToStringAndFree()
                End Function

                Private ReadOnly Property Compilation As VisualBasicCompilation
                    Get
                        Return Me._symbol.DeclaringCompilation
                    End Get
                End Property

                Private ReadOnly Property [Module] As SourceModuleSymbol
                    Get
                        Return DirectCast(Me.Compilation.SourceModule, SourceModuleSymbol)
                    End Get
                End Property

                Public Overrides Sub DefaultVisit(node As SyntaxNode)
                    Dim kind As SyntaxKind = node.Kind()

                    If kind = SyntaxKind.XmlCrefAttribute Then
                        Dim crefAttr = DirectCast(node, XmlCrefAttributeSyntax)

                        ' Write [cref="]
                        Visit(crefAttr.Name)
                        VisitToken(crefAttr.EqualsToken)
                        VisitToken(crefAttr.StartQuoteToken)

                        Dim reference As CrefReferenceSyntax = crefAttr.Reference
                        Debug.Assert(Not reference.ContainsDiagnostics)

                        Dim crefBinder = CreateDocumentationCommentBinderForSymbol(Me.Module, Me._symbol, Me._syntaxTree, DocumentationCommentBinder.BinderType.Cref)
                        Dim useSiteInfo = crefBinder.GetNewCompoundUseSiteInfo(_diagnostics)
                        Dim diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, _diagnostics.AccumulatesDependencies)
                        Dim result As ImmutableArray(Of Symbol) = crefBinder.BindInsideCrefAttributeValue(reference, preserveAliases:=False, diagnosticBag:=diagnostics, useSiteInfo:=useSiteInfo)
                        _diagnostics.AddDependencies(diagnostics)
                        _diagnostics.AddDependencies(useSiteInfo)

                        Dim errorLocations = diagnostics.DiagnosticBag.ToReadOnly.SelectAsArray(Function(x) x.Location).WhereAsArray(Function(x) x IsNot Nothing)
                        diagnostics.Free()

                        If Not useSiteInfo.Diagnostics.IsNullOrEmpty AndAlso Me._reportDiagnostics Then
                            ProcessErrorLocations(node, errorLocations, useSiteInfo, Nothing)
                        End If

                        If result.IsEmpty Then
                            ' We were not able to find anything by this name,
                            ' generate diagnostic and use erroneous 
                            ProcessErrorLocations(crefAttr, errorLocations, Nothing, ERRID.WRN_XMLDocCrefAttributeNotFound1)

                        ElseIf result.Length > 1 AndAlso reference.Signature IsNot Nothing Then
                            ' In strict mode we don't allow ambiguities
                            ProcessErrorLocations(crefAttr, errorLocations, Nothing, ERRID.WRN_XMLDocCrefAttributeNotFound1)
                        Else
                            ' Dev11 seems to ignore any ambiguity and use the first symbol it finds,
                            ' we have to repro this behavior
                            Dim compilation As VisualBasicCompilation = Me.Compilation

                            ' Some symbols found may not support doc-comment-ids and we just filter those out.
                            ' From the rest of the symbols we take the symbol with 'smallest' documentation 
                            ' comment id: we want to ensure that when we compile the same compilation several 
                            ' times we deterministically use/write the same documentation id each time, and it 
                            ' does not matter much which one it is. So instead of doing sophisticated location 
                            ' based sorting we just choose the lexically smallest documentation id.

                            Dim smallestSymbolCommentId As String = Nothing
                            Dim smallestSymbol As Symbol = Nothing
                            Dim errid As ERRID = ERRID.WRN_XMLDocCrefAttributeNotFound1

                            For Each symbol In result
                                If symbol.Kind = SymbolKind.TypeParameter Then
                                    errid = ERRID.WRN_XMLDocCrefToTypeParameter
                                    Continue For
                                End If

                                Dim candidateId As String = symbol.OriginalDefinition.GetDocumentationCommentId()

                                If candidateId IsNot Nothing AndAlso (smallestSymbolCommentId Is Nothing OrElse String.CompareOrdinal(smallestSymbolCommentId, candidateId) > 0) Then
                                    smallestSymbolCommentId = candidateId
                                    smallestSymbol = symbol
                                End If
                            Next

                            If smallestSymbolCommentId Is Nothing Then
                                ' some symbols were found, but none of them has id
                                ProcessErrorLocations(crefAttr, errorLocations, Nothing, errid)
                            Else
                                If Me._writer IsNot Nothing Then
                                    ' Write [<id>]
                                    Me._writer.Write(smallestSymbolCommentId)
                                End If

                                _diagnostics.AddAssembliesUsedByCrefTarget(smallestSymbol.OriginalDefinition)
                            End If
                        End If

                        ' Write ["]
                        VisitToken(crefAttr.EndQuoteToken)

                        ' We are done with this node
                        Return

                    ElseIf kind = SyntaxKind.XmlAttribute Then
                        Dim attr = DirectCast(node, XmlAttributeSyntax)
                        If Not attr.ContainsDiagnostics Then
                            ' Check name: this may be either 'cref' or 'name' in 'param', 
                            ' 'paramref', 'typeparam' or 'typeparamref' well-known tags

                            Dim attrName = DirectCast(attr.Name, XmlNameSyntax)
                            If DocumentationCommentXmlNames.AttributeEquals(attrName.LocalName.ValueText,
                                                                             DocumentationCommentXmlNames.CrefAttributeName) Then
                                ' If this is 'cref=', this node can be created for two reasons: 
                                '   (a) the value is represented in a form "X:SOME-ID-STRING", or
                                '   (b) the value between '"' is not a valid NameSyntax
                                '
                                ' in both cases we want just to put the result into documentation XML,
                                ' but in the second case we also generate a diagnostic and add '!:' in from 
                                ' of the value indicating wrong id
                                '
                                ' Other value types should have produced diagnostic on the syntax node

                                Dim str = DirectCast(attr.Value, XmlStringSyntax)
                                Dim strValue = Binder.GetXmlString(str.TextTokens)

                                Dim needError As Boolean = strValue.Length < 2 OrElse strValue(0) = ":"c OrElse strValue(1) <> ":"c

                                ' Write [cref="]
                                Visit(attr.Name)
                                VisitToken(attr.EqualsToken)
                                VisitToken(str.StartQuoteToken)

                                If needError AndAlso Me._reportDiagnostics Then
                                    Me._diagnostics.Add(ERRID.WRN_XMLDocCrefAttributeNotFound1, node.GetLocation(), strValue.Trim())
                                End If

                                If needError AndAlso Me._writer IsNot Nothing Then
                                    Me._writer.Write("!:")
                                End If

                                ' Write [<attr-value>]
                                For Each tk In str.TextTokens
                                    VisitToken(tk)
                                Next

                                ' Write ["]
                                VisitToken(str.EndQuoteToken)
                                ' We are done with this node
                                Return
                            End If
                            ' Otherwise go to default visitor

                        End If
                        ' Otherwise go to default visitor
                    End If

                    MyBase.DefaultVisit(node)
                End Sub

                Private Sub ProcessErrorLocations(node As SyntaxNode, errorLocations As ImmutableArray(Of Location), useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol), errid As Nullable(Of ERRID))
                    Dim crefAttr = TryCast(node, XmlCrefAttributeSyntax)
                    If crefAttr IsNot Nothing AndAlso errid.HasValue Then
                        If errorLocations.Length = 0 Then
                            ProcessBadNameInCrefAttribute(crefAttr, crefAttr.GetLocation, errid.Value)
                        Else
                            For Each location In errorLocations
                                ProcessBadNameInCrefAttribute(crefAttr, location, errid.Value)
                            Next
                        End If
                    ElseIf errorLocations.Length = 0 AndAlso useSiteInfo.Diagnostics IsNot Nothing Then
                        Me._diagnostics.AddDiagnostics(node, useSiteInfo)
                    ElseIf useSiteInfo.Diagnostics IsNot Nothing Then
                        For Each location In errorLocations
                            Me._diagnostics.AddDiagnostics(location, useSiteInfo)
                        Next
                    End If
                End Sub

                Private Sub ProcessBadNameInCrefAttribute(crefAttribute As XmlCrefAttributeSyntax, errorLocation As Location, errid As ERRID)
                    ' Write [!:<name>]
                    If Me._writer IsNot Nothing Then
                        Me._writer.Write("!:")
                    End If

                    Dim reference As VisualBasicSyntaxNode = crefAttribute.Reference

                    Visit(reference) ' This will write the name to XML

                    If Me._reportDiagnostics Then
                        Dim location = If(errorLocation, reference.GetLocation)
                        Me._diagnostics.Add(errid, location, reference.ToFullString().TrimEnd())
                    End If
                End Sub

                Public Overrides Sub VisitToken(token As SyntaxToken)
                    If Me._writer IsNot Nothing Then
                        token.WriteTo(Me._writer)
                    End If
                    MyBase.VisitToken(token)
                End Sub

            End Class

        End Class
    End Class
End Namespace
