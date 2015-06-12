' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class DeterministicTests
        Inherits BasicTestBase

        Private Function GetBytesEmitted(source As String, platform As Platform, debug As Boolean) As ImmutableArray(Of Byte)
            Dim options = If(debug, TestOptions.DebugExe, TestOptions.ReleaseExe).WithPlatform(platform)

            Dim compilation = CreateCompilationWithMscorlib({source}, assemblyName:="DeterminismTest", options:=options,
                                                            parseOptions:=TestOptions.Regular.WithFeature("dEtErmInIstIc", "true")) ' expect case-insensitivity

            ' The resolution of the PE header time date stamp is seconds, and we want to make sure
            ' that has an opportunity to change between calls to Emit.
            Thread.Sleep(TimeSpan.FromSeconds(1))

            Return compilation.EmitToArray()
        End Function

        <Fact>
        Public Sub CompareAllBytesEmitted_Release()
            Dim source =
"Class Program
    Shared Sub Main()
    End Sub
End Class"
            Dim result1 = GetBytesEmitted(source, platform:=Platform.AnyCpu32BitPreferred, debug:=False)
            Dim result2 = GetBytesEmitted(source, platform:=Platform.AnyCpu32BitPreferred, debug:=False)
            AssertEx.Equal(result1, result2)

            Dim result3 = GetBytesEmitted(source, platform:=Platform.X64, debug:=False)
            Dim result4 = GetBytesEmitted(source, platform:=Platform.X64, debug:=False)
            AssertEx.Equal(result3, result4)
        End Sub

        <Fact, WorkItem(926)>
        Public Sub CompareAllBytesEmitted_Debug()
            Dim source =
"Class Program
    Shared Sub Main()
    End Sub
End Class"
            Dim result1 = GetBytesEmitted(source, platform:=Platform.AnyCpu32BitPreferred, debug:=True)
            Dim result2 = GetBytesEmitted(source, platform:=Platform.AnyCpu32BitPreferred, debug:=True)
            AssertEx.Equal(result1, result2)

            Dim result3 = GetBytesEmitted(source, platform:=Platform.X64, debug:=True)
            Dim result4 = GetBytesEmitted(source, platform:=Platform.X64, debug:=True)
            AssertEx.Equal(result3, result4)
        End Sub
    End Class
End Namespace
