' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Blender = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Blender
Imports Parser = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Parser
Imports Scanner = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Scanner

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The parsed representation of a Visual Basic source document.
    ''' </summary>
    Partial Public MustInherit Class VisualBasicSyntaxTree
        Inherits SyntaxTree

        ''' <summary>
        ''' The options used by the parser to produce the syntax tree.
        ''' </summary>
        Public MustOverride Shadows ReadOnly Property Options As VisualBasicParseOptions

        ''' <summary>
        ''' Returns True for MyTemplate automatically added by compiler.
        ''' </summary>
        Friend Overridable ReadOnly Property IsMyTemplate As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Produces a clone of a <see cref="VisualBasicSyntaxNode"/> which will have current syntax tree as its parent.
        ''' 
        ''' Caller must guarantee that if the same instance of <see cref="VisualBasicSyntaxNode"/> makes multiple calls
        ''' to this function, only one result is observable.
        ''' </summary>
        ''' <typeparam name="T">Type of the syntax node.</typeparam>
        ''' <param name="node">The original syntax node.</param>
        ''' <returns>A clone of the original syntax node that has current <see cref="VisualBasicSyntaxTree"/> as its parent.</returns>
        Protected Function CloneNodeAsRoot(Of T As VisualBasicSyntaxNode)(node As T) As T
            Return VisualBasicSyntaxNode.CloneNodeAsRoot(node, Me)
        End Function

        ''' <summary>
        ''' Gets the root node of the syntax tree.
        ''' </summary>
        Public MustOverride Shadows Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode

        ''' <summary>
        ''' Gets the root node of the syntax tree asynchronously.
        ''' </summary>
        ''' <remarks>
        ''' By default, the work associated with this method will be executed immediately on the current thread.
        ''' Implementations that wish to schedule this work differently should override <see cref="GetRootAsync(CancellationToken)"/>.
        ''' </remarks>
        Public Overridable Shadows Function GetRootAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of VisualBasicSyntaxNode)
            Dim node As VisualBasicSyntaxNode = Nothing
            Return Task.FromResult(If(Me.TryGetRoot(node), node, Me.GetRoot(cancellationToken)))
        End Function

        ''' <summary>
        ''' Gets the root node of the syntax tree if it is already available.
        ''' </summary>
        Public MustOverride Shadows Function TryGetRoot(ByRef root As VisualBasicSyntaxNode) As Boolean

        ''' <summary>
        ''' Gets the root of the syntax tree statically typed as <see cref="CompilationUnitSyntax"/>.
        ''' </summary>
        ''' <remarks>
        ''' Ensure that <see cref="SyntaxTree.HasCompilationUnitRoot"/> is true for this tree prior to invoking this method.
        ''' </remarks>
        ''' <exception cref="InvalidCastException">Throws this exception if <see cref="SyntaxTree.HasCompilationUnitRoot"/> is false.</exception>
        Public Function GetCompilationUnitRoot(Optional cancellationToken As CancellationToken = Nothing) As CompilationUnitSyntax
            Return DirectCast(Me.GetRoot(cancellationToken), CompilationUnitSyntax)
        End Function

        Friend ReadOnly Property HasReferenceDirectives As Boolean
            Get
                Debug.Assert(Me.HasCompilationUnitRoot)

                Return Options.Kind = SourceCodeKind.Script AndAlso GetCompilationUnitRoot().GetReferenceDirectives().Count > 0
            End Get
        End Property

        ''' <summary>
        ''' Creates a new syntax based off this tree using a new source text.
        ''' </summary>
        ''' <remarks>
        ''' If the new source text is a minor change from the current source text an incremental parse will occur
        ''' reusing most of the current syntax tree internal data.  Otherwise, a full parse will occur using the new
        ''' source text.
        ''' </remarks>
        Public Overrides Function WithChangedText(newText As SourceText) As SyntaxTree
            ' try to find the changes between the old text and the new text.
            Dim oldText As SourceText = Nothing
            If Me.TryGetText(oldText) Then
                Return Me.WithChanges(newText, newText.GetChangeRanges(oldText).ToArray())
            End If

            ' if we do not easily know the old text, then specify entire text as changed so we do a full reparse.
            Return Me.WithChanges(newText, {New TextChangeRange(New TextSpan(0, Me.Length), newText.Length)})
        End Function

        ''' <summary>
        ''' Applies a text change to this syntax tree, returning a new syntax tree with the changes applied to it.
        ''' </summary>
        Private Function WithChanges(newText As SourceText, changes As TextChangeRange()) As SyntaxTree
            If changes Is Nothing Then
                Throw New ArgumentNullException(NameOf(changes))
            End If

            Dim scanner As Scanner
            If changes.Length = 1 AndAlso changes(0).Span = New TextSpan(0, Me.Length) AndAlso changes(0).NewLength = newText.Length Then
                ' if entire text is replaced then do a full reparse
                scanner = New Scanner(newText, Options)
            Else
                scanner = New Blender(newText, changes, Me, Me.Options)
            End If

            Dim node As InternalSyntax.CompilationUnitSyntax
            Using scanner
                node = New Parser(scanner).ParseCompilationUnit()
            End Using

            Dim root = DirectCast(node.CreateRed(Nothing, 0), CompilationUnitSyntax)
            ' Diagnostic options are obsolete, but preserved for compat
#Disable Warning BC40000
            Dim tree = New ParsedSyntaxTree(
                newText,
                newText.Encoding,
                newText.ChecksumAlgorithm,
                FilePath,
                Options,
                root,
                isMyTemplate:=False,
                DiagnosticOptions)
#Enable Warning BC40000

            tree.VerifySource(changes)
            Return tree
        End Function

        Private _lineDirectiveMap As VisualBasicLineDirectiveMap  ' created on demand

        Friend Shared ReadOnly Dummy As VisualBasicSyntaxTree = New DummySyntaxTree()

        Friend Shared ReadOnly DummyReference As SyntaxReference = Dummy.GetReference(Dummy.GetRoot())

        ''' <summary>
        ''' Creates a new syntax tree from a syntax node.
        ''' </summary>
        ''' <param name="diagnosticOptions">An obsolete parameter. Diagnostic options should now be passed with <see cref="CompilationOptions.SyntaxTreeOptionsProvider"/></param>
        Public Shared Function Create(root As VisualBasicSyntaxNode,
                                      Optional options As VisualBasicParseOptions = Nothing,
                                      Optional path As String = "",
                                      Optional encoding As Encoding = Nothing,
                                      Optional diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing) As SyntaxTree
            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If

            Return New ParsedSyntaxTree(
                textOpt:=Nothing,
                encodingOpt:=encoding,
                checksumAlgorithm:=SourceHashAlgorithm.Sha1,
                path:=path,
                options:=If(options, VisualBasicParseOptions.Default),
                syntaxRoot:=root,
                isMyTemplate:=False,
                diagnosticOptions)
        End Function

        ''' <summary>
        ''' <para>
        ''' Internal helper for <see cref="VisualBasicSyntaxNode"/> class to create a new syntax tree rooted at the given root node.
        ''' This method does not create a clone of the given root, but instead preserves its reference identity.
        ''' </para>
        ''' <para>NOTE: This method is only intended to be used from <see cref="SyntaxNode.SyntaxTree"/> property.</para>
        ''' <para>NOTE: Do not use this method elsewhere, instead use <see cref="Create"/> method for creating a syntax tree.</para>
        ''' </summary>
        Friend Shared Function CreateWithoutClone(root As VisualBasicSyntaxNode) As SyntaxTree
            Debug.Assert(root IsNot Nothing)

            Return New ParsedSyntaxTree(
                textOpt:=Nothing,
                path:="",
                encodingOpt:=Nothing,
                checksumAlgorithm:=SourceHashAlgorithm.Sha1,
                options:=VisualBasicParseOptions.Default,
                syntaxRoot:=root,
                isMyTemplate:=False,
                diagnosticOptions:=Nothing,
                cloneRoot:=False)
        End Function

        Friend Shared Function ParseTextLazy(text As SourceText,
                                         Optional options As VisualBasicParseOptions = Nothing,
                                         Optional path As String = "") As SyntaxTree
            Return New LazySyntaxTree(text, If(options, VisualBasicParseOptions.Default), path, Nothing)
        End Function

        ''' <param name="diagnosticOptions">An obsolete parameter. Diagnostic options should now be passed with <see cref="CompilationOptions.SyntaxTreeOptionsProvider"/></param>
        Public Shared Function ParseText(text As String,
                                         Optional options As VisualBasicParseOptions = Nothing,
                                         Optional path As String = "",
                                         Optional encoding As Encoding = Nothing,
                                         Optional diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
                                         Optional cancellationToken As CancellationToken = Nothing) As SyntaxTree
            Return ParseText(text, isMyTemplate:=False, options, path, encoding, diagnosticOptions, cancellationToken)
        End Function

        Friend Shared Function ParseText(text As String,
                                         isMyTemplate As Boolean,
                                         Optional options As VisualBasicParseOptions = Nothing,
                                         Optional path As String = "",
                                         Optional encoding As Encoding = Nothing,
                                         Optional diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
                                         Optional cancellationToken As CancellationToken = Nothing) As SyntaxTree
            Return ParseText(
                SourceText.From(text, encoding),
                isMyTemplate,
                options,
                path,
                diagnosticOptions,
                cancellationToken)
        End Function

        ''' <summary>
        ''' Creates a syntax tree by parsing the source text.
        ''' </summary>
        Public Shared Function ParseText(text As SourceText,
                                         Optional options As VisualBasicParseOptions = Nothing,
                                         Optional path As String = "",
                                         Optional diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
                                         Optional cancellationToken As CancellationToken = Nothing) As SyntaxTree
            Return ParseText(
                text,
                isMyTemplate:=False,
                options,
                path,
                diagnosticOptions:=diagnosticOptions,
                cancellationToken)
        End Function

        Friend Shared Function ParseText(
            text As SourceText,
            isMyTemplate As Boolean,
            Optional parseOptions As VisualBasicParseOptions = Nothing,
            Optional path As String = "",
            Optional diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
            Optional cancellationToken As CancellationToken = Nothing) As SyntaxTree

            If text Is Nothing Then
                Throw New ArgumentNullException(NameOf(text))
            End If

            parseOptions = If(parseOptions, VisualBasicParseOptions.Default)

            Dim node As InternalSyntax.CompilationUnitSyntax
            Using parser As New Parser(text, parseOptions, cancellationToken)
                node = parser.ParseCompilationUnit()
            End Using

            Dim root = DirectCast(node.CreateRed(Nothing, 0), CompilationUnitSyntax)
            Dim tree = New ParsedSyntaxTree(
                text,
                text.Encoding,
                text.ChecksumAlgorithm,
                path,
                parseOptions,
                root,
                isMyTemplate,
                diagnosticOptions:=diagnosticOptions)

            tree.VerifySource()
            Return tree
        End Function

        ''' <summary>
        ''' Gets a list of all the diagnostics in the sub tree that has the specified node as its root.
        ''' </summary>
        ''' <remarks>
        ''' This method does not filter diagnostics based on compiler options like /nowarn, /warnaserror etc.
        ''' </remarks>
        Public Overrides Function GetDiagnostics(node As SyntaxNode) As IEnumerable(Of Diagnostic)
            If node Is Nothing Then Throw New ArgumentNullException(NameOf(node))

            Return Me.GetDiagnostics(DirectCast(node.Green, InternalSyntax.VisualBasicSyntaxNode), DirectCast(node, VisualBasicSyntaxNode).Position, InDocumentationComment(node))
        End Function

        ''' <summary>
        ''' Gets a list of all the diagnostics associated with the token and any related trivia.
        ''' </summary>
        ''' <remarks>
        ''' This method does not filter diagnostics based on compiler options like /nowarn, /warnaserror etc.
        ''' </remarks>
        Public Overrides Function GetDiagnostics(token As SyntaxToken) As IEnumerable(Of Diagnostic)
            Return Me.GetDiagnostics(DirectCast(token.Node, InternalSyntax.SyntaxToken), token.Position, InDocumentationComment(token))
        End Function

        ''' <summary>
        ''' Gets a list of all the diagnostics associated with the trivia.
        ''' </summary>
        ''' <remarks>
        ''' This method does not filter diagnostics based on compiler options like /nowarn, /warnaserror etc.
        ''' </remarks>
        Public Overrides Function GetDiagnostics(trivia As SyntaxTrivia) As IEnumerable(Of Diagnostic)
            Return Me.GetDiagnostics(DirectCast(trivia.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), trivia.Position, InDocumentationComment(trivia))
        End Function

        ''' <summary>
        ''' Gets a list of all the diagnostics in either the sub tree that has the specified node as its root or
        ''' associated with the token and its related trivia.
        ''' </summary>
        ''' <remarks>
        ''' This method does not filter diagnostics based on compiler options like /nowarn, /warnaserror etc.
        ''' </remarks>
        Public Overrides Function GetDiagnostics(nodeOrToken As SyntaxNodeOrToken) As IEnumerable(Of Diagnostic)
            Return Me.GetDiagnostics(DirectCast(nodeOrToken.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), nodeOrToken.Position, InDocumentationComment(nodeOrToken))
        End Function

        ''' <summary>
        ''' Gets a list of all the diagnostics in the syntax tree.
        ''' </summary>
        ''' <remarks>
        ''' This method does not filter diagnostics based on compiler options like /nowarn, /warnaserror etc.
        ''' </remarks>
        Public Overrides Function GetDiagnostics(Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of Diagnostic)
            Return Me.GetDiagnostics(Me.GetRoot(cancellationToken).VbGreen, 0, False)
        End Function

        Friend Iterator Function EnumerateDiagnostics(node As InternalSyntax.VisualBasicSyntaxNode, position As Integer, InDocumentationComment As Boolean) As IEnumerable(Of Diagnostic)
            Dim enumerator As New SyntaxTreeDiagnosticEnumerator(Me, node, position, InDocumentationComment)

            While enumerator.MoveNext()
                Yield enumerator.Current
            End While
        End Function

        Friend Overloads Function GetDiagnostics(node As InternalSyntax.VisualBasicSyntaxNode, position As Integer, InDocumentationComment As Boolean) As IEnumerable(Of Diagnostic)

            'This is part of the public contract for GetDiagnostics(xxx)
            If node Is Nothing Then Throw New InvalidOperationException()

            If node.ContainsDiagnostics Then
                Return EnumerateDiagnostics(node, position, InDocumentationComment)
            End If
            Return SpecializedCollections.EmptyEnumerable(Of Diagnostic)()
        End Function

        Private Function InDocumentationComment(node As SyntaxNode) As Boolean
            Dim foundXml As Boolean = False

            While node IsNot Nothing
                If Not SyntaxFacts.IsXmlSyntax(node.Kind()) Then
                    Exit While
                End If

                foundXml = True
                node = node.Parent
            End While

            Return foundXml AndAlso node IsNot Nothing AndAlso node.IsKind(SyntaxKind.DocumentationCommentTrivia)
        End Function

        Private Function InDocumentationComment(node As SyntaxNodeOrToken) As Boolean
            If node.IsToken Then
                Return InDocumentationComment(node.AsToken)
            End If

            Return InDocumentationComment(node.AsNode)
        End Function

        Private Function InDocumentationComment(token As SyntaxToken) As Boolean
            Return InDocumentationComment(token.Parent)
        End Function

        Private Function InDocumentationComment(trivia As SyntaxTrivia) As Boolean
            Return InDocumentationComment(CType(trivia.Token, SyntaxToken))
        End Function

        Private Function GetDirectiveMap() As VisualBasicLineDirectiveMap
            If _lineDirectiveMap Is Nothing Then
                ' Create the line directive map on demand.
                Interlocked.CompareExchange(_lineDirectiveMap, New VisualBasicLineDirectiveMap(Me), Nothing)
            End If

            Return _lineDirectiveMap
        End Function

        ''' <summary>
        ''' Gets the location in terms of path, line and column for a given <paramref name="span"/>.
        ''' </summary>
        ''' <param name="span">Span within the tree.</param>
        ''' <param name="cancellationToken">Cancellation token.</param>
        ''' <returns>
        ''' <see cref="FileLinePositionSpan"/> that contains path, line and column information.
        ''' </returns>
        ''' <remarks>
        ''' The values are not affected by line mapping directives (<c>#ExternalSource</c>).
        ''' </remarks>
        Public Overrides Function GetLineSpan(span As TextSpan, Optional cancellationToken As CancellationToken = Nothing) As FileLinePositionSpan
            Return New FileLinePositionSpan(Me.FilePath, GetLinePosition(span.Start), GetLinePosition(span.End))
        End Function

        ''' <summary>
        ''' Gets the location in terms of path, line and column after applying source line mapping directives (<c>#ExternalSource</c>).
        ''' </summary>
        ''' <param name="span">Span within the tree.</param>
        ''' <param name="cancellationToken">Cancellation token.</param>
        ''' <returns>
        ''' A valid <see cref="FileLinePositionSpan"/> that contains path, line and column information.
        '''
        ''' If the location path is not mapped the resulting path is <see cref="String.Empty"/>.
        ''' </returns>
        Public Overrides Function GetMappedLineSpan(span As TextSpan, Optional cancellationToken As CancellationToken = Nothing) As FileLinePositionSpan
            Return GetDirectiveMap().TranslateSpan(GetText(cancellationToken), FilePath, span)
        End Function

        ''' <inheritdoc/>
        Public Overrides Function GetLineVisibility(position As Integer, Optional cancellationToken As CancellationToken = Nothing) As LineVisibility
            Return GetDirectiveMap().GetLineVisibility(Me.GetText(cancellationToken), position)
        End Function

        Friend Overrides Function GetMappedLineSpanAndVisibility(span As TextSpan, ByRef isHiddenPosition As Boolean) As FileLinePositionSpan
            Return GetDirectiveMap().TranslateSpanAndVisibility(Me.GetText(), Me.FilePath, span, isHiddenPosition)
        End Function

        ''' <inheritdoc/>
        Public Overrides Function GetLineMappings(Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of LineMapping)
            ' Currently not implemented: https://github.com/dotnet/roslyn/issues/53024
            'Dim map = GetDirectiveMap()
            'Debug.Assert(map.Entries.Length >= 1)
            'Return If(map.Entries.Length = 1, Array.Empty(Of LineMapping)(), map.GetLineMappings(GetText(cancellationToken).Lines))
            Return Array.Empty(Of LineMapping)()
        End Function

        Public Overrides Function HasHiddenRegions() As Boolean
            Return GetDirectiveMap().HasAnyHiddenRegions()
        End Function

        ' Gets the reporting state for a warning (diagnostic) at a given source location based on warning directives.
        Friend Function GetWarningState(id As String, position As Integer) As ReportDiagnostic
            If _lazyWarningStateMap Is Nothing Then
                ' Create the warning state map on demand.
                Interlocked.CompareExchange(_lazyWarningStateMap, New VisualBasicWarningStateMap(Me), Nothing)
            End If

            Return _lazyWarningStateMap.GetWarningState(id, position)
        End Function

        Private _lazyWarningStateMap As VisualBasicWarningStateMap

        Private Function GetLinePosition(position As Integer) As LinePosition
            Return Me.GetText().Lines.GetLinePosition(position)
        End Function

        ''' <summary>
        ''' Gets a location for the specified text <paramref name="span"/>.
        ''' </summary>
        Public Overrides Function GetLocation(span As TextSpan) As Location
            If Me.IsEmbeddedSyntaxTree Then
                Return New EmbeddedTreeLocation(Me.GetEmbeddedKind, span)
            ElseIf Me.IsMyTemplate Then
                Return New MyTemplateLocation(Me, span)
            End If

            Return New SourceLocation(Me, span)
        End Function

        ''' <summary>
        ''' Determines if two trees are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="tree">The tree to compare against.</param>
        ''' <param name="topLevel">
        ''' If true then the trees are equivalent if the contained nodes and tokens declaring metadata visible symbolic information are equivalent,
        ''' ignoring any differences of nodes inside method bodies or initializer expressions, otherwise all nodes and tokens must be equivalent.
        ''' </param>
        Public Overrides Function IsEquivalentTo(tree As SyntaxTree, Optional topLevel As Boolean = False) As Boolean
            Return SyntaxFactory.AreEquivalent(Me, tree, topLevel)
        End Function

        ''' <summary>
        ''' Produces a pessimistic list of spans that denote the regions of text in this tree that
        ''' are changed from the text of the old tree.
        ''' </summary>
        ''' <param name="oldTree">The old tree. Cannot be <c>Nothing</c>.</param>
        ''' <remarks>The list is pessimistic because it may claim more or larger regions than actually changed.</remarks>
        Public Overrides Function GetChangedSpans(oldTree As SyntaxTree) As IList(Of TextSpan)
            If oldTree Is Nothing Then
                Throw New ArgumentNullException(NameOf(oldTree))
            End If

            Return SyntaxDiffer.GetPossiblyDifferentTextSpans(oldTree.GetRoot(), Me.GetRoot())
        End Function

        ''' <summary>
        ''' Gets a list of text changes that when applied to the old tree produce this tree.
        ''' </summary>
        ''' <param name="oldTree">The old tree. Cannot be <c>Nothing</c>.</param>
        ''' <remarks>The list of changes may be different than the original changes that produced this tree.</remarks>
        Public Overrides Function GetChanges(oldTree As SyntaxTree) As IList(Of TextChange)
            If oldTree Is Nothing Then
                Throw New ArgumentNullException(NameOf(oldTree))
            End If

            Return SyntaxDiffer.GetTextChanges(oldTree, Me)
        End Function

#Region "SyntaxTree"

        Protected Overrides Function GetRootCore(CancellationToken As CancellationToken) As SyntaxNode
            Return Me.GetRoot(CancellationToken)
        End Function

        Protected Overrides Async Function GetRootAsyncCore(cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Return Await Me.GetRootAsync(cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function TryGetRootCore(ByRef root As SyntaxNode) As Boolean
            Dim node As VisualBasicSyntaxNode = Nothing
            If Me.TryGetRoot(node) Then
                root = node
                Return True
            Else
                root = Nothing
                Return False
            End If
        End Function

        Protected Overrides ReadOnly Property OptionsCore As ParseOptions
            Get
                Return Me.Options
            End Get
        End Property

#End Region

#Region "Preprocessor Symbols"
        ' Lazily created
        Private _lazySymbolsMap As ConditionalSymbolsMap = ConditionalSymbolsMap.Uninitialized

        Private ReadOnly Property ConditionalSymbols As ConditionalSymbolsMap
            Get
                If _lazySymbolsMap Is ConditionalSymbolsMap.Uninitialized Then
                    Interlocked.CompareExchange(_lazySymbolsMap, ConditionalSymbolsMap.Create(Me.GetRoot(CancellationToken.None), Options), ConditionalSymbolsMap.Uninitialized)
                End If

                Return _lazySymbolsMap
            End Get
        End Property

        Friend Function IsAnyPreprocessorSymbolDefined(conditionalSymbolNames As IEnumerable(Of String), atNode As SyntaxNodeOrToken) As Boolean
            Debug.Assert(conditionalSymbolNames IsNot Nothing)

            Dim conditionalSymbolsMap As ConditionalSymbolsMap = Me.ConditionalSymbols
            If conditionalSymbolsMap Is Nothing Then
                Return False
            End If

            For Each conditionalSymbolName As String In conditionalSymbolNames
                If conditionalSymbolsMap.IsConditionalSymbolDefined(conditionalSymbolName, atNode) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Friend Function GetPreprocessingSymbolInfo(identifierNode As IdentifierNameSyntax) As VisualBasicPreprocessingSymbolInfo
            Dim conditionalSymbolName As String = identifierNode.Identifier.ValueText
            Dim conditionalSymbols As ConditionalSymbolsMap = Me.ConditionalSymbols

            Return If(conditionalSymbols Is Nothing, VisualBasicPreprocessingSymbolInfo.None, conditionalSymbols.GetPreprocessingSymbolInfo(conditionalSymbolName, identifierNode))
        End Function

#End Region

        ' 2.8 BACK COMPAT OVERLOAD -- DO NOT MODIFY
        <EditorBrowsable(EditorBrowsableState.Never)>
        Public Shared Function ParseText(text As String,
                                         options As VisualBasicParseOptions,
                                         path As String,
                                         encoding As Encoding,
                                         cancellationToken As CancellationToken) As SyntaxTree
            Return ParseText(text, options, path, encoding, diagnosticOptions:=Nothing, cancellationToken)
        End Function

        ' 2.8 BACK COMPAT OVERLOAD -- DO NOT MODIFY
        <EditorBrowsable(EditorBrowsableState.Never)>
        Public Shared Function ParseText(text As SourceText,
                                         options As VisualBasicParseOptions,
                                         path As String,
                                         cancellationToken As CancellationToken) As SyntaxTree
            Return ParseText(text, options, path, diagnosticOptions:=Nothing, cancellationToken)
        End Function

        ' 2.8 BACK COMPAT OVERLOAD -- DO NOT MODIFY
        <EditorBrowsable(EditorBrowsableState.Never)>
        Public Shared Function Create(root As VisualBasicSyntaxNode,
                                      options As VisualBasicParseOptions,
                                      path As String,
                                      encoding As Encoding) As SyntaxTree
            Return Create(root, options, path, encoding, diagnosticOptions:=Nothing)
        End Function
    End Class
End Namespace
