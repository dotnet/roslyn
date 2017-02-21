Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Test.Utilities.CommonTestBase

Namespace Global.Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class Unit
        'Private Property _Files As Immutable.ImmutableArray(Of TestFile) = Immutable.ImmutableArray(Of TestFile).Empty
        Private Property _Files As Bunch.Bunch(Of TestFile) = Bunch.Bunch(Of TestFile).Empty
        Public ReadOnly Property Files As IEnumerable(Of TestFile)
            Get
                Return _Files.Items
            End Get
        End Property
        Public ReadOnly Property Name As String

        Public Sub New(Optional Name As String = Nothing)
            Me.Name = Name
        End Sub
        Public Function WithFile(Name As String, Text As String) As UnitTests.Unit
            Dim tmp = TestFile.Create(Name, Text)
            Return WithFile(tmp)
        End Function

        Public Function WithFile(File As TestFile) As UnitTests.Unit
            If File Is Nothing Then Return Me
            'If _Files Is Nothing Then _Files = (Of UnitTests.TestFile)
            _Files = _Files.Add(File)
            Return Me
        End Function
        Public Shared Function Make(Optional Name As String = Nothing) As UnitTests.Unit
            Return New Unit(Name)
        End Function
        Public Class TestFile
            Public ReadOnly Property Name As String
            Public ReadOnly Property Source As SourceText
            Private Sub New(Name As String, Source As SourceText)
                Me.Name = Name
                Me.Source = Source
            End Sub

            Public Shared Function Create(Name As String, Source As SourceText) As TestFile
                Return New TestFile(Name, Source)
            End Function
            Public Shared Function Create(Name As String, Text As String) As TestFile
                Return New TestFile(Name, SourceText.From(Text))
            End Function
            Public Class SourceText
                Public ReadOnly Property Text As String
                Private Sub New(Text As String)
                    Me.Text = If(Text, String.Empty)
                End Sub
                Public Shared Function [From](Text As String) As SourceText
                    Return New SourceText(Text)
                End Function
            End Class
        End Class

    End Class
    Namespace Bunch
        Public MustInherit Class Bunch(Of T)
            Public ReadOnly Property Count As Int32
            Friend Sub New(Count As Int32)
                Me.Count = Count
            End Sub
            MustOverride Function Items() As IEnumerable(Of T)
            Shared __Empty As New Zero(Of T)
            Public Shared ReadOnly Property Empty As Zero(Of T) = __Empty
            MustOverride Function Add(Item As T) As Bunch(Of T)
        End Class
        Public NotInheritable Class Zero(Of T) : Inherits Bunch(Of T)
            Friend Sub New()
                MyBase.New(0)
            End Sub
            Public Overrides Function Items() As IEnumerable(Of T)
                Return SpecializedCollections.EmptyEnumerable(Of T)
            End Function
            Public Overrides Function Add(Item As T) As Bunch(Of T)
                Return If(Item Is Nothing, DirectCast(Me, Bunch(Of T)), New One(Of T)(Item))
            End Function
        End Class
        Public NotInheritable Class One(Of T) : Inherits Bunch(Of T)
            Private _i0 As T
            Friend Sub New(i0 As T)
                MyBase.New(1)
                _i0 = i0
            End Sub
            Public Overrides Iterator Function Items() As IEnumerable(Of T)
                Yield _i0
            End Function
            Public Overrides Function Add(Item As T) As Bunch(Of T)
                Return If(Item Is Nothing, DirectCast(Me, Bunch(Of T)), New Two(Of T)(_i0, Item))
            End Function
        End Class
        Public NotInheritable Class Two(Of T) : Inherits Bunch(Of T)
            Private _i0 As T, _i1 As T
            Friend Sub New(i0 As T, i1 As T)
                MyBase.New(2)
                _i0 = i0 : _i1 = i1
            End Sub
            Public Overrides Iterator Function Items() As IEnumerable(Of T)
                Yield _i0 : Yield _i1
            End Function
            Public Overrides Function Add(Item As T) As Bunch(Of T)
                Return If(Item Is Nothing, DirectCast(Me, Bunch(Of T)), New Three(Of T)(_i0, _i1, Item))
            End Function
        End Class
        Public NotInheritable Class Three(Of T) : Inherits Bunch(Of T)
            Private _i0 As T, _i1 As T, _i2 As T
            Friend Sub New(i0 As T, i1 As T, i2 As T)
                MyBase.New(3)
                _i0 = i0 : _i1 = i1 : _i2 = i2
            End Sub
            Public Overrides Iterator Function Items() As IEnumerable(Of T)
                Yield _i0
                Yield _i1
                Yield _i2
            End Function
            Public Overrides Function Add(Item As T) As Bunch(Of T)
                Return If(Item Is Nothing, DirectCast(Me, Bunch(Of T)), New Many(Of T)(Count + 1, Items.Concat(Enumerable.Repeat(Item, 1))))
            End Function
        End Class
        Public NotInheritable Class Many(Of T) : Inherits Bunch(Of T)
            Private _ix As IEnumerable(Of T)
            Friend Sub New(Count As Int32, ix As IEnumerable(Of T))
                MyBase.New(Count)
                _ix = ix
            End Sub
            Public Overrides Function Items() As IEnumerable(Of T)
                Return _ix.AsEnumerable
            End Function
            Public Overrides Function Add(Item As T) As Bunch(Of T)
                Return If(Item Is Nothing, Me, New Many(Of T)(Count + 1, Items.Concat(Enumerable.Repeat(Item, 1))))
            End Function
        End Class
    End Namespace
End Namespace