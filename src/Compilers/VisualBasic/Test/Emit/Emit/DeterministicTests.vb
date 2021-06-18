' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class DeterministicTests
        Inherits BasicTestBase

        Private Function GetBytesEmitted(source As String, platform As Platform, debug As Boolean) As ImmutableArray(Of Byte)
            Dim options = If(debug, TestOptions.DebugExe, TestOptions.ReleaseExe).WithPlatform(platform).WithDeterministic(True)

            Dim compilation = CreateCompilationWithMscorlib40({source}, assemblyName:="DeterminismTest", options:=options)

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
            Dim compilationDeterministic = CreateCompilationWithMscorlib40({source},
                                                                         assemblyName:="DeterminismTest",
                                                                         options:=TestOptions.DebugExe.WithDeterministic(True))
            Dim compilationNonDeterministic = CreateCompilationWithMscorlib40({source},
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

            Dim result5 = GetBytesEmitted(source, platform:=Platform.Arm64, debug:=False)
            Dim result6 = GetBytesEmitted(source, platform:=Platform.Arm64, debug:=False)
            AssertEx.Equal(result5, result6)
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
        Public Sub ForwardedTypesAreEmittedInADeterministicOrder()
            ' VBC doesn't recognize type forwards in VB source code. Because of that,
            ' this test generates types as a C# library (1), then generates a C# netmodule (2) that
            ' contains the type forwards that forwards to reference (1), then generates an empty VB
            ' library (3) that contains (2) and references (1).

            Dim forwardedToCode = "
namespace Namespace2 {
    public class GenericType1<T> {}
    public class GenericType3<T> {}
    public class GenericType2<T> {}
}
namespace Namespace1 {
    public class Type3 {}
    public class Type2 {}
    public class Type1 {}
}
namespace Namespace4 {
    namespace Embedded {
        public class Type2 {}
        public class Type1 {}
    }
}
namespace Namespace3 {
    public class GenericType {}
    public class GenericType<T> {}
    public class GenericType<T, U> {}
}
"
            Dim forwardedToCompilation1 = CreateCSharpCompilation(
                forwardedToCode, assemblyName:="ForwardedTo",
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim forwardedToReference1 = forwardedToCompilation1.EmitToImageReference()

            Dim forwardingCode = "
using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(Namespace2.GenericType1<int>))]
[assembly: TypeForwardedTo(typeof(Namespace2.GenericType3<int>))]
[assembly: TypeForwardedTo(typeof(Namespace2.GenericType2<int>))]
[assembly: TypeForwardedTo(typeof(Namespace1.Type3))]
[assembly: TypeForwardedTo(typeof(Namespace1.Type2))]
[assembly: TypeForwardedTo(typeof(Namespace1.Type1))]
[assembly: TypeForwardedTo(typeof(Namespace4.Embedded.Type2))]
[assembly: TypeForwardedTo(typeof(Namespace4.Embedded.Type1))]
[assembly: TypeForwardedTo(typeof(Namespace3.GenericType))]
[assembly: TypeForwardedTo(typeof(Namespace3.GenericType<int>))]
[assembly: TypeForwardedTo(typeof(Namespace3.GenericType<int, int>))]
"
            Dim forwardingNetModule = CreateCSharpCompilation(
                    "ForwardingAssembly",
                    forwardingCode,
                    compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.NetModule),
                    referencedAssemblies:={MscorlibRef, SystemRef, forwardedToReference1})

            Dim forwardingNetModuleReference = forwardingNetModule.EmitToImageReference()

            Dim forwardingCompilation = CreateCompilationWithMscorlib40(
                    assemblyName:="ForwardingAssembly",
                    source:=String.Empty,
                    options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    references:={forwardingNetModuleReference, forwardedToReference1})
            Dim forwardingReference = new VisualBasicCompilationReference(forwardingCompilation)

            Dim sortedFullNames =
            {
                "Namespace1.Type1",
                "Namespace1.Type2",
                "Namespace1.Type3",
                "Namespace2.GenericType1`1",
                "Namespace2.GenericType2`1",
                "Namespace2.GenericType3`1",
                "Namespace3.GenericType",
                "Namespace3.GenericType`1",
                "Namespace3.GenericType`2",
                "Namespace4.Embedded.Type1",
                "Namespace4.Embedded.Type2"
            }

            Dim metadataValidator As Action(Of ModuleSymbol) = Sub(m)
                                                                   Dim assembly = m.ContainingAssembly
                                                                   Assert.Equal(sortedFullNames, GetNamesOfForwardedTypes(assembly))
                                                               End Sub

            CompileAndVerify(forwardingCompilation, symbolValidator:=metadataValidator, sourceSymbolValidator:=metadataValidator, verify:=Verification.Skipped)

            Using stream = forwardingCompilation.EmitToStream()
                Using block = ModuleMetadata.CreateFromStream(stream)
                    Dim metadataFullNames = MetadataValidation.GetExportedTypesFullNames(block.MetadataReader)
                    Assert.Equal(sortedFullNames, metadataFullNames)
                End Using
            End Using

            Dim forwardedToCompilation2 = CreateCSharpCompilation(
                forwardedToCode, assemblyName:="ForwardedTo",
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim forwardedToReference2 = forwardedToCompilation2.EmitToImageReference()

            Dim withRetargeting = CreateCompilationWithMscorlib40(
                    source:=String.Empty,
                    options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    references:={forwardedToReference2, forwardingReference})

            Dim retargeting = DirectCast(withRetargeting.GetReferencedAssemblySymbol(forwardingReference), Retargeting.RetargetingAssemblySymbol)
            Dim forwardedToAssembly2 = withRetargeting.GetReferencedAssemblySymbol(forwardedToReference2)
            Assert.Equal(sortedFullNames, GetNamesOfForwardedTypes(retargeting))

            For Each t In GetForwardedTypes(retargeting)
                Assert.Same(forwardedToAssembly2, t.ContainingAssembly)
            Next
        End Sub

        Private Shared Function GetNamesOfForwardedTypes(assembly As AssemblySymbol) As IEnumerable(Of String)
            Return GetForwardedTypes(assembly).Select(Function(t) t.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))
        End Function

        Private Shared Function GetForwardedTypes(assembly As AssemblySymbol) As IEnumerable(Of INamedTypeSymbol)
            Return DirectCast(assembly, IAssemblySymbol).GetForwardedTypes()
        End Function

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
