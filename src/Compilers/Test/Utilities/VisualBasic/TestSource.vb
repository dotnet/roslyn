Namespace Global.Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class Unit
        Private Property _Files As Immutable.ImmutableArray(Of TestFile) = Immutable.ImmutableArray(Of TestFile).Empty
        Public ReadOnly Property Files As IList(Of TestFile)
            Get
                Return _Files
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



End Namespace