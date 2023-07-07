' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests
    Public Class CommandLineIVTTests
        Inherits BasicTestBase

        <Fact>
        Public Sub InaccessibleBaseType()
            Dim source1 = "
Namespace N1
    Friend Class A
    End Class
End Namespace"

            Dim comp1 = CreateCompilation(source1, assemblyName:="N1", targetFramework:=TargetFramework.Mscorlib461)

            Dim dir = Temp.CreateDirectory()
            Dim source2 = dir.CreateFile("B.vb").WriteAllText("
Class B
    Inherits N1.A
End Class")

            Dim sw = New StringWriter()

            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                dir.Path,
                {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/reportivts",
                source2.Path
                }, additionalReferences:={comp1.ToMetadataReference()})

            Dim errorCode = compiler.Run(sw)

            Assert.Equal(1, errorCode)

            AssertEx.AssertEqualToleratingWhitespaceDifferences($"
{source2.Path}(3) : error BC30389: 'N1.A' is not accessible in this context because it is 'Friend'.
    Inherits N1.A
             ~~~~
{source2.Path}(3) : error BC37327: 'A' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    Inherits N1.A
             ~~~~

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'System, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'System.Net.Http'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293
    Assembly name: 'System.Net.Http.WebRequest'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293", sw.ToString())
        End Sub

        <Fact>
        Public Sub InaccessibleMember()
            Dim source1 = "
Namespace N1
    Public Class A
        Friend Sub M()
        End Sub
    End Class
End Namespace"

            Dim comp1 = CreateCompilation(source1, assemblyName:="N1", targetFramework:=TargetFramework.Mscorlib461)

            Dim dir = Temp.CreateDirectory()
            Dim source2 = dir.CreateFile("B.vb").WriteAllText("
Class B
    Inherits N1.A
    Public Sub S()
        MyBase.M()
    End Sub
End Class")

            Dim sw = New StringWriter()

            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                dir.Path,
                {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/reportivts",
                source2.Path
                }, additionalReferences:={comp1.ToMetadataReference()})

            Dim errorCode = compiler.Run(sw)

            Assert.Equal(1, errorCode)

            AssertEx.AssertEqualToleratingWhitespaceDifferences($"
{source2.Path}(5) : error BC30390: 'A.Friend Sub M()' is not accessible in this context because it is 'Friend'.
        MyBase.M()
        ~~~~~~~~  
{source2.Path}(5) : error BC37327: 'Friend Sub M()' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
        MyBase.M()
        ~~~~~~~~  

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'System, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'System.Net.Http'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293
    Assembly name: 'System.Net.Http.WebRequest'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293", sw.ToString())
        End Sub

        <Fact>
        Public Sub InaccessibleMemberOverride()
            Dim source1 = "
Namespace N1
    Public Class A
        Friend Overridable Sub M()
        End Sub
    End Class
End Namespace"

            Dim comp1 = CreateCompilation(source1, assemblyName:="N1", targetFramework:=TargetFramework.Mscorlib461)

            Dim dir = Temp.CreateDirectory()
            Dim source2 = dir.CreateFile("B.vb").WriteAllText("
Class B
    Inherits N1.A

    Friend Overrides Sub M()
    End Sub
End Class")

            Dim sw = New StringWriter()

            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                dir.Path,
                {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/reportivts",
                source2.Path
                }, additionalReferences:={comp1.ToMetadataReference()})

            Dim errorCode = compiler.Run(sw)

            Assert.Equal(1, errorCode)

            AssertEx.AssertEqualToleratingWhitespaceDifferences($"
{source2.Path}(5) : error BC31417: 'Friend Overrides Sub M()' cannot override 'Friend Overridable Sub M()' because it is not accessible in this context.
    Friend Overrides Sub M()
                         ~  
{source2.Path}(5) : error BC37327: 'Friend Overridable Sub M()' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    Friend Overrides Sub M()
                         ~ 

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'System, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'System.Net.Http'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293
    Assembly name: 'System.Net.Http.WebRequest'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293", sw.ToString())
        End Sub

        <Fact>
        Public Sub InaccessibleCoClass()
            Dim source1 = "
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib(""NoPIANew1-PIA2.dll"")>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
Public Class Class1
    <Guid(""bd60d4b3-f50b-478b-8ef2-e777df99d810"")> _
    <ComImport()> _
    <InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
    <CoClass(GetType(FooImpl))> _
    Public Interface IFoo
    End Interface
    <Guid(""c9dcf748-b634-4504-a7ce-348cf7c61891"")> _
    Friend Class FooImpl
    End Class
End Class
"

            Dim comp1 = CreateCompilation(source1, assemblyName:="N1", targetFramework:=TargetFramework.Mscorlib461)

            Dim dir = Temp.CreateDirectory()
            Dim source2 = dir.CreateFile("B.vb").WriteAllText("
Public Module Module1
    Public Sub Main()
        Dim i1 As New Class1.IFoo(1)
        Dim i2 = New Class1.IFoo(Nothing)
    End Sub
End Module
")

            Dim sw = New StringWriter()

            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                dir.Path,
                {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/reportivts",
                source2.Path
                }, additionalReferences:={comp1.ToMetadataReference()})

            Dim errorCode = compiler.Run(sw)

            Assert.Equal(1, errorCode)

            AssertEx.AssertEqualToleratingWhitespaceDifferences($"
{source2.Path}(4) : error BC31109: Implementing class 'Class1.FooImpl' for interface 'Class1.IFoo' is not accessible in this context because it is 'Friend'.
        Dim i1 As New Class1.IFoo(1)
                  ~~~~~~~~~~~~~~~~~~
{source2.Path}(5) : error BC31109: Implementing class 'Class1.FooImpl' for interface 'Class1.IFoo' is not accessible in this context because it is 'Friend'.
        Dim i2 = New Class1.IFoo(Nothing)
                 ~~~~~~~~~~~~~~~~~~~~~~~~
{source2.Path}(4) : error BC37327: 'Class1.FooImpl' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
        Dim i1 As New Class1.IFoo(1)
                  ~~~~~~~~~~~~~~~~~~
{source2.Path}(5) : error BC37327: 'Class1.FooImpl' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
        Dim i2 = New Class1.IFoo(Nothing)
                 ~~~~~~~~~~~~~~~~~~~~~~~~

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'System, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'System.Net.Http'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293
    Assembly name: 'System.Net.Http.WebRequest'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293", sw.ToString())
        End Sub

        <Fact>
        Public Sub InaccessibleReturnTypeOfMember()
            Dim source1 = "
<assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""N2"")>
Namespace N1
    Friend Class A
    End Class
End Namespace"

            Dim comp1 = CreateCompilation(source1, assemblyName:="N1", targetFramework:=TargetFramework.Mscorlib461)

            Dim source2 = "
<assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")>
Namespace N2
    Friend Class B
        Friend Function M() As N1.A
            Return Nothing
        End Function
    End Class
End Namespace"

            Dim comp2 = CreateCompilation(source2, assemblyName:="N2", references:={comp1.ToMetadataReference()}, targetFramework:=TargetFramework.Mscorlib461)

            Dim dir = Temp.CreateDirectory()
            Dim source3 = dir.CreateFile("C.vb").WriteAllText("
Class C
    Sub S()
        Dim b as New N2.B()
        b.M()
    End Sub
End Class")

            Dim sw = New StringWriter()

            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                dir.Path,
                {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/reportivts",
                source3.Path
                }, additionalReferences:={comp1.ToMetadataReference(), comp2.ToMetadataReference()})

            Dim errorCode = compiler.Run(sw)

            Assert.Equal(1, errorCode)

            AssertEx.AssertEqualToleratingWhitespaceDifferences($"
{source3.Path}(5) : error BC36666: 'Friend Function N2.B.M() As N1.A' is not accessible in this context because the return type is not accessible.
        b.M()
        ~~~~~
{source3.Path}(5) : error BC37327: 'Friend Function M() As A' is defined in assembly 'N2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
        b.M()
        ~~~~~

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'N2'
    Public Keys:

Assembly reference: 'N2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: True
  Grants IVTs to:
    Assembly name: 'C'
    Public Keys:

Assembly reference: 'System, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'System.Net.Http'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293
    Assembly name: 'System.Net.Http.WebRequest'
    Public Keys:
      002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293", sw.ToString())
        End Sub

        <Theory>
        <InlineData("/reportivts")>
        <InlineData("/reportivts+")>
        Public Sub TurnOnLast(onFlag As String)
            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                DirectCast(Nothing, String),
                {
                "/nologo",
                "/reportivts-",
                onFlag
                })

            Assert.True(compiler.Arguments.ReportInternalsVisibleToAttributes)
        End Sub

        <Theory>
        <InlineData("/reportivts")>
        <InlineData("/reportivts+")>
        Public Sub TurnOffLast(onFlag As String)
            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                DirectCast(Nothing, String),
                {
                "/nologo",
                onFlag,
                "/reportivts-"
                })

            Assert.False(compiler.Arguments.ReportInternalsVisibleToAttributes)
        End Sub

        <Fact>
        Public Sub BadReportIvtsValue()
            Dim compiler = New MockVisualBasicCompiler(
                Nothing,
                DirectCast(Nothing, String),
                {
                "/nologo",
                "/reportivts:bad"
                })

            Assert.False(compiler.Arguments.ReportInternalsVisibleToAttributes)
            compiler.Arguments.Errors.Verify(
                Diagnostic(ERRID.ERR_SwitchNeedsBool).WithArguments("reportivts").WithLocation(1, 1),
                Diagnostic(ERRID.ERR_NoSources).WithLocation(1, 1))
        End Sub
    End Class
End Namespace
