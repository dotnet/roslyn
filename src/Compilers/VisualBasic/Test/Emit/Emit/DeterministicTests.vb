Imports Microsoft.CodeAnalysis.Test.Utilities
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class DeterministicTests : Inherits BasicTestBase

        Private Function GetBytesEmitted(source As String, platform As Platform, debug As Boolean, deterministic As Boolean) As ImmutableArray(Of Byte)
            Dim options = If(debug, TestOptions.DebugExe, TestOptions.ReleaseExe).WithPlatform(platform)
            If deterministic Then
                options = options.WithFeatures({"dEtErmInIstIc"}.AsImmutable()) ' expect case-insensitivity
            End If

            Dim compilation = CreateCompilationWithMscorlib({source}, assemblyName:="DeterminismTest", compOptions:=options)

            ' The resolution of the PE header time date stamp Is seconds, And we want to make sure that has an opportunity to change
            ' between calls to Emit.
            Thread.Sleep(TimeSpan.FromSeconds(1))

            Return compilation.EmitToArray()
        End Function

        Private Class ImmutableByteArrayEqualityComparer : Implements IEqualityComparer(Of ImmutableArray(Of Byte))
            Public Overloads Function Equals(x As ImmutableArray(Of Byte), y As ImmutableArray(Of Byte)) As Boolean Implements IEqualityComparer(Of ImmutableArray(Of Byte)).Equals
                Return x.SequenceEqual(y)
            End Function

            Public Overloads Function GetHashCode(obj As ImmutableArray(Of Byte)) As Integer Implements IEqualityComparer(Of ImmutableArray(Of Byte)).GetHashCode
                Return obj.GetHashCode()
            End Function
        End Class

        <Fact>
        Public Sub CompareAllBytesEmitted()
            Dim source =
"Class Program
    Shared Sub Main()
    End Sub
End Class"
            Dim comparer = New ImmutableByteArrayEqualityComparer()

            Dim result1 = GetBytesEmitted(source, platform:=Platform.AnyCpu32BitPreferred, debug:=True, deterministic:=True)
            Dim result2 = GetBytesEmitted(source, platform:=Platform.AnyCpu32BitPreferred, debug:=True, deterministic:=True)
            Assert.Equal(result1, result2, comparer)

            Dim result3 = GetBytesEmitted(source, platform:=Platform.X64, debug:=False, deterministic:=True)
            Dim result4 = GetBytesEmitted(source, platform:=Platform.X64, debug:=False, deterministic:=True)
            Assert.Equal(result3, result4, comparer)
            Assert.NotEqual(result1, result3, comparer)

            Dim result5 = GetBytesEmitted(source, platform:=Platform.X64, debug:=False, deterministic:=False)
            Dim result6 = GetBytesEmitted(source, platform:=Platform.X64, debug:=False, deterministic:=False)
            Assert.NotEqual(result5, result6, comparer)
            Assert.NotEqual(result3, result5, comparer)
        End Sub

    End Class

End Namespace
