Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' Represents a location in source code or metadata. Used for the location of diagnostics and symbols.
    ''' </summary>
    Public MustInherit Class Location
        Implements ILocation

        Public MustOverride ReadOnly Property Kind As LocationKind Implements ILocation.Kind

        ''' <summary>
        ''' Indicates if the location refers to a particular place in source code.
        ''' </summary>
        Public MustOverride ReadOnly Property InSource As Boolean Implements ILocation.InSource

        ''' <summary>
        ''' Indicates if the location refers to metadata.
        ''' </summary>
        Public MustOverride ReadOnly Property InMetadata As Boolean Implements ILocation.InMetadata

        ''' <summary>
        ''' The location within the syntax tree that this error is associated with.
        ''' Throws an InvalidOperationException unless InSource is true.
        ''' </summary>
        Public MustOverride ReadOnly Property SourceSpan As TextSpan Implements ILocation.SourceSpan

        ''' <summary>
        ''' The syntax tree this error is located in. Throws
        ''' an InvalidOperationException unless InSource is true.
        ''' </summary>
        Public MustOverride ReadOnly Property SourceTree As SyntaxTree

        Private ReadOnly Property SourceTree1 As CommonSyntaxTree Implements ILocation.SourceTree
            Get
                Return Me.SourceTree
            End Get
        End Property

        ''' <summary>
        ''' Returns the metadata module the error is associated with. 
        ''' Throws an InvalidOperationException unless InMetadata is true.
        ''' </summary>
        Public MustOverride ReadOnly Property MetadataModule As ModuleSymbol

        ''' <summary>
        ''' Gets the location in terms of file name/line/column.
        ''' Throws an InvalidOperationException unless InSource is true.
        ''' </summary>
        ''' <param name="usePreprocessorDirectives">If true, the filename/line/column
        ''' reported takes into account #line directivees. If false, #line directives
        ''' are ignored.</param>
        ''' <returns></returns>
        Public MustOverride Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan Implements ILocation.GetLineSpan

        ' Derived classes should provide value equality semantics.
        Public MustOverride Overrides Function [Equals](obj As Object) As Boolean
        Public MustOverride Overrides Function GetHashCode() As Integer
    End Class

    ''' <summary>
    ''' A program location in source code.
    ''' </summary>
    Friend Class SourceLocation
        Inherits Location

        Private ReadOnly _syntaxTree As SyntaxTree
        Private ReadOnly _span As TextSpan

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.SourceFile
            End Get
        End Property


        Public Sub New(syntaxTree As SyntaxTree,
                       span As TextSpan)
            Contract.ThrowIfNull(syntaxTree)

            _syntaxTree = syntaxTree
            _span = span
        End Sub

        Public Sub New(syntaxTree As SyntaxTree,
                       node As SyntaxNode)
            Me.New(syntaxTree, node.Span)
        End Sub

        Public Sub New(syntaxTree As SyntaxTree,
                       token As SyntaxToken)
            Me.New(syntaxTree, token.Span)
        End Sub

        Public Sub New(syntaxTree As SyntaxTree,
                       nodeOrToken As SyntaxNodeOrToken)
            Me.New(syntaxTree, nodeOrToken.Span)
        End Sub

        Public Sub New(syntaxTree As SyntaxTree,
                       trivia As SyntaxTrivia)
            Me.New(syntaxTree, trivia.Span)
        End Sub

        Public Sub New(syntaxRef As SyntaxReference)
            Me.New(syntaxRef.SyntaxTree, syntaxRef.Span)

            ' If we're using a syntaxref, we don't have a node in hand, so we couldn't get equality
            ' on syntax node, so associatedNode shouldn't be set. We never use this constructor
            ' when binding executable code anywhere, so it has no use.
        End Sub

        Public Overrides ReadOnly Property InMetadata As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property InSource As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataModule As ModuleSymbol
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides ReadOnly Property SourceSpan As TextSpan
            Get
                Return _span
            End Get
        End Property

        Public Overrides ReadOnly Property SourceTree As SyntaxTree
            Get
                Return _syntaxTree
            End Get
        End Property

        Public Overrides Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan
            Return _syntaxTree.GetLineSpan(_span, usePreprocessorDirectives)
        End Function

        Public Overloads Function Equals(other As SourceLocation) As Boolean
            Return other IsNot Nothing AndAlso other._syntaxTree Is _syntaxTree AndAlso other._span.Equals(_span)
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, SourceLocation))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(_syntaxTree.GetHashCode(), _span.GetHashCode())
        End Function
    End Class

    ''' <summary>
    ''' A program location in speculative bound code. Right now, speculatively bound code
    ''' doesn't have a syntax tree associated with it. This probably will change in the future, but
    ''' for now this works.
    ''' </summary>
    Friend Class SpeculativeLocation
        Inherits Location

        Private ReadOnly _span As TextSpan

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.None  ' TODO: should be better kind than this?
            End Get
        End Property

        Public Sub New(span As TextSpan)
            _span = span
        End Sub

        Public Sub New(node As SyntaxNode)
            Me.New(node.Span)
            '_associatedNode = New WeakReference(Of SyntaxNode)(node)
            '_associateInParent = associateInParent
        End Sub

        Public Sub New(token As SyntaxToken)
            Me.New(token.Span)
            '_associatedNode = New WeakReference(Of SyntaxNode)(token.Parent)
            '_associateInParent = False
        End Sub

        Public Sub New(nodeOrToken As SyntaxNodeOrToken)
            Me.New(nodeOrToken.Span)
            'If nodeOrToken.IsNode Then
            '    _associatedNode = New WeakReference(Of SyntaxNode)(nodeOrToken.AsNode())
            'Else
            '    _associatedNode = New WeakReference(Of SyntaxNode)(nodeOrToken.AsToken().Parent)
            'End If

            '_associateInParent = False
        End Sub

        Public Sub New(trivia As SyntaxTrivia)
            Me.New(trivia.Span)
            '_associatedNode = New WeakReference(Of SyntaxNode)(trivia.Token.Parent)
            '_associateInParent = False
        End Sub

        Public Overrides ReadOnly Property InMetadata As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property InSource As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataModule As ModuleSymbol
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides ReadOnly Property SourceSpan As TextSpan
            Get
                Return _span
            End Get
        End Property

        Public Overrides ReadOnly Property SourceTree As SyntaxTree
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan
            Throw New InvalidOperationException()
        End Function

        Public Overloads Function Equals(other As SpeculativeLocation) As Boolean
            Return other IsNot Nothing AndAlso other._span.Equals(_span)
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, SpeculativeLocation))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _span.GetHashCode()
        End Function
    End Class

    ''' <summary>
    ''' A program location in metadata.
    ''' </summary>
    Friend NotInheritable Class MetadataLocation
        Inherits Location

        Private _module As ModuleSymbol

        Public Sub New([module] As ModuleSymbol)
            Contract.ThrowIfNull([module])
            _module = [module]
        End Sub

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.MetadataFile
            End Get
        End Property

        Public Overrides ReadOnly Property InSource As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property InMetadata As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataModule As ModuleSymbol
            Get
                Return _module
            End Get
        End Property

        Public Overrides ReadOnly Property SourceSpan As TextSpan
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides ReadOnly Property SourceTree As SyntaxTree
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan
            Throw New InvalidOperationException()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _module.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As MetadataLocation = TryCast(obj, MetadataLocation)
            Return (other IsNot Nothing) AndAlso (other._module Is Me._module)
        End Function
    End Class

    ''' <summary>
    ''' A class that represents no location at all. Useful for errors in command line options, for example.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class NoLocation
        Inherits Location

        Public Shared Singleton As New NoLocation

        Private Sub New()
            ' nothing to do.
        End Sub

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.None
            End Get
        End Property

        Public Overrides ReadOnly Property InSource As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property InMetadata As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataModule As ModuleSymbol
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides ReadOnly Property SourceSpan As TextSpan
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides ReadOnly Property SourceTree As SyntaxTree
            Get
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Overrides Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan
            Throw New InvalidOperationException()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return &H58914756  ' arbitrary number, since all NoLocation's are equal
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is NoLocation
        End Function

    End Class

End Namespace
