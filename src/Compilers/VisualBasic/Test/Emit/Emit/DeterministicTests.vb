' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class DeterministicTests
        Inherits BasicTestBase

        Private Function GetBytesEmitted(source As String, platform As Platform, debug As Boolean) As ImmutableArray(Of Byte)
            Dim options = If(debug, TestOptions.DebugExe, TestOptions.ReleaseExe).WithPlatform(platform).WithDeterministic(True)

            Dim compilation = CreateCompilationWithMscorlib({source}, assemblyName:="DeterminismTest", options:=options)

            ' The resolution of the PE header time date stamp is seconds, and we want to make sure
            ' that has an opportunity to change between calls to Emit.
            Thread.Sleep(TimeSpan.FromSeconds(1))

            Return compilation.EmitToArray()
        End Function

        <Fact>
        Public Sub BanVersionWildcards()
            Dim source =
"<assembly: System.Reflection.AssemblyVersion(""10101.0.*"")> 
Class C 
    Shared Sub Main()
    End Sub
End Class"
            Dim compilationDeterministic = CreateCompilationWithMscorlib({source},
                                                                         assemblyName:="DeterminismTest",
                                                                         options:=TestOptions.DebugExe.WithDeterministic(True))
            Dim compilationNonDeterministic = CreateCompilationWithMscorlib({source},
                                                                         assemblyName:="DeterminismTest",
                                                                         options:=TestOptions.DebugExe.WithDeterministic(False))

            Dim resultDeterministic = compilationDeterministic.Emit(Stream.Null, Stream.Null)
            Dim resultNonDeterministic = compilationNonDeterministic.Emit(Stream.Null, Stream.Null)

            Assert.False(resultDeterministic.Success)
            Assert.True(resultNonDeterministic.Success)
        End Sub

        <Fact>
        <WorkItem(5813, "https://github.com/dotnet/roslyn/issues/5813")>
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

        <Fact,
         WorkItem(5813, "https://github.com/dotnet/roslyn/issues/5813"),
         WorkItem(926, "https://github.com/dotnet/roslyn/issues/926")>
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

        <Fact>
        Public Sub TestStaticFieldInitializersPartialClassDeterminism()
            ' When we have initializers in different parts of a partial class,
            ' they must be initialized in a deterministic order.
            For i As Integer = 1 To 2
                ' We run multiple times to increase the chance of observing any nondeterminism
                CompileAndVerify(
    <compilation>
        <file name="x1.vb">
Partial Class [Partial]
    Public Shared a As Integer = D.Init(1, "Partial.a")
End Class
    </file>
        <file name="x2.vb">
Partial Class [Partial]
    Public Shared c As Integer, b As Integer = D.Init(2, "Partial.b")

    Shared Sub New()
        c = D.Init(3, "Partial.c")
    End Sub
End Class
    </file>
        <file name="x3.vb">
Class D
    Public Shared Sub Main()
        System.Console.WriteLine("Partial.a = {0}", [Partial].a)
        System.Console.WriteLine("Partial.b = {0}", [Partial].b)
        System.Console.WriteLine("Partial.c = {0}", [Partial].c)
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class
    </file>
    </compilation>,
    expectedOutput:=<![CDATA[
Partial.a
Partial.b
Partial.c
Partial.a = 1
Partial.b = 2
Partial.c = 3
]]>)

            Next
        End Sub

        <Fact>
        Public Sub TestInterfacesPartialClassDeterminism()
            ' When we have parts of a class in different trees,
            ' their bases must be emitted in a deterministic order.
            For i As Integer = 1 To 2
                ' We run multiple times to increase the chance of observing any nondeterminism
                CompileAndVerify(
    <compilation>
        <file name="x1.vb">
Partial Class [Partial]
    Implements I1
End Class
    </file>
        <file name="x2.vb">
Partial Class [Partial]
    Implements I2
End Class
    </file>
        <file name="x3.vb">
Class D
    Public Shared Sub Main()
        For Each i In GetType([Partial]).GetInterfaces()
            System.Console.WriteLine(i.Name)
        Next
    End Sub
End Class
Interface I1
End Interface
Interface I2
End Interface
    </file>
    </compilation>,
    expectedOutput:=<![CDATA[
I1
I2
]]>)

            Next
        End Sub
    End Class
End Namespace
