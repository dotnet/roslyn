' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports System.Text

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
        <WorkItem(11990, "https://github.com/dotnet/roslyn/issues/11990")>
        Public Sub ForwardedTypesAreEmmittedInADeterministicOrder()
            Const generatedTypes = 100

            ' VBC doesn't recognize type forwards in VB source code. Because of that,
            ' this test generates types as a C# library (1), then generates a C# netmodule (2) that
            ' contains the type forwards that forwards to reference (1), then generates an empty VB
            ' library (3) that contains (2) and references (1).

            Dim forwardedToCode = New StringBuilder()
            forwardedToCode.AppendLine("namespace ForwardedToNamespace {")
            For i = 1 To generatedTypes
                forwardedToCode.AppendLine($"public class T{i} {{}}")
            Next
            forwardedToCode.AppendLine("}")

            Dim forwardedToCompilation = CreateCSharpCompilation(
                forwardedToCode.ToString(),
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim compileAndGetExportedTypes As Func(Of List(Of String)) =
            Function()
                Dim forwardingCode = New StringBuilder()
                forwardingCode.AppendLine("using System.Runtime.CompilerServices;")
                For i = 1 To generatedTypes
                    forwardingCode.AppendLine($"[assembly: TypeForwardedTo(typeof(ForwardedToNamespace.T{i}))]")
                Next

                Dim forwardingNetModule = CreateCSharpCompilation(
                    "ForwardingAssembly",
                    forwardingCode.ToString(),
                    compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.NetModule),
                    referencedAssemblies:={MscorlibRef, SystemRef, forwardedToCompilation.EmitToImageReference()})

                Dim forwardingCompilation = CreateCompilationWithMscorlib(
                    assemblyName:="ForwardingAssembly",
                    source:=String.Empty,
                    options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    references:={forwardingNetModule.EmitToImageReference(), forwardedToCompilation.EmitToImageReference()})

                Using stream = forwardingCompilation.EmitToStream()
                    Using block = ModuleMetadata.CreateFromStream(stream)
                        Dim typeNames = New List(Of String)(block.MetadataReader.ExportedTypes.Count)
                        For Each handle In block.MetadataReader.ExportedTypes
                            Dim Type = block.MetadataReader.GetExportedType(handle)
                            Dim TypeName = block.MetadataReader.GetString(Type.Name)
                            typeNames.Add(TypeName)
                        Next
                        Return typeNames
                    End Using
                End Using
            End Function

            Dim baseline = compileAndGetExportedTypes().ToArray()
            Assert.Equal(generatedTypes, baseline.Length)

            Dim reference = compileAndGetExportedTypes().ToArray()
            Assert.Equal(baseline, reference)
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
