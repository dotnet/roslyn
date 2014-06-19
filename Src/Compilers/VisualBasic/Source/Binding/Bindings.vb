'*********************************************************
'**** WARNING WARNING WARNING WARNING WARNING WARNING ****
'**** WARNING WARNING WARNING WARNING WARNING WARNING ****
'**** WARNING WARNING WARNING WARNING WARNING WARNING ****
'*********************************************************
'
' The Bindings class is OBSOLETE, and it only continues to exists to keep services (e.g., type colorization)
' working until the new Binding APIs for VB are written. New code should not be using this and time spent
' maintaing this code is likely wasted (petergo, 1/11/11) [Nice date!]
'
'*********************************************************
'**** WARNING WARNING WARNING WARNING WARNING WARNING ****
'**** WARNING WARNING WARNING WARNING WARNING WARNING ****
'**** WARNING WARNING WARNING WARNING WARNING WARNING ****
'*********************************************************


Imports System.Collections.Generic
Imports System.Collections.Concurrent
Imports System.Collections.ObjectModel

Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.Internal.Contract
Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' A Bindings object is used to ask questions about the semantic meaning of the parts of a
    ''' program. When an answer is a named symbol that is reachable by traversing from the root of
    ''' the symbol table, (that is, from an AssemblySymbol of the Compilation), that symbol will be
    ''' returned (i.e. the returned value will be reference-equal to one reachable from the root of
    ''' the symbol table).  Symbols representing entities without names (e.g. array-of-int) may or
    ''' may not exhibit reference equality.
    ''' 
    ''' However, some named symbols (such as local variables) are not reachable from the root. These
    ''' symbols are visible as answers to binding questions asked of the Bindings object. When the
    ''' same Binding object is used, the answers exhibit reference-equality.  However, the cost is
    ''' that the Bindings object retains a great deal of information about its previous answers.  If
    ''' the client needs to ask repeated questions about the body of a single method, it is a good
    ''' idea to reuse a single Bindings object, so that it can compute and cache the binding for the
    ''' method body, and then the client should discard the Bindings object when done with that
    ''' method body.
    ''' </summary>
    Public Class Bindings
        Private ReadOnly _compilation As Compilation
        Private ReadOnly _sourceModule As SourceModuleSymbol
        Private ReadOnly _tree As SyntaxTree
        Private _binderCache As BinderCache

        Friend Sub New(ByVal compilation As Compilation, ByVal sourceModule As SourceModuleSymbol, ByVal tree As SyntaxTree)
            _compilation = compilation
            _sourceModule = sourceModule
            _binderCache = New BinderCache(sourceModule, tree)
            _tree = tree
        End Sub

        Public ReadOnly Property Compilation As Compilation
            Get
                Return _compilation
            End Get
        End Property


        ''' <summary>
        ''' Get all diagnostics inside the assocaited syntax tree. This includes diagnostics from parsing, declarations, and
        ''' the bodies of methods. Getting all the diagnostics is potentially a length operations, as it requires parsing and
        ''' compiling all the code. The set of diagnostics is not cached, so each call to this method will recompile all
        ''' methods.
        ''' </summary>
        ''' <param name="cancellationToken">Cancellation token to allow cancelling the operation.</param>
        Public Function GetDiagnostics(Optional ByVal cancellationToken As CancellationToken = Nothing) As IEnumerable(Of Diagnostic)
            Return _compilation.GetDiagnosticsForTree(CompilationStage.Compile, _tree, cancellationToken)
        End Function

        ''' <summary>
        ''' Get parse and decalrations diagnostics inside the associated syntax tree. This includes diagnostics from parsing, declarations, BUT NOT
        ''' the bodies of methods or initializers. The set of declaration diagnostics is cached, so calling this method a second time
        ''' should be fast.
        ''' </summary>
        ''' <param name="cancellationToken">Cancellation token to allow cancelling the operation.</param>
        Public Function GetDeclarationDiagnostics(Optional ByVal cancellationToken As CancellationToken = Nothing) As IEnumerable(Of Diagnostic)
            Return _compilation.GetDiagnosticsForTree(CompilationStage.Declare, _tree, cancellationToken)
        End Function

        Friend Function GetBinderForNode(ByVal node As SyntaxNode) As Binder
            _sourceModule.ValidateSyntaxTree(_tree)

            Return _binderCache.GetBinderForNode(node)
        End Function

        ''' <summary>
        ''' Given a type declaration, get the corresponding type symbol.
        ''' </summary>
        Public Function GetDeclaredSymbol(ByVal declarationSyntax As TypeDeclarationSyntax) As NamedTypeSymbol
            Dim binder As Binder = GetBinderForNode(declarationSyntax.Parent)

            If binder IsNot Nothing AndAlso TypeOf binder Is NamedTypeBinder Then
                Return DirectCast(binder.ContainingType, NamedTypeSymbol)
            Else
                Return Nothing  ' Can this happen? Maybe in some weird error cases.
            End If
        End Function

        ''' <summary>
        ''' Get the binding for a type appearing in the source.
        ''' </summary>
        Public Function LookupType(ByVal type As TypeSyntax) As BindingInfo
            ' Set up the binding context.
            Dim diagnosticBag As New DiagnosticBag()
            Dim binder As Binder = GetBinderForNode(type)

            ' Attempt to bind the type
            Dim resultSymbol As TypeSymbol = binder.BindTypeSyntax(type, diagnosticBag)

            ' Create the result.
            Return New BindingInfo(resultSymbol, GetAllSymbols(resultSymbol), diagnosticBag.GetDiagnostics())
        End Function

        ''' <summary>
        ''' Get the binding for a type appearing in the source, as if it were at the given location in the source.
        ''' </summary>
        Public Function BindType(ByVal location As SyntaxNode, ByVal type As TypeSyntax) As BindingInfo
            ' Set up the binding context.
            Dim diagnosticBag As New DiagnosticBag()
            Dim binder As Binder = GetBinderForNode(location)

            ' Attempt to bind the type
            Dim resultSymbol As TypeSymbol = binder.BindTypeSyntax(type, diagnosticBag)

            ' Create the result.
            Return New BindingInfo(resultSymbol, GetAllSymbols(resultSymbol), diagnosticBag.GetDiagnostics())
        End Function

        ' Get all the possible symbols this type symbol might refer to. This is just the symbols
        ' itself unless the the type symbol is an error symbol, in which case we look at the
        ' attached DiagnosticInfo to get any possible (incorrect) symbols this might have been.
        Private Function GetAllSymbols(ByVal typeSymbol As TypeSymbol) As IEnumerable(Of Symbol)
            Assert(typeSymbol IsNot Nothing)

            If typeSymbol.Kind = SymbolKind.ErrorType Then
                Dim errorInfo = DirectCast(typeSymbol, ErrorTypeSymbol).ErrorInfo

                ' Get any bad symbols from the attached error information.
                If TypeOf errorInfo Is BadSymbolDiagnostic Then
                    Return New Symbol() {DirectCast(errorInfo, BadSymbolDiagnostic).BadSymbol}
                ElseIf TypeOf errorInfo Is AmbiguousSymbolDiagnostic Then
                    Return DirectCast(errorInfo, AmbiguousSymbolDiagnostic).AmbiguousSymbols.AsEnumerable()
                Else
                    ' No bad symbols at all.
                    Return New Symbol() {}
                End If
            Else
                ' Type symbol is good on its own.
                Return {typeSymbol}
            End If
        End Function

        ' For API parity with C#.  Remove once we switch over to the SyntaxBinding type
        Public Function GetBindingInfo(ByVal expression As ExpressionSyntax) As BindingInfo
            If TypeOf expression Is TypeSyntax Then
                Return LookupType(DirectCast(expression, TypeSyntax))
            End If

            Throw New NotImplementedException()
        End Function

        ' For API parity with C#.  Remove once we switch over to the SyntaxBinding type
        Public Function GetBindingInfoInParent(ByVal expression As ExpressionSyntax) As BindingInfo
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
