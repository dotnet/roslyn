Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services
Imports Roslyn.Services.Host
Imports Roslyn.Utilities

Namespace Roslyn.Services.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryService
        ''' <summary>
        ''' Represents a syntax syntaxTree that only has a weak reference to its underlying data.
        ''' This way it can be passed around without forcing the underlying full syntaxTree to stay
        ''' alive. Think of it more as a key that can be used to identify a syntaxTree rather than
        ''' the syntaxTree itself.
        ''' </summary>
        Private Class WeakSyntaxTree
            Inherits SyntaxTree

            Private ReadOnly _syntaxTreeFactory As VisualBasicSyntaxTreeFactoryService
            Private ReadOnly _createText As Func(Of CancellationToken, IText)
            Private ReadOnly _fileName As String
            Private ReadOnly _options As ParseOptions
            Private ReadOnly _syntaxTreeRetainerFactory As IRetainerFactory(Of CommonSyntaxTree)
            Private ReadOnly _nullSyntaxReference As NullSyntaxReference

            ' protects mutable state of this class
            Private ReadOnly _semaphore As New SemaphoreSlim(1)

            ' We hold onto the underlying syntaxTree weakly.  That way if we're asked for it again and
            ' it's still alive, we can just return it.
            Private _weakTreeRetainer As IRetainer(Of CommonSyntaxTree)

            ' NOTE(cyrusn): Extremely subtle.  We need to guarantee that there cannot be two
            ' different 'root' nodes around for this syntaxTree at any time.  If that happened then we'd
            ' violate lots of invariants in our system.  However, if we only have the
            ' weakTreeReference then it's possible to get multiple roots.  One caller comes in and
            ' gets the first root, and then the actual syntax syntaxTree is reclaimed .  The next caller
            ' comes in and then gets a different underlying syntaxTree and a different underlying root.
            ' In order to prevent that, we must ensure that we always give back the same root
            ' (without forcing the root to stay alive).  To do that, we keep a weak reference around
            ' to the root.  This means that as long as the root is held by someone, anyone else will
            ' always see the same root if they ask for it.
            Private _weakRootReference As WeakReference(Of SyntaxNode)

            Public Sub New(syntaxTreeFactory As VisualBasicSyntaxTreeFactoryService,
                           createText As Func(Of CancellationToken, IText),
                           fileName As String,
                           options As ParseOptions,
                           syntaxTreeRetainerFactory As IRetainerFactory(Of CommonSyntaxTree))
                _syntaxTreeFactory = syntaxTreeFactory
                _createText = createText
                _fileName = fileName
                _options = options
                _syntaxTreeRetainerFactory = syntaxTreeRetainerFactory
                _nullSyntaxReference = New NullSyntaxReference(Me)
            End Sub

            Private Function GetUnderlyingTreeNoLock(Optional cancellationToken As CancellationToken = Nothing) As SyntaxTree
                ' See if we're still holding onto a syntaxTree we previously created
                Dim syntaxTree As SyntaxTree = DirectCast(If(_weakTreeRetainer IsNot Nothing, _weakTreeRetainer.GetValue(cancellationToken), Nothing), SyntaxTree)
                If syntaxTree IsNot Nothing Then
                    Return syntaxTree
                End If

                ' Ok. We need to actually parse out a syntaxTree.
                cancellationToken.ThrowIfCancellationRequested()
                Dim text = _createText(cancellationToken)

                cancellationToken.ThrowIfCancellationRequested()
                syntaxTree = _syntaxTreeFactory.CreateSyntaxTree(_fileName, text, _options, cancellationToken)

                _weakTreeRetainer = Me._syntaxTreeRetainerFactory.CreateRetainer(syntaxTree)
                Return syntaxTree
            End Function

            ' TODO: change the implementation to match C#
            Public Overrides ReadOnly Property FilePath As String
                Get
                    Return GetUnderlyingTreeNoLock().FilePath
                End Get
            End Property

            ' TODO: we should lock here!
            ' TODO: this should be a method
            Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As IText
                Return GetUnderlyingTreeNoLock().GetText(cancellationToken)
            End Function

            ' TODO: change the implementation to match C#
            Public Overrides ReadOnly Property Options As ParseOptions
                Get
                    Return GetUnderlyingTreeNoLock().Options
                End Get
            End Property

            Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
                ' NOTE(cyrusn): We can only have one thread executing here at a time.  Otherwise
                ' we could have a race condition where both created the new root and then one
                ' stomped on the other.  By locking here we ensure that only one will succeed in
                ' creating the new root. The other thread will either get that same root, or it
                ' might get a new root (but only if the first root isn't being held onto by
                ' anything).
                '
                ' We lock on the weakRootReference here as it is safe to do so.  It is private
                ' to us and allows us to save on all the extra space necessary to hold a lock.
                Using _semaphore.DisposableWait(cancellationToken)
                    Dim result As SyntaxNode = Nothing
                    If _weakRootReference Is Nothing OrElse Not _weakRootReference.TryGetTarget(result) Then
                        result = Me.CloneNodeAsRoot(GetUnderlyingTreeNoLock(cancellationToken).GetRoot(cancellationToken))
                        _weakRootReference = New WeakReference(Of SyntaxNode)(result)
                    End If

                    Return result
                End Using
            End Function

            Public Overrides Function TryGetRoot(ByRef root As Compilers.VisualBasic.SyntaxNode) As Boolean
                root = Nothing
                Dim wr = _weakRootReference
                Return wr IsNot Nothing AndAlso wr.TryGetTarget(root)
            End Function

            ' TODO: we should lock here!
            Public Overrides Function WithChange(newText As IText, ParamArray changes As TextChangeRange()) As SyntaxTree
                Return GetUnderlyingTreeNoLock().WithChange(newText, changes)
            End Function

            Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
                If node IsNot Nothing Then
                    If node.Span.Length = 0 Then
                        Return New PathSyntaxReference(Me, node)
                    Else
                        Return New PositionalSyntaxReference(Me, node)
                    End If
                Else
                    Return _nullSyntaxReference
                End If
            End Function
        End Class
    End Class
End Namespace
