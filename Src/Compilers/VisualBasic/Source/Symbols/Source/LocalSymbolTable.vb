Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' Instances of this class represent the local symbol table. The local symbol
    ''' table contains a set of LocalDeclSpace objects, that represent the blocks
    ''' that can contain local variable declarations. Each declaration space is associated with 
    ''' a SyntaxNode that is the node that contains that block. Each of these declaration spaces
    ''' is logically immutable once constructed, in that the set of local variables is created on demand.
    ''' 
    ''' UNDONE: need to handle the case of Option Explicit Off. Possible approach:
    '''    If Option Explicit is Off, there is a local declaration space associated with implicitly 
    '''    declared local variables. This declaration space is logically mutable, in that variables can be 
    '''    added to it. SHould this be the same as the top-most local declaration space, or be different?
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class LocalSymbolTable
        Private _declSpaces As New Dictionary(Of SyntaxNode, LocalDeclSpace)
        Private _syntaxTree As SyntaxTree

        Public Sub New(ByVal syntaxTree As SyntaxTree)
            _syntaxTree = syntaxTree
        End Sub

        Public ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _syntaxTree
            End Get
        End Property

        Public Function GetDeclSpaces() As IEnumerable(Of LocalDeclSpace)
            Return _declSpaces.Values
        End Function

        Public Function GetAllLocalVariables() As IEnumerable(Of LocalSymbol)
            Return (From declSpace In GetDeclSpaces(), localVar In declSpace.GetVariables() Select localVar)
        End Function

        Public Function GetDeclSpace(ByVal syntax As SyntaxNode) As LocalDeclSpace
            Dim declSpace As LocalDeclSpace = Nothing

            If _declSpaces.TryGetValue(syntax, declSpace) Then
                Return declSpace
            Else
                Return Nothing  ' should this instead throw, or move up the parent chain.
            End If
        End Function

        Private Class DeclarationWalker
            Inherits StatementSyntaxWalker(Of LocalDeclSpace)

            Private _localSymbolTable As LocalSymbolTable

            Public Sub New(ByVal localSymbolTable As LocalSymbolTable)
                _localSymbolTable = localSymbolTable
            End Sub

            Public Function WalkMethodStatements(ByVal methodBlock As MethodBlockSyntax) As LocalDeclSpace
                Dim newDeclSpace = New LocalDeclSpace(Nothing, _localSymbolTable)

                ' UNDONE: Should we add the local variable for the return value?
                VisitListWithoutNewDeclSpace(methodBlock.Statements, newDeclSpace)
                Return Nothing
            End Function

            ' Visit a list without opening a new decl space.
            Private Function VisitListWithoutNewDeclSpace(ByVal list As IEnumerable(Of SyntaxNode), ByVal currentDeclSpace As LocalDeclSpace) As Object
                For Each n In list
                    Visit(n, currentDeclSpace)
                Next
                Return Nothing
            End Function

            ' Called when a block of statements is encountered. By default, we open a new declaration space for the block.
            Protected Overrides Function VisitList(ByVal list As IEnumerable(Of SyntaxNode), ByVal currentDeclSpace As LocalDeclSpace) As Object
                Dim newDeclSpace = New LocalDeclSpace(currentDeclSpace, _localSymbolTable)
                VisitListWithoutNewDeclSpace(list, newDeclSpace)
                Return Nothing
            End Function

            ' UNDONE: we need to override declaration statements and add them to the current declaration space.

            ' UNDONE: below this line needs more work.

            Public Overrides Function VisitUsingBlock(ByVal node As Compilers.VisualBasic.UsingBlockSyntax, ByVal currentDeclSpace As LocalDeclSpace) As Object
                Visit(node.Begin, currentDeclSpace)
                VisitList(node.Statements, currentDeclSpace)
                Visit(node.End, currentDeclSpace)
                Return Nothing
            End Function

            Public Overrides Function VisitSyncLockBlock(ByVal node As Compilers.VisualBasic.SyncLockBlockSyntax, ByVal currentDeclSpace As LocalDeclSpace) As Object
                Visit(node.Begin, currentDeclSpace)
                VisitList(node.Statements, currentDeclSpace)
                Visit(node.End, currentDeclSpace)
                Return Nothing
            End Function

            ' CONSIDER: How to handle the "With" variable and its type. Should it get a special local variable in this stage?
            Public Overrides Function VisitWithBlock(ByVal node As Compilers.VisualBasic.WithBlockSyntax, ByVal currentDeclSpace As LocalDeclSpace) As Object
                Visit(node.Begin, currentDeclSpace)
                VisitList(node.Statements, currentDeclSpace)
                Visit(node.End, currentDeclSpace)
                Return Nothing
            End Function

            Public Overrides Function VisitCatchPart(ByVal node As Compilers.VisualBasic.CatchPartSyntax, ByVal currentDeclSpace As LocalDeclSpace) As Object
                Visit(node.Begin, currentDeclSpace)
                VisitList(node.Statements, currentDeclSpace)
                Return Nothing
            End Function

            ' For and For Each need special handling here also.

        End Class
    End Class

    ' Represents a single local scope in which variables are declared.
    Friend Class LocalDeclSpace
        Private _frozen As Boolean = False                       ' if true, cannot add any more symbols.
        Private ReadOnly _symbolTable As LocalSymbolTable
        Private ReadOnly _containingDeclSpace As LocalDeclSpace  ' may be Nothing for top-level.
        Private ReadOnly _variables As New Dictionary(Of String, LocalSymbol)  ' maybe only create this the first time a variable is added.

        Public Sub New(ByVal containingDeclSpace As LocalDeclSpace, ByVal symbolTable As LocalSymbolTable)
            _symbolTable = symbolTable
            _containingDeclSpace = containingDeclSpace
        End Sub

        Public ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _symbolTable.SyntaxTree
            End Get
        End Property

        Public Function GetVariables() As IEnumerable(Of LocalSymbol)
            Throw New NotImplementedException()
        End Function

        Public Function GetVariable(ByVal name As String) As LocalSymbol
            Throw New NotImplementedException()
        End Function

        Public Sub Freeze()
            _frozen = True
        End Sub

        Public Function AddLocalVariable(ByVal declaringIdentifier As SyntaxToken,
                                     ByVal asClauseOpt As AsClauseSyntax,
                                     ByVal initializerOpt As EqualsValueSyntax,
                                     ByVal inferType As Boolean) As LocalSymbol
            If _frozen Then
                Throw New InvalidOperationException("Tried to add local variable to a frozen declaration space")
            End If
            Dim symbol As LocalSymbol = New LocalSymbol(Me, declaringIdentifier, asClauseOpt, initializerOpt, inferType)
            _variables.Add(symbol.Name, symbol)  ' TODO: what to do about duplicates. I think we can ignore duplicates and then give an error when we encounter them in the main binding pass.
            Return symbol
        End Function
    End Class
End Namespace

