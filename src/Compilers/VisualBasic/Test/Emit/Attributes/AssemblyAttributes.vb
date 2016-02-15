' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

Public Class AssemblyAttributeTests
    Inherits BasicTestBase

    <Fact>
    Public Sub VersionAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.2.3.4")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal(New Version(1, 2, 3, 4), other.Assembly.Identity.Version)
    End Sub

    <Fact, WorkItem(543708, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543708")>
    Public Sub VersionAttribute02()
        Dim comp As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.22.333.4444")>
Public Class C
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)

        VerifyAssemblyTable(comp, Sub(r)
                                      Assert.Equal(New Version(1, 22, 333, 4444), r.Version)
                                  End Sub)

        ' ---------------------------------------------
        comp = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("10101.0.*")>
Public Class C
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        VerifyAssemblyTable(comp, Sub(r)
                                      Assert.Equal(10101, r.Version.Major)
                                      Assert.Equal(0, r.Version.Minor)
                                  End Sub)

    End Sub

    <Fact, WorkItem(545948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545948")>
    Public Sub VersionAttributeErr()
        Dim comp As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.*")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        CompilationUtils.AssertTheseDiagnostics(comp,
<error><![CDATA[
BC36962: The specified version string does not conform to the required format - major[.minor[.build|*[.revision|*]]]
<Assembly: System.Reflection.AssemblyVersion("1.*")>
                                             ~~~~~
]]></error>)

        ' ---------------------------------------------
        comp = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("-1")>
Public Class C
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        CompilationUtils.AssertTheseDiagnostics(comp,
<error><![CDATA[
BC36962: The specified version string does not conform to the required format - major[.minor[.build|*[.revision|*]]]
<Assembly: System.Reflection.AssemblyVersion("-1")>
                                             ~~~~
]]></error>)

    End Sub

    <Fact>
    Public Sub FileVersionAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyFileVersion("1.2.3.4")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("1.2.3.4", DirectCast(other.Assembly, SourceAssemblySymbol).FileVersion)
    End Sub

    <Fact, WorkItem(545948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545948")>
    Public Sub SatelliteContractVersionAttributeErr()
        Dim comp As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[<Assembly: System.Resources.SatelliteContractVersionAttribute("1.2.3.A")>]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)

        CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC36976: The specified version string does not conform to the recommended format - major.minor.build.revision
<Assembly: System.Resources.SatelliteContractVersionAttribute("1.2.3.A")>
                                                              ~~~~~~~~~
]]></expected>)

        comp = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[<Assembly: System.Resources.SatelliteContractVersionAttribute("1.2.*")>]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)

        CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC36976: The specified version string does not conform to the recommended format - major.minor.build.revision
<Assembly: System.Resources.SatelliteContractVersionAttribute("1.2.*")>
                                                              ~~~~~~~
]]></expected>)
    End Sub

    <Fact>
    Public Sub FileVersionAttributeWrn()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyFileVersion("1.2.*")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        CompilationUtils.AssertTheseDiagnostics(other,
<error><![CDATA[
BC42366: The specified version string does not conform to the recommended format - major.minor.build.revision
<Assembly: System.Reflection.AssemblyFileVersion("1.2.*")>
                                                 ~~~~~~~
]]></error>)
    End Sub

    <Fact>
    Public Sub TitleAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyTitle("One Hundred Years Of Solitude")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("One Hundred Years Of Solitude", DirectCast(other.Assembly, SourceAssemblySymbol).Title)
        Assert.Equal(False, DirectCast(other.Assembly, SourceAssemblySymbol).MightContainNoPiaLocalTypes())
    End Sub

    <Fact>
    Public Sub TitleAttributeNothing()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyTitle(Nothing)>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Null(DirectCast(other.Assembly, SourceAssemblySymbol).Title)
    End Sub

    <Fact>
    Public Sub DescriptionAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDescription("A classic of magical realist literature")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("A classic of magical realist literature", DirectCast(other.Assembly, SourceAssemblySymbol).Description)
    End Sub

    <Fact>
    Public Sub CultureAttribute()

        Dim src = <compilation>
                      <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCulture("pt-BR")>
Public Class C
    Shared Sub Main()
    End Sub
End Class
]]>
                      </file>
                  </compilation>

        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(src, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("pt-BR", other.Assembly.Identity.CultureName)

    End Sub

    <Fact>
    Public Sub CultureAttribute02()
        Dim comp As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCulture("")>
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)

        VerifyAssemblyTable(comp, Sub(r) Assert.True(r.Culture.IsNil))

        comp = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCulture(Nothing)>
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)

        VerifyAssemblyTable(comp, Sub(r) Assert.True(r.Culture.IsNil))

        comp = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCulture("ja-JP")>
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)

        VerifyAssemblyTable(comp, Nothing, strData:="ja-JP")
    End Sub

    <Fact>
    Public Sub CultureAttribute03()
        Dim comp As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCulture("")>
Public Class C
    Shared Sub Main()
    End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.ConsoleApplication)

        VerifyAssemblyTable(comp, Sub(r) Assert.True(r.Culture.IsNil))

        comp = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCulture(Nothing)>
Public Class C
    Shared Sub Main()
    End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.ConsoleApplication)

        VerifyAssemblyTable(comp, Sub(r) Assert.True(r.Culture.IsNil))
    End Sub

    <Fact>
    Public Sub CultureAttributeNul()
        Dim comp As VisualBasicCompilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports Microsoft.VisualBasic

<Assembly: System.Reflection.AssemblyCulture(vbNullChar)>
]]>
    </file>
</compilation>, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        CompilationUtils.AssertTheseDiagnostics(comp,
<error><![CDATA[
BC36982: Assembly culture strings may not contain embedded NUL characters.
<Assembly: System.Reflection.AssemblyCulture(vbNullChar)>
                                             ~~~~~~~~~~
]]></error>)
    End Sub

    <Fact, WorkItem(545951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545951")>
    Public Sub CultureAttributeErr()

        Dim src = <compilation>
                      <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCulture("pt-BR")>
Public Class C
    Shared Sub Main()
    End Sub
End Class
]]>
                      </file>
                  </compilation>

        Dim comp = CreateCompilationWithMscorlib(src, OutputKind.ConsoleApplication)
        CompilationUtils.AssertTheseDiagnostics(comp,
<error><![CDATA[
BC36977: Executables cannot be satellite assemblies; culture should always be empty
<Assembly: System.Reflection.AssemblyCulture("pt-BR")>
                                             ~~~~~~~
]]></error>)

    End Sub

    <Fact(Skip:=("https://github.com/dotnet/roslyn/issues/5866"))>
    Public Sub CultureAttributeMismatch()
        Dim neutral As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="neutral">
    <file name="a.vb"><![CDATA[
public class neutral
end class
]]>
    </file>
</compilation>, TestOptions.ReleaseDll)

        Dim neutralRef = New VisualBasicCompilationReference(neutral)

        Dim en_UK As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="en_UK">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCultureAttribute("en-UK")>

public class en_UK
end class
]]>
    </file>
</compilation>, TestOptions.ReleaseDll)

        Dim en_UKRef = New VisualBasicCompilationReference(en_UK)

        Dim en_us As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="en_us">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCultureAttribute("en-us")>

public class en_us
end class
]]>
    </file>
</compilation>, TestOptions.ReleaseDll)

        Dim en_usRef = New VisualBasicCompilationReference(en_us)

        Dim compilation As VisualBasicCompilation

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCultureAttribute("en-US")>

public class en_US
    Sub M(x as en_UK)
    End Sub
end class
]]>
    </file>
</compilation>, {en_UKRef, neutralRef}, TestOptions.ReleaseDll)

        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC42371: Referenced assembly 'en_UK, Version=0.0.0.0, Culture=en-UK, PublicKeyToken=null' has different culture setting of 'en-UK'.
</expected>)

        compilation = compilation.WithOptions(TestOptions.ReleaseModule)
        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
</expected>)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb">
    </file>
</compilation>, {compilation.EmitToImageReference()}, TestOptions.ReleaseDll)

        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC42371: Referenced assembly 'en_UK, Version=0.0.0.0, Culture=en-UK, PublicKeyToken=null' has different culture setting of 'en-UK'.
</expected>)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCultureAttribute("en-US")>

public class Test
    Sub M(x as en_us)
    End Sub
end class
]]>
    </file>
</compilation>, {en_usRef}, TestOptions.ReleaseDll)

        CompileAndVerify(compilation).VerifyDiagnostics()

        compilation = compilation.WithOptions(TestOptions.ReleaseModule)
        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
</expected>)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb">
    </file>
</compilation>, {compilation.EmitToImageReference()}, TestOptions.ReleaseDll)

        CompileAndVerify(compilation).VerifyDiagnostics()

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCultureAttribute("en-US")>

public class en_US
    Sub M(x as neutral)
    End Sub
end class
]]>
    </file>
</compilation>, {en_UKRef, neutralRef}, TestOptions.ReleaseDll)

        CompileAndVerify(compilation).VerifyDiagnostics()

        compilation = compilation.WithOptions(TestOptions.ReleaseModule)
        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
</expected>)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb">
    </file>
</compilation>, {compilation.EmitToImageReference()}, TestOptions.ReleaseDll)

        CompileAndVerify(compilation,
                         sourceSymbolValidator:=Sub(m As ModuleSymbol)
                                                    Assert.Equal(1, m.GetReferencedAssemblySymbols().Length)

                                                    Dim naturalRef = m.ContainingAssembly.Modules(1).GetReferencedAssemblySymbols(1)
                                                    Assert.True(naturalRef.IsMissing)
                                                    Assert.Equal("neutral, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", naturalRef.ToTestDisplayString())
                                                End Sub,
                         symbolValidator:=Sub(m As ModuleSymbol)
                                              Assert.Equal(2, m.GetReferencedAssemblySymbols().Length)
                                              Assert.Equal("neutral, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", m.GetReferencedAssemblySymbols()(1).ToTestDisplayString())
                                          End Sub).VerifyDiagnostics()

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
public class neutral
    Sub M(x as en_UK)
    End Sub
end class
]]>
    </file>
</compilation>, {en_UKRef}, TestOptions.ReleaseDll)

        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC42371: Referenced assembly 'en_UK, Version=0.0.0.0, Culture=en-UK, PublicKeyToken=null' has different culture setting of 'en-UK'.
</expected>)

        compilation = compilation.WithOptions(TestOptions.ReleaseModule)
        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
</expected>)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb">
    </file>
</compilation>, {compilation.EmitToImageReference()}, TestOptions.ReleaseDll)

        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC42371: Referenced assembly 'en_UK, Version=0.0.0.0, Culture=en-UK, PublicKeyToken=null' has different culture setting of 'en-UK'.
</expected>)

    End Sub

    <Fact>
    Public Sub CompanyAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCompany("MossBrain")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("MossBrain", DirectCast(other.Assembly, SourceAssemblySymbol).Company)

        other = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[<Assembly: System.Reflection.AssemblyCompany("微软")>]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("微软", DirectCast(other.Assembly, SourceAssemblySymbol).Company)

    End Sub

    <Fact>
    Public Sub ProductAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyProduct("Sound Cannon")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("Sound Cannon", DirectCast(other.Assembly, SourceAssemblySymbol).Product)
    End Sub

    <Fact>
    Public Sub CopyrightAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyCopyright("مايكروسوفت")>
Public Structure S

End Structure
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("مايكروسوفت", DirectCast(other.Assembly, SourceAssemblySymbol).Copyright)
    End Sub

    <Fact>
    Public Sub TrademarkAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyTrademark("circle r")>
Interface IFoo

End Interface
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("circle r", DirectCast(other.Assembly, SourceAssemblySymbol).Trademark)
    End Sub

    <Fact>
    Public Sub InformationalVersionAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyInformationalVersion("1.2.3garbage")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, OutputKind.DynamicallyLinkedLibrary)
        Assert.Empty(other.GetDiagnostics())
        Assert.Equal("1.2.3garbage", DirectCast(other.Assembly, SourceAssemblySymbol).InformationalVersion)
    End Sub

    <Fact(), WorkItem(529922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529922")>
    Public Sub AlgorithmIdAttribute()

        Dim hash_module = TestReferences.SymbolsTests.netModule.hash_module

        Dim hash_resources = {New ResourceDescription("hash_resource", "snKey.snk",
            Function() New MemoryStream(TestResources.General.snKey, writable:=False),
            True)}

        Dim compilation As VisualBasicCompilation

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={hash_module})

        CompileAndVerify(compilation,
            manifestResources:=hash_resources,
            validator:=Sub(peAssembly)
                           Dim reader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = reader.GetAssemblyDefinition()
                           Assert.Equal(AssemblyHashAlgorithm.Sha1, assembly.HashAlgorithm)

                           Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                           Assert.Equal(New Byte() {&H6C, &H9C, &H3E, &HDA, &H60, &HF, &H81, &H93, &H4A, &HC1, &HD, &H41, &HB3, &HE9, &HB2, &HB7, &H2D, &HEE, &H59, &HA8},
                               reader.GetBlobBytes(file1.HashValue))

                           Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))
                           Assert.Equal(New Byte() {&H7F, &H28, &HEA, &HD1, &HF4, &HA1, &H7C, &HB8, &HC, &H14, &HC0, &H2E, &H8C, &HFF, &H10, &HEC, &HB3, &HC2, &HA5, &H1D},
                               reader.GetBlobBytes(file2.HashValue))

                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.None)>

class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={hash_module})

        CompileAndVerify(compilation,
            manifestResources:=hash_resources,
            validator:=Sub(peAssembly)
                           Dim reader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = reader.GetAssemblyDefinition()
                           Assert.Equal(AssemblyHashAlgorithm.None, assembly.HashAlgorithm)

                           Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                           Assert.Equal(New Byte() {&H6C, &H9C, &H3E, &HDA, &H60, &HF, &H81, &H93, &H4A, &HC1, &HD, &H41, &HB3, &HE9, &HB2, &HB7, &H2D, &HEE, &H59, &HA8},
                               reader.GetBlobBytes(file1.HashValue))

                           Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))
                           Assert.Equal(New Byte() {&H7F, &H28, &HEA, &HD1, &HF4, &HA1, &H7C, &HB8, &HC, &H14, &HC0, &H2E, &H8C, &HFF, &H10, &HEC, &HB3, &HC2, &HA5, &H1D},
                               reader.GetBlobBytes(file2.HashValue))

                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(CUInt(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5))>

class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={hash_module})

        CompileAndVerify(compilation,
            manifestResources:=hash_resources,
            validator:=Sub(peAssembly)
                           Dim reader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = reader.GetAssemblyDefinition()
                           Assert.Equal(AssemblyHashAlgorithm.MD5, assembly.HashAlgorithm)

                           Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                           Assert.Equal(New Byte() {&H24, &H22, &H3, &HC3, &H94, &HD5, &HC2, &HD9, &H99, &HB3, &H6D, &H59, &HB2, &HCA, &H23, &HBC},
                               reader.GetBlobBytes(file1.HashValue))

                           Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))
                           Assert.Equal(New Byte() {&H8D, &HFE, &HBF, &H49, &H8D, &H62, &H2A, &H88, &H89, &HD1, &HE, &H0, &H9E, &H29, &H72, &HF1},
                               reader.GetBlobBytes(file2.HashValue))

                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1)>

class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={hash_module})

        CompileAndVerify(compilation,
            manifestResources:=hash_resources,
            validator:=Sub(peAssembly)
                           Dim reader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = reader.GetAssemblyDefinition()
                           Assert.Equal(AssemblyHashAlgorithm.Sha1, assembly.HashAlgorithm)

                           Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                           Assert.Equal(New Byte() {&H6C, &H9C, &H3E, &HDA, &H60, &HF, &H81, &H93, &H4A, &HC1, &HD, &H41, &HB3, &HE9, &HB2, &HB7, &H2D, &HEE, &H59, &HA8},
                               reader.GetBlobBytes(file1.HashValue))

                           Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))
                           Assert.Equal(New Byte() {&H7F, &H28, &HEA, &HD1, &HF4, &HA1, &H7C, &HB8, &HC, &H14, &HC0, &H2E, &H8C, &HFF, &H10, &HEC, &HB3, &HC2, &HA5, &H1D},
                               reader.GetBlobBytes(file2.HashValue))

                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)

        compilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA256)>

class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={MscorlibRef_v4_0_30316_17626, hash_module})

        CompileAndVerify(compilation, verify:=False,
            manifestResources:=hash_resources,
            validator:=Sub(peAssembly)
                           Dim reader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = reader.GetAssemblyDefinition()
                           Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA256, CType(assembly.HashAlgorithm, System.Configuration.Assemblies.AssemblyHashAlgorithm))

                           Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                           Assert.Equal(New Byte() {&HA2, &H32, &H3F, &HD, &HF4, &HB8, &HED, &H5A, &H1B, &H7B, &HBE, &H14, &H4F, &HEC, &HBF, &H88, &H23, &H61, &HEB, &H40, &HF7, &HF9, &H46, &HEF, &H68, &H3B, &H70, &H29, &HCF, &H12, &H5, &H35},
                               reader.GetBlobBytes(file1.HashValue))

                           Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))
                           Assert.Equal(New Byte() {&HCC, &HAE, &HA0, &HB4, &H9E, &HAE, &H28, &HE0, &HA3, &H46, &HE9, &HCF, &HF3, &HEF, &HEA, &HF7,
                                                     &H1D, &HDE, &H62, &H8F, &HD6, &HF4, &H87, &H76, &H1A, &HC3, &H6F, &HAD, &H10, &H1C, &H10, &HAC},
                               reader.GetBlobBytes(file2.HashValue))

                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)

        compilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA384)>

class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={MscorlibRef_v4_0_30316_17626, hash_module})

        CompileAndVerify(compilation, verify:=False,
            manifestResources:=hash_resources,
            validator:=Sub(peAssembly)
                           Dim reader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = reader.GetAssemblyDefinition()
                           Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA384, CType(assembly.HashAlgorithm, System.Configuration.Assemblies.AssemblyHashAlgorithm))

                           Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                           Assert.Equal(New Byte() {&HB6, &H35, &H9B, &HBE, &H82, &H89, &HFF, &H1, &H22, &H8B, &H56, &H5E, &H9B, &H15, &H5D, &H10,
                                                     &H68, &H83, &HF7, &H75, &H4E, &HA6, &H30, &HF7, &H8D, &H39, &H9A, &HB7, &HE8, &HB6, &H47, &H1F,
                                                     &HF6, &HFD, &H1E, &H64, &H63, &H6B, &HE7, &HF4, &HBE, &HA7, &H21, &HED, &HFC, &H82, &H38, &H95},
                               reader.GetBlobBytes(file1.HashValue))

                           Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))
                           Assert.Equal(New Byte() {&H45, &H5, &H2E, &H90, &H9B, &H61, &HA3, &HF8, &H60, &HD2, &H86, &HCB, &H10, &H33, &HC9, &H86,
                                                     &H68, &HA5, &HEE, &H4A, &HCF, &H21, &H10, &HA9, &H8F, &H14, &H62, &H8D, &H3E, &H7D, &HFD, &H7E,
                                                     &HE6, &H23, &H6F, &H2D, &HBA, &H4, &HE7, &H13, &HE4, &H5E, &H8C, &HEB, &H80, &H68, &HA3, &H17},
                               reader.GetBlobBytes(file2.HashValue))

                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)

        compilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA512)>

class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={MscorlibRef_v4_0_30316_17626, hash_module})

        CompileAndVerify(compilation, verify:=False,
            manifestResources:=hash_resources,
            validator:=Sub(peAssembly)
                           Dim reader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = reader.GetAssemblyDefinition()
                           Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA512, CType(assembly.HashAlgorithm, System.Configuration.Assemblies.AssemblyHashAlgorithm))

                           Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                           Assert.Equal(New Byte() {&H5F, &H4D, &H7E, &H63, &HC9, &H87, &HD9, &HEB, &H4F, &H5C, &HFD, &H96, &H3F, &H25, &H58, &H74,
                                                     &H86, &HDF, &H97, &H75, &H93, &HEE, &HC2, &H5F, &HFD, &H8A, &H40, &H5C, &H92, &H5E, &HB5, &H7,
                                                     &HD6, &H12, &HE9, &H21, &H55, &HCE, &HD7, &HE5, &H15, &HF5, &HBA, &HBC, &H1B, &H31, &HAD, &H3C,
                                                     &H5E, &HE0, &H91, &H98, &HC2, &HE0, &H96, &HBB, &HAD, &HD, &H4E, &HF4, &H91, &H53, &H3D, &H84},
                               reader.GetBlobBytes(file1.HashValue))

                           Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))
                           Assert.Equal(New Byte() {&H79, &HFE, &H97, &HAB, &H8, &H8E, &HDF, &H74, &HC2, &HEF, &H84, &HBB, &HFC, &H74, &HAC, &H60,
                                                     &H18, &H6E, &H1A, &HD2, &HC5, &H94, &HE0, &HDA, &HE0, &H45, &H33, &H43, &H99, &HF0, &HF3, &HF1,
                                                     &H72, &H5, &H4B, &HF, &H37, &H50, &HC5, &HD9, &HCE, &H29, &H82, &H4C, &HF7, &HE6, &H94, &H5F,
                                                     &HE5, &H7, &H2B, &H4A, &H18, &H9, &H56, &HC9, &H52, &H69, &H7D, &HC4, &H48, &H63, &H70, &HF2},
                               reader.GetBlobBytes(file2.HashValue))

                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)

        Dim hash_module_Comp = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)>

public class Test
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseModule)

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={hash_module_Comp.EmitToImageReference()})

        CompileAndVerify(compilation,
            validator:=Sub(peAssembly)
                           Dim metadataReader = peAssembly.ManifestModule.GetMetadataReader()
                           Dim assembly As AssemblyDefinition = metadataReader.GetAssemblyDefinition()
                           Assert.Equal(AssemblyHashAlgorithm.MD5, assembly.HashAlgorithm)
                           Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute))
                       End Sub)


        compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345UI)>

class Program
    Sub M()
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll)

        ' no error reported if we don't need to hash
        compilation.VerifyEmitDiagnostics()

        compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345UI)>

class Program
    Sub M(x As Test)
    End Sub
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll, references:={hash_module})

        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream()).Diagnostics,
<expected>
BC37215: Cryptographic failure while creating hashes.
</expected>)

        compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345UI)>

class Program
end class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll)

        AssertTheseDiagnostics(compilation.Emit(New System.IO.MemoryStream(), manifestResources:=hash_resources).Diagnostics,
<expected>
BC37215: Cryptographic failure while creating hashes.
</expected>)


        Dim comp = CreateVisualBasicCompilation("AlgorithmIdAttribute",
        <![CDATA[<Assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)>]]>,
            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        VerifyAssemblyTable(comp, Sub(r)
                                      Assert.Equal(AssemblyHashAlgorithm.MD5, r.HashAlgorithm)
                                  End Sub)
        '
        comp = CreateVisualBasicCompilation("AlgorithmIdAttribute1",
        <![CDATA[<Assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.None)>]]>,
            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        VerifyAssemblyTable(comp, Sub(r)
                                      Assert.Equal(AssemblyHashAlgorithm.None, r.HashAlgorithm)
                                  End Sub)

        '
        comp = CreateVisualBasicCompilation("AlgorithmIdAttribute2",
        <![CDATA[<Assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345UI)>]]>,
            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        VerifyAssemblyTable(comp, Sub(r) Assert.Equal(12345, CInt(r.HashAlgorithm)))
    End Sub

    <Fact()>
    Public Sub AssemblyFlagsAttribute()
        Dim comp = CreateVisualBasicCompilation("AssemblyFlagsAttribute",
        <![CDATA[
Imports System.Reflection
<Assembly: AssemblyFlags(AssemblyNameFlags.EnableJITcompileOptimizer Or AssemblyNameFlags.Retargetable)>]]>,
            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        VerifyAssemblyTable(comp, Sub(r)
                                      Assert.Equal(AssemblyFlags.DisableJitCompileOptimizer Or AssemblyFlags.Retargetable, r.Flags)
                                  End Sub)

    End Sub

    <Fact, WorkItem(546635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546635")>
    Public Sub AssemblyFlagsAttribute02()
        Dim comp = CreateVisualBasicCompilation("AssemblyFlagsAttribute02",
        <![CDATA[<Assembly: System.Reflection.AssemblyFlags(12345)>]]>,
 compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        ' Both native & Roslyn PEVerifier fail: [MD]: Error: Invalid Assembly flags (0x3038). [token:0x20000001]
        VerifyAssemblyTable(comp, Sub(r)
                                      Assert.Equal(12345 - 1, CInt(r.Flags))
                                  End Sub)

        comp.VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "Assembly: System.Reflection.AssemblyFlags(12345)").WithArguments("Public Overloads Sub New(assemblyFlags As Integer)", "This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202"))

    End Sub

    <Fact>
    Public Sub AssemblyFlagsAttribute03()
        Dim comp = CreateVisualBasicCompilation("AssemblyFlagsAttribute02",
        <![CDATA[<Assembly: System.Reflection.AssemblyFlags(12345UI)>]]>,
 compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        ' Both native & Roslyn PEVerifier fail: [MD]: Error: Invalid Assembly flags (0x3038). [token:0x20000001]
        VerifyAssemblyTable(comp, Sub(r)
                                      Assert.Equal(12345 - 1, CInt(r.Flags))
                                  End Sub)

        comp.VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "Assembly: System.Reflection.AssemblyFlags(12345UI)").WithArguments("Public Overloads Sub New(flags As UInteger)", "This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202"))

    End Sub

#Region "Metadata Verifier (TODO: consolidate with others)"

    Friend Sub VerifyAssemblyTable(comp As VisualBasicCompilation, verifier As Action(Of AssemblyDefinition), Optional strData As String = Nothing)

        Dim stream = New MemoryStream()
        Assert.True(comp.Emit(stream).Success)

        Using mt = ModuleMetadata.CreateFromImage(stream.ToImmutable())

            Dim metadataReader = mt.Module.GetMetadataReader()
            Dim row As AssemblyDefinition = metadataReader.GetAssemblyDefinition()
            If verifier IsNot Nothing Then
                verifier(row)
            End If
            ' tmp
            If strData IsNot Nothing Then
                Assert.Equal(strData, metadataReader.GetString(row.Culture))
            End If

        End Using
    End Sub

#End Region

#Region "NetModule Assembly attribute tests"

#Region "Helpers"

    Private Shared ReadOnly s_defaultNetModuleSourceHeader As String = <![CDATA[
Imports System
Imports System.Reflection
Imports System.Security.Permissions

<Assembly: AssemblyTitle("AssemblyTitle")>
<Assembly: FileIOPermission(SecurityAction.RequestOptional)>
<Assembly: UserDefinedAssemblyAttrNoAllowMultiple("UserDefinedAssemblyAttrNoAllowMultiple")>
<Assembly: UserDefinedAssemblyAttrAllowMultiple("UserDefinedAssemblyAttrAllowMultiple")>
]]>.Value

    Private Shared ReadOnly s_defaultNetModuleSourceBody As String = <![CDATA[
Public Class NetModuleClass
End Class

<AttributeUsage(AttributeTargets.Assembly, AllowMultiple := False)>
Public Class UserDefinedAssemblyAttrNoAllowMultipleAttribute
	Inherits Attribute
	Public Property Text() As String
    Public Property Text2() As String
	Public Sub New(text1 As String)
		Text = text1
	End Sub
    Public Sub New(text1 As Integer)
		Text = text1.ToString()
	End Sub
End Class

<AttributeUsage(AttributeTargets.Assembly, AllowMultiple := True)>
Public Class UserDefinedAssemblyAttrAllowMultipleAttribute
	Inherits Attribute
	Public Property Text() As String
    Public Property Text2() As String
	Public Sub New(text1 As String)
		Text = text1
	End Sub
    Public Sub New(text1 As Integer)
		Text = text1.ToString()
	End Sub
End Class
]]>.Value

    Private Function GetNetModuleWithAssemblyAttributesRef(Optional netModuleSourceHeader As String = Nothing,
                                                           Optional netModuleSourceBody As String = Nothing,
                                                           Optional references As IEnumerable(Of MetadataReference) = Nothing,
                                                           Optional nameSuffix As String = "") As MetadataReference
        Return GetNetModuleWithAssemblyAttributes(netModuleSourceHeader, netModuleSourceBody, references, nameSuffix).GetReference()
    End Function

    Private Function GetNetModuleWithAssemblyAttributes(Optional netModuleSourceHeader As String = Nothing,
                                                        Optional netModuleSourceBody As String = Nothing,
                                                        Optional references As IEnumerable(Of MetadataReference) = Nothing,
                                                        Optional nameSuffix As String = "") As ModuleMetadata
        Dim netmoduleSource As String = If(netModuleSourceHeader, s_defaultNetModuleSourceHeader) & If(netModuleSourceBody, s_defaultNetModuleSourceBody)
        Dim netmoduleCompilation = CreateCompilationWithMscorlib({netmoduleSource}, references:=references, options:=TestOptions.ReleaseModule, assemblyName:="NetModuleWithAssemblyAttributes" & nameSuffix)
        Dim diagnostics = netmoduleCompilation.GetDiagnostics()
        Dim bytes = netmoduleCompilation.EmitToArray()
        Return ModuleMetadata.CreateFromImage(bytes)
    End Function

    Private Shared Sub TestDuplicateAssemblyAttributesNotEmitted(assembly As AssemblySymbol, expectedSrcAttrCount As Integer, expectedDuplicateAttrCount As Integer, attrTypeName As String)
        ' SOURCE ATTRIBUTES

        Dim allSrcAttrs = assembly.GetAttributes()
        Dim srcAttrs = allSrcAttrs.Where(Function(a) a.AttributeClass.Name.Equals(attrTypeName)).AsImmutable()

        Assert.Equal(expectedSrcAttrCount, srcAttrs.Length)

        ' EMITTED ATTRIBUTES

        Dim compilation = assembly.DeclaringCompilation
        compilation.GetDiagnostics()
        compilation.EmbeddedSymbolManager.MarkAllDeferredSymbolsAsReferenced(compilation)

        ' We should get only unique netmodule/assembly attributes here, duplicate ones should not be emitted.
        Dim expectedEmittedAttrsCount As Integer = expectedSrcAttrCount - expectedDuplicateAttrCount

        Dim allEmittedAttrs = assembly.GetCustomAttributesToEmit(New ModuleCompilationState).Cast(Of VisualBasicAttributeData)()
        Dim emittedAttrs = allEmittedAttrs.Where(Function(a) a.AttributeClass.Name.Equals(attrTypeName)).AsImmutable()

        Assert.Equal(expectedEmittedAttrsCount, emittedAttrs.Length)
        Dim uniqueAttributes = New HashSet(Of VisualBasicAttributeData)(comparer:=CommonAttributeDataComparer.Instance)
        For Each attr In emittedAttrs
            Assert.True(uniqueAttributes.Add(attr))
        Next
    End Sub
#End Region

    <Fact()>
    Public Sub AssemblyAttributesFromNetModule()

        Dim consoleappSource =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Class Program
	Private Shared Sub Main(args As String())
	End Sub
End Class
                    ]]>
                </file>
            </compilation>

        Dim netModuleWithAssemblyAttributes = GetNetModuleWithAssemblyAttributes()

        Dim metadata As PEModule = netModuleWithAssemblyAttributes.Module
        Dim metadataReader = metadata.GetMetadataReader()

        Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ExportedType))
        Assert.Equal(18, metadataReader.CustomAttributes.Count)
        Assert.Equal(0, metadataReader.DeclarativeSecurityAttributes.Count)

        Dim token As EntityHandle = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM")
        Assert.False(token.IsNil)   'could the type ref be located? If not then the attribute's not there.

        Dim consoleappCompilation = CreateCompilationWithMscorlibAndReferences(consoleappSource, {netModuleWithAssemblyAttributes.GetReference()})
        Dim diagnostics = consoleappCompilation.GetDiagnostics()

        Dim attrs = consoleappCompilation.Assembly.GetAttributes()
        Assert.Equal(4, attrs.Length)
        For Each a In attrs
            Select Case a.AttributeClass.Name
                Case "AssemblyTitleAttribute"
                    Assert.Equal("System.Reflection.AssemblyTitleAttribute(""AssemblyTitle"")", a.ToString())
                    Exit Select
                Case "FileIOPermissionAttribute"
                    Assert.Equal("System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrNoAllowMultipleAttribute"
                    Assert.Equal("UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrAllowMultipleAttribute"
                    Assert.Equal("UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")", a.ToString())
                    Exit Select
                Case Else
                    Assert.Equal("Unexpected Attr", a.AttributeClass.Name)
                    Exit Select
            End Select
        Next

        metadata = AssemblyMetadata.CreateFromImage(consoleappCompilation.EmitToArray()).GetAssembly.ManifestModule
        metadataReader = metadata.GetMetadataReader()

        Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ModuleRef))
        Assert.Equal(3, metadataReader.GetTableRowCount(TableIndex.ExportedType))
        Assert.Equal(6, metadataReader.CustomAttributes.Count)
        Assert.Equal(1, metadataReader.DeclarativeSecurityAttributes.Count)

        token = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM")
        Assert.True(token.IsNil)   'could the type ref be located? If not then the attribute's not there.

        consoleappCompilation = CreateCompilationWithMscorlibAndReferences(consoleappSource, {netModuleWithAssemblyAttributes.GetReference()}, TestOptions.ReleaseModule)
        Assert.Equal(0, consoleappCompilation.Assembly.GetAttributes().Length)

        Dim modRef = DirectCast(consoleappCompilation.EmitToImageReference(), MetadataImageReference)

        metadata = ModuleMetadata.CreateFromImage(consoleappCompilation.EmitToArray()).Module
        metadataReader = metadata.GetMetadataReader()

        Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ModuleRef))
        Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ExportedType))
        Assert.Equal(0, metadataReader.CustomAttributes.Count)
        Assert.Equal(0, metadataReader.DeclarativeSecurityAttributes.Count)

        token = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM")
        Assert.True(token.IsNil)   'could the type ref be located? If not then the attribute's not there.
    End Sub

    <Fact()>
    Public Sub AssemblyAttributesFromNetModuleDropIdentical()
        Dim consoleappSource =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
<Assembly: UserDefinedAssemblyAttrNoAllowMultiple("UserDefinedAssemblyAttrNoAllowMultiple")>
<Assembly: UserDefinedAssemblyAttrAllowMultiple("UserDefinedAssemblyAttrAllowMultiple")>

Class Program
	Private Shared Sub Main(args As String())
	End Sub
End Class
                    ]]>
                </file>
            </compilation>

        Dim consoleappCompilation = CreateCompilationWithMscorlibAndReferences(consoleappSource, {GetNetModuleWithAssemblyAttributesRef()})
        Dim diagnostics = consoleappCompilation.GetDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(consoleappCompilation.Assembly,
            expectedSrcAttrCount:=2,
            expectedDuplicateAttrCount:=1,
            attrTypeName:="UserDefinedAssemblyAttrAllowMultipleAttribute")

        TestDuplicateAssemblyAttributesNotEmitted(consoleappCompilation.Assembly,
           expectedSrcAttrCount:=2,
           expectedDuplicateAttrCount:=1,
           attrTypeName:="UserDefinedAssemblyAttrNoAllowMultipleAttribute")

        Dim attrs = consoleappCompilation.Assembly.GetAttributes()
        For Each a In attrs
            Select Case a.AttributeClass.Name
                Case "AssemblyTitleAttribute"
                    Assert.Equal("System.Reflection.AssemblyTitleAttribute(""AssemblyTitle"")", a.ToString())
                    Exit Select
                Case "FileIOPermissionAttribute"
                    Assert.Equal("System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrNoAllowMultipleAttribute"
                    Assert.Equal("UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrAllowMultipleAttribute"
                    Assert.Equal("UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")", a.ToString())
                    Exit Select
                Case Else
                    Assert.Equal("Unexpected Attr", a.AttributeClass.Name)
                    Exit Select
            End Select
        Next
    End Sub

    <Fact()>
    Public Sub AssemblyAttributesFromNetModuleDropSpecial()
        Dim consoleappSource =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Reflection

<Assembly: AssemblyTitle("AssemblyTitle (from source)")>

Class Program
	Private Shared Sub Main(args As String())
	End Sub
End Class
                    ]]>
                </file>
            </compilation>

        Dim consoleappCompilation = CreateCompilationWithMscorlibAndReferences(consoleappSource, {GetNetModuleWithAssemblyAttributesRef()})
        Dim diagnostics = consoleappCompilation.GetDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(consoleappCompilation.Assembly,
            expectedSrcAttrCount:=2,
            expectedDuplicateAttrCount:=1,
            attrTypeName:="AssemblyTitleAttribute")

        Dim attrs = consoleappCompilation.Assembly.GetCustomAttributesToEmit(New ModuleCompilationState).Cast(Of VisualBasicAttributeData)()
        For Each a In attrs
            Select Case a.AttributeClass.Name
                Case "AssemblyTitleAttribute"
                    Assert.Equal("System.Reflection.AssemblyTitleAttribute(""AssemblyTitle (from source)"")", a.ToString())
                    Exit Select
                Case "FileIOPermissionAttribute"
                    Assert.Equal("System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrNoAllowMultipleAttribute"
                    Assert.Equal("UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrAllowMultipleAttribute"
                    Assert.Equal("UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")", a.ToString())
                    Exit Select
                Case "CompilationRelaxationsAttribute",
                     "RuntimeCompatibilityAttribute",
                     "DebuggableAttribute"
                    ' synthesized attributes
                    Exit Select
                Case Else
                    Assert.Equal("Unexpected Attr", a.AttributeClass.Name)
                    Exit Select
            End Select
        Next
    End Sub

    <Fact()>
    Public Sub AssemblyAttributesFromNetModuleAddMulti()
        Dim consoleappSource =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultiple("UserDefinedAssemblyAttrAllowMultiple (from source)")>

Class Program
	Private Shared Sub Main(args As String())
	End Sub
End Class
                    ]]>
                </file>
            </compilation>

        Dim consoleappCompilation = CreateCompilationWithMscorlibAndReferences(consoleappSource, {GetNetModuleWithAssemblyAttributesRef()})
        Dim diagnostics = consoleappCompilation.GetDiagnostics()

        Dim attrs = consoleappCompilation.Assembly.GetAttributes()
        Assert.Equal(5, attrs.Length)
        For Each a In attrs
            Select Case a.AttributeClass.Name
                Case "AssemblyTitleAttribute"
                    Assert.Equal("System.Reflection.AssemblyTitleAttribute(""AssemblyTitle"")", a.ToString())
                    Exit Select
                Case "FileIOPermissionAttribute"
                    Assert.Equal("System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrNoAllowMultipleAttribute"
                    Assert.Equal("UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString())
                    Exit Select
                Case "UserDefinedAssemblyAttrAllowMultipleAttribute"
                    Assert.[True](("UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")" = a.ToString()) OrElse ("UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple (from source)"")" = a.ToString()), "Unexpected attribute construction")
                    Exit Select
                Case Else
                    Assert.Equal("Unexpected Attr", a.AttributeClass.Name)
                    Exit Select
            End Select
        Next
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub AssemblyAttributesFromNetModuleBadMulti()
        Dim source As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrNoAllowMultiple("UserDefinedAssemblyAttrNoAllowMultiple (from source)")>
]]>.Value

        Dim netmodule1Ref = GetNetModuleWithAssemblyAttributesRef()
        Dim comp = CreateCompilationWithMscorlib({source}, references:={netmodule1Ref}, options:=TestOptions.ReleaseDll)
        ' error BC36978: Attribute 'UserDefinedAssemblyAttrNoAllowMultipleAttribute' in 'NetModuleWithAssemblyAttributes.netmodule' cannot be applied multiple times.
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsageInNetModule2).WithArguments("UserDefinedAssemblyAttrNoAllowMultipleAttribute", "NetModuleWithAssemblyAttributes.netmodule"))

        Dim attrs = comp.Assembly.GetAttributes()
        ' even duplicates are preserved in source.
        Assert.Equal(5, attrs.Length)

        ' Build NetModule
        comp = CreateCompilationWithMscorlib({source}, references:={netmodule1Ref}, options:=TestOptions.ReleaseModule)
        comp.VerifyDiagnostics()
        Dim netmodule2Ref = comp.EmitToImageReference()

        attrs = comp.Assembly.GetAttributes()
        Assert.Equal(1, attrs.Length)

        comp = CreateCompilationWithMscorlib({""}, references:={netmodule1Ref, netmodule2Ref}, options:=TestOptions.ReleaseDll)
        ' error BC36978: Attribute 'UserDefinedAssemblyAttrNoAllowMultipleAttribute' in 'NetModuleWithAssemblyAttributes.netmodule' cannot be applied multiple times.
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsageInNetModule2).WithArguments("UserDefinedAssemblyAttrNoAllowMultipleAttribute", "NetModuleWithAssemblyAttributes.netmodule"))

        attrs = comp.Assembly.GetAttributes()
        ' even duplicates are preserved in source.
        Assert.Equal(5, attrs.Length)
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub InternalsVisibleToAttributeDropIdentical()
        Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("Assembly2")>
<Assembly: InternalsVisibleTo("Assembly2")>
                    ]]>
                </file>
            </compilation>

        Dim comp = CreateCompilationWithMscorlib(source, OutputKind.DynamicallyLinkedLibrary)
        CompileAndVerify(comp)

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=2,
            expectedDuplicateAttrCount:=1,
            attrTypeName:="InternalsVisibleToAttribute")
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub AssemblyAttributesFromSourceDropIdentical()
        Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' duplicate

<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str2", Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str1", Text := "str1")> ' unique
                    ]]>
                </file>
            </compilation>

        Dim netmoduleRef = GetNetModuleWithAssemblyAttributesRef()
        Dim comp = CreateCompilationWithMscorlibAndReferences(source, references:={netmoduleRef})
        Dim diagnostics = comp.GetDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=16,
            expectedDuplicateAttrCount:=5,
            attrTypeName:="UserDefinedAssemblyAttrAllowMultipleAttribute")
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub AssemblyAttributesFromSourceDropIdentical_02()
        Dim source1 As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrNoAllowMultipleAttribute(0)> ' unique
]]>.Value

        Dim source2 As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrNoAllowMultipleAttribute(0)> ' duplicate ignored, no error because identical
]]>.Value

        Dim defaultHeaderString As String = <![CDATA[
Imports System
]]>.Value

        Dim defsRef As MetadataReference = CreateCompilationWithMscorlib({defaultHeaderString & s_defaultNetModuleSourceBody}, references:=Nothing, options:=TestOptions.ReleaseDll).ToMetadataReference()
        Dim netmodule1Ref As MetadataReference = GetNetModuleWithAssemblyAttributesRef(source2, "", references:={defsRef}, nameSuffix:="1")

        Dim comp = CreateCompilationWithMscorlib({source1}, references:={defsRef, netmodule1Ref}, options:=TestOptions.ReleaseDll)
        ' duplicate ignored, no error because identical
        comp.VerifyDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=2,
            expectedDuplicateAttrCount:=1,
            attrTypeName:="UserDefinedAssemblyAttrNoAllowMultipleAttribute")

        Dim netmodule2Ref As MetadataReference = GetNetModuleWithAssemblyAttributesRef(source1, "", references:={defsRef}, nameSuffix:="2")
        comp = CreateCompilationWithMscorlib({""}, references:={defsRef, netmodule1Ref, netmodule2Ref}, options:=TestOptions.ReleaseDll)
        ' duplicate ignored, no error because identical
        comp.VerifyDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=2,
            expectedDuplicateAttrCount:=1,
            attrTypeName:="UserDefinedAssemblyAttrNoAllowMultipleAttribute")
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub AssemblyAttributesFromNetModuleDropIdentical_01()
        ' Duplicate ignored attributes in netmodule
        Dim netmoduleAttributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str2", Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str1", Text := "str1")> ' unique
                    ]]>.Value

        Dim netmoduleRef = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader & netmoduleAttributes, s_defaultNetModuleSourceBody)
        Dim comp = CreateCompilationWithMscorlib({""}, references:={netmoduleRef}, options:=TestOptions.ReleaseDll)
        Dim diagnostics = comp.GetDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=16,
            expectedDuplicateAttrCount:=5,
            attrTypeName:="UserDefinedAssemblyAttrAllowMultipleAttribute")
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub AssemblyAttributesFromNetModuleDropIdentical_02()
        ' Duplicate ignored attributes in netmodules
        Dim netmodule1Attributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str2", Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str1", Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' unique
                    ]]>.Value

        Dim netmodule2Attributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' duplicate
                    ]]>.Value

        Dim netmodule3Attributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' duplicate
                    ]]>.Value


        Dim defaultImportsString As String = <![CDATA[
Imports System
]]>.Value

        Dim defsRef As MetadataReference = CreateCompilationWithMscorlib({defaultImportsString & s_defaultNetModuleSourceBody}, references:=Nothing, options:=TestOptions.ReleaseDll).ToMetadataReference()
        Dim netmodule0Ref = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader, "", references:={defsRef})
        Dim netmodule1Ref = GetNetModuleWithAssemblyAttributesRef(netmodule1Attributes, "", references:={defsRef})
        Dim netmodule2Ref = GetNetModuleWithAssemblyAttributesRef(netmodule2Attributes, "", references:={defsRef})
        Dim netmodule3Ref = GetNetModuleWithAssemblyAttributesRef(netmodule3Attributes, "", references:={defsRef})

        Dim comp = CreateCompilationWithMscorlib({""}, references:={defsRef, netmodule0Ref, netmodule1Ref, netmodule2Ref, netmodule3Ref}, options:=TestOptions.ReleaseDll)
        Dim diagnostics = comp.GetDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=17,
            expectedDuplicateAttrCount:=6,
            attrTypeName:="UserDefinedAssemblyAttrAllowMultipleAttribute")
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub AssemblyAttributesFromSourceAndNetModuleDropIdentical_01()
        ' All duplicate ignored attributes in netmodule
        Dim netmoduleAttributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' duplicate
                    ]]>.Value

        Dim sourceAttributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str2")> ' unique

<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str2", Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str1", Text := "str1")> ' unique
                    ]]>.Value

        Dim netmoduleRef = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader & netmoduleAttributes, s_defaultNetModuleSourceBody)
        Dim comp = CreateCompilationWithMscorlib({sourceAttributes}, references:={netmoduleRef}, options:=TestOptions.ReleaseDll)
        Dim diagnostics = comp.GetDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=16,
            expectedDuplicateAttrCount:=5,
            attrTypeName:="UserDefinedAssemblyAttrAllowMultipleAttribute")
    End Sub

    <Fact(), WorkItem(546963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546963")>
    Public Sub AssemblyAttributesFromSourceAndNetModuleDropIdentical_02()
        ' Duplicate ignored attributes in netmodule & source
        Dim netmoduleAttributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' duplicate
                    ]]>.Value

        Dim sourceAttributes As String = <![CDATA[
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute("str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str2")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str2", Text := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' unique
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text := "str1", Text2 := "str1")> ' duplicate
<Assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 := "str1", Text := "str1")> ' unique
                    ]]>.Value

        Dim netmoduleRef = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader & netmoduleAttributes, s_defaultNetModuleSourceBody)
        Dim comp = CreateCompilationWithMscorlib({sourceAttributes}, references:={netmoduleRef}, options:=TestOptions.ReleaseDll)
        Dim diagnostics = comp.GetDiagnostics()

        TestDuplicateAssemblyAttributesNotEmitted(comp.Assembly,
            expectedSrcAttrCount:=21,
            expectedDuplicateAttrCount:=10,
            attrTypeName:="UserDefinedAssemblyAttrAllowMultipleAttribute")
    End Sub
#End Region

    <Fact, WorkItem(545527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545527")>
    Public Sub CompilationRelaxationsAndRuntimeCompatibility_MultiModule()
        Dim moduleSrc =
<compilation>
    <file><![CDATA[
Imports System.Runtime.CompilerServices

<Assembly:CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)>
<Assembly:RuntimeCompatibilityAttribute(WrapNonExceptionThrows:=False)>
]]>
    </file>
</compilation>

        Dim [module] = CreateCompilationWithMscorlib(moduleSrc, options:=TestOptions.ReleaseModule)

        Dim assemblySrc =
<compilation>
    <file>
Public Class C
End Class
    </file>
</compilation>

        Dim assembly = CreateCompilationWithMscorlib(assemblySrc, references:={[module].EmitToImageReference()})

        CompileAndVerify(assembly, symbolValidator:=
            Sub(moduleSymbol)
                Dim attrs = moduleSymbol.ContainingAssembly.GetAttributes().Select(Function(a) a.ToString()).ToArray()

                AssertEx.SetEqual({
                     "System.Diagnostics.DebuggableAttribute(System.Diagnostics.DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)",
                     "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute(WrapNonExceptionThrows:=False)",
                     "System.Runtime.CompilerServices.CompilationRelaxationsAttribute(System.Runtime.CompilerServices.CompilationRelaxations.NoStringInterning)"
                 },
                 attrs)
            End Sub)
    End Sub

    <Fact, WorkItem(546460, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546460")>
    Public Sub RuntimeCompatibilityAttribute_False()
        ' VB emits catch(Exception) even for an empty catch, so it can never catch non-Exception objects.

        Dim source =
<compilation>
    <file><![CDATA[
Imports System.Runtime.CompilerServices

<Assembly:RuntimeCompatibilityAttribute(WrapNonExceptionThrows:=False)>
Class C

    Public Shared Sub Main()
        Try
        Catch e As System.Exception
        Catch
        End Try
    End Sub
End Class
]]>
    </file>
</compilation>

        CreateCompilationWithMscorlibAndVBRuntime(source).AssertTheseDiagnostics(
<errors>
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
        Catch
        ~~~~~
</errors>)
    End Sub

    <Fact, WorkItem(530585, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530585")>
    Public Sub Bug16465()
        Dim modSource =
        <![CDATA[
Imports System.Configuration.Assemblies
Imports System.Reflection

<assembly: AssemblyAlgorithmId(AssemblyHashAlgorithm.SHA1)>
<assembly: AssemblyCulture("en-US")>
<assembly: AssemblyDelaySign(true)>
<assembly: AssemblyFlags(AssemblyNameFlags.EnableJITcompileOptimizer Or AssemblyNameFlags.Retargetable Or AssemblyNameFlags.EnableJITcompileTracking)>
<assembly: AssemblyVersion("1.2.3.4")>
<assembly: AssemblyFileVersion("4.3.2.1")>
<assembly: AssemblyTitle("HELLO")>
<assembly: AssemblyDescription("World")>
<assembly: AssemblyCompany("MS")>
<assembly: AssemblyProduct("Roslyn")>
<assembly: AssemblyInformationalVersion("Info")>
<assembly: AssemblyCopyright("Roslyn")>
<assembly: AssemblyTrademark("Roslyn")>

class Program1 
    Shared Sub Main()
    End Sub
End Class
]]>

        Dim source =
<compilation>
    <file><![CDATA[
Class C
End Class
]]>
    </file>
</compilation>

        Dim appCompilation = CreateCompilationWithMscorlibAndReferences(source, {GetNetModuleWithAssemblyAttributesRef(modSource.Value, "")})

        Dim m = DirectCast(appCompilation.Assembly.Modules(1), PEModuleSymbol)
        Dim metadata = m.Module
        Dim metadataReader = metadata.GetMetadataReader()


        Dim token As EntityHandle = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHere")
        Assert.False(token.IsNil())   'could the type ref be located? If not then the attribute's not there.

        Dim attributes = m.GetCustomAttributesForToken(token)
        Dim builder = New System.Text.StringBuilder()

        For Each attr In attributes
            builder.AppendLine(attr.ToString())
        Next

        Dim expectedStr =
        <![CDATA[
System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1)
System.Reflection.AssemblyCultureAttribute("en-US")
System.Reflection.AssemblyDelaySignAttribute(True)
System.Reflection.AssemblyFlagsAttribute(System.Reflection.AssemblyNameFlags.None Or System.Reflection.AssemblyNameFlags.EnableJITcompileOptimizer Or System.Reflection.AssemblyNameFlags.EnableJITcompileTracking Or System.Reflection.AssemblyNameFlags.Retargetable)
System.Reflection.AssemblyVersionAttribute("1.2.3.4")
System.Reflection.AssemblyFileVersionAttribute("4.3.2.1")
System.Reflection.AssemblyTitleAttribute("HELLO")
System.Reflection.AssemblyDescriptionAttribute("World")
System.Reflection.AssemblyCompanyAttribute("MS")
System.Reflection.AssemblyProductAttribute("Roslyn")
System.Reflection.AssemblyInformationalVersionAttribute("Info")
System.Reflection.AssemblyCopyrightAttribute("Roslyn")
System.Reflection.AssemblyTrademarkAttribute("Roslyn")
]]>.Value.Trim()

        expectedStr = CompilationUtils.FilterString(expectedStr)

        Dim actualStr = CompilationUtils.FilterString(builder.ToString().Trim())

        Assert.True(expectedStr.Equals(actualStr), AssertEx.GetAssertMessage(expectedStr, actualStr))
    End Sub

    <Fact, WorkItem(530579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530579")>
    Public Sub Bug530579_1()
        Dim mod1Source =
<compilation name="M1">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module1")>
    ]]></file>
</compilation>


        Dim mod2Source =
<compilation name="M2">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module1")>
    ]]></file>
</compilation>

        Dim source =
<compilation name="M3">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module1")>
]]>
    </file>
</compilation>

        Dim compMod1 = CreateCompilationWithMscorlib(mod1Source, TestOptions.ReleaseModule)
        Dim compMod2 = CreateCompilationWithMscorlib(mod2Source, TestOptions.ReleaseModule)

        Dim appCompilation = CreateCompilationWithMscorlibAndReferences(source,
                                                                        {compMod1.EmitToImageReference(), compMod2.EmitToImageReference()},
                                                                        TestOptions.ReleaseDll)

        Assert.Equal(3, appCompilation.Assembly.Modules.Length)

        CompileAndVerify(appCompilation, symbolValidator:=Sub(m As ModuleSymbol)
                                                              Dim list As New ArrayBuilder(Of VisualBasicAttributeData)
                                                              GetAssemblyDescriptionAttributes(m.ContainingAssembly, list)

                                                              Assert.Equal(1, list.Count)
                                                              Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(""Module1"")", list(0).ToString())
                                                          End Sub).VerifyDiagnostics()

    End Sub

    Private Shared Sub GetAssemblyDescriptionAttributes(assembly As AssemblySymbol, list As ArrayBuilder(Of VisualBasicAttributeData))
        For Each attrData In assembly.GetAttributes()
            If attrData.IsTargetAttribute(assembly, AttributeDescription.AssemblyDescriptionAttribute) Then
                list.Add(attrData)
            End If
        Next
    End Sub

    <Fact, WorkItem(530579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530579")>
    Public Sub Bug530579_2()
        Dim mod1Source =
<compilation name="M1">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module1")>
    ]]></file>
</compilation>


        Dim mod2Source =
<compilation name="M2">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module2")>
    ]]></file>
</compilation>

        Dim source =
<compilation name="M3">
    <file><![CDATA[
]]>
    </file>
</compilation>

        Dim compMod1 = CreateCompilationWithMscorlib(mod1Source, TestOptions.ReleaseModule)
        Dim compMod2 = CreateCompilationWithMscorlib(mod2Source, TestOptions.ReleaseModule)

        Dim appCompilation = CreateCompilationWithMscorlibAndReferences(source,
                                                                        {compMod1.EmitToImageReference(), compMod2.EmitToImageReference()},
                                                                        TestOptions.ReleaseDll)

        Assert.Equal(3, appCompilation.Assembly.Modules.Length)

        AssertTheseDiagnostics(appCompilation,
<expected>
BC42370: Attribute 'AssemblyDescriptionAttribute' from module 'M1.netmodule' will be ignored in favor of the instance appearing in source.
</expected>)

        CompileAndVerify(appCompilation, symbolValidator:=Sub(m As ModuleSymbol)
                                                              Dim list As New ArrayBuilder(Of VisualBasicAttributeData)
                                                              GetAssemblyDescriptionAttributes(m.ContainingAssembly, list)

                                                              Assert.Equal(1, list.Count)
                                                              Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(""Module2"")", list(0).ToString())
                                                          End Sub)

    End Sub

    <Fact, WorkItem(530579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530579")>
    Public Sub Bug530579_3()
        Dim mod1Source =
<compilation name="M1">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module1")>
    ]]></file>
</compilation>


        Dim mod2Source =
<compilation name="M2">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module2")>
    ]]></file>
</compilation>

        Dim source =
<compilation name="M3">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module3")>
]]>
    </file>
</compilation>

        Dim compMod1 = CreateCompilationWithMscorlib(mod1Source, TestOptions.ReleaseModule)
        Dim compMod2 = CreateCompilationWithMscorlib(mod2Source, TestOptions.ReleaseModule)

        Dim appCompilation = CreateCompilationWithMscorlibAndReferences(source,
                                                                        {compMod1.EmitToImageReference(), compMod2.EmitToImageReference()},
                                                                        TestOptions.ReleaseDll)

        Assert.Equal(3, appCompilation.Assembly.Modules.Length)

        AssertTheseDiagnostics(appCompilation,
<expected>
BC42370: Attribute 'AssemblyDescriptionAttribute' from module 'M1.netmodule' will be ignored in favor of the instance appearing in source.
BC42370: Attribute 'AssemblyDescriptionAttribute' from module 'M2.netmodule' will be ignored in favor of the instance appearing in source.
</expected>)

        CompileAndVerify(appCompilation, symbolValidator:=Sub(m As ModuleSymbol)
                                                              Dim list As New ArrayBuilder(Of VisualBasicAttributeData)
                                                              GetAssemblyDescriptionAttributes(m.ContainingAssembly, list)

                                                              Assert.Equal(1, list.Count)
                                                              Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(""Module3"")", list(0).ToString())
                                                          End Sub)

    End Sub

    <Fact, WorkItem(530579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530579")>
    Public Sub Bug530579_4()
        Dim mod1Source =
<compilation name="M1">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module1")>
    ]]></file>
</compilation>


        Dim mod2Source =
<compilation name="M2">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module2")>
    ]]></file>
</compilation>

        Dim source =
<compilation name="M3">
    <file><![CDATA[
<Assembly:System.Reflection.AssemblyDescriptionAttribute("Module1")>
]]>
    </file>
</compilation>

        Dim compMod1 = CreateCompilationWithMscorlib(mod1Source, TestOptions.ReleaseModule)
        Dim compMod2 = CreateCompilationWithMscorlib(mod2Source, TestOptions.ReleaseModule)

        Dim appCompilation = CreateCompilationWithMscorlibAndReferences(source,
                                                                        {compMod1.EmitToImageReference(), compMod2.EmitToImageReference()},
                                                                        TestOptions.ReleaseDll)

        Assert.Equal(3, appCompilation.Assembly.Modules.Length)

        AssertTheseDiagnostics(appCompilation,
<expected>
BC42370: Attribute 'AssemblyDescriptionAttribute' from module 'M2.netmodule' will be ignored in favor of the instance appearing in source.
</expected>)

        CompileAndVerify(appCompilation, symbolValidator:=Sub(m As ModuleSymbol)
                                                              Dim list As New ArrayBuilder(Of VisualBasicAttributeData)
                                                              GetAssemblyDescriptionAttributes(m.ContainingAssembly, list)

                                                              Assert.Equal(1, list.Count)
                                                              Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(""Module1"")", list(0).ToString())
                                                          End Sub)

    End Sub

End Class
