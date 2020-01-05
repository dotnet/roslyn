' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.SigningTestHelpers

Partial Public Class InternalsVisibleToAndStrongNameTests
    Inherits BasicTestBase

    Public Shared ReadOnly Property AllProviderParseOptions As IEnumerable(Of Object())
        Get
            If ExecutionConditionUtil.IsWindows Then
                Return New Object()() {
                    New Object() {TestOptions.Regular},
                    New Object() {TestOptions.RegularWithLegacyStrongName}
                }
            End If

            Return SpecializedCollections.SingletonEnumerable(
                New Object() {TestOptions.Regular}
            )
        End Get
    End Property


#Region "Helpers"

    Public Sub New()
        SigningTestHelpers.InstallKey()
    End Sub

    Private Shared ReadOnly s_keyPairFile As String = SigningTestHelpers.KeyPairFile
    Private Shared ReadOnly s_publicKeyFile As String = SigningTestHelpers.PublicKeyFile
    Private Shared ReadOnly s_publicKey As ImmutableArray(Of Byte) = SigningTestHelpers.PublicKey

    Private Shared Function GetDesktopProviderWithPath(keyFilePath As String) As StrongNameProvider
        Return New DesktopStrongNameProvider(ImmutableArray.Create(keyFilePath), New VirtualizedStrongNameFileSystem())
    End Function

    Private Shared Sub VerifySigned(comp As Compilation, Optional expectedToBeSigned As Boolean = True)
        Using outStream = comp.EmitToStream()
            outStream.Position = 0

            Dim headers = New PEHeaders(outStream)
            Assert.Equal(expectedToBeSigned, headers.CorHeader.Flags.HasFlag(CorFlags.StrongNameSigned))
        End Using
    End Sub

#End Region

#Region "Naming Tests"

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyFileAttribute(parseOptions As VisualBasicParseOptions)
        Dim x = s_keyPairFile
        Dim s = "<Assembly: System.Reflection.AssemblyKeyFile(""" & x & """)>" & vbCrLf &
                "Public Class C" & vbCrLf &
                "End Class"

        Dim g = Guid.NewGuid()
        Dim other = VisualBasicCompilation.Create(
            g.ToString(),
            {VisualBasicSyntaxTree.ParseText(s, parseOptions)},
            {MscorlibRef},
            TestOptions.SigningReleaseDll)

        other.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver(parseOptions As VisualBasicParseOptions)
        Dim keyFileDir = Path.GetDirectoryName(s_keyPairFile)
        Dim keyFileName = Path.GetFileName(s_keyPairFile)

        Dim s = "<Assembly: System.Reflection.AssemblyKeyFile(""" & keyFileName & """)>" & vbCrLf &
                "Public Class C" & vbCrLf &
                "End Class"

        Dim syntaxTree = ParseAndVerify(s, parseOptions)

        ' verify failure with default assembly key file resolver
        Dim comp = CreateCompilationWithMscorlib40({syntaxTree}, options:=TestOptions.SigningReleaseDll)
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(keyFileName, CodeAnalysisResources.FileNotFound))

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with keyFileDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            {syntaxTree},
            {MscorlibRef},
            TestOptions.ReleaseDll.WithStrongNameProvider(GetDesktopProviderWithPath(keyFileDir)))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver_RelativeToCurrentParent(parseOptions As VisualBasicParseOptions)
        Dim keyFileDir = Path.GetDirectoryName(s_keyPairFile)
        Dim keyFileName = Path.GetFileName(s_keyPairFile)

        Dim s = "<Assembly: System.Reflection.AssemblyKeyFile(""..\" & keyFileName & """)>" & vbCrLf &
                "Public Class C" & vbCrLf &
                "End Class"

        Dim syntaxTree = ParseAndVerify(s, parseOptions)

        ' verify failure with default assembly key file resolver
        Dim comp As Compilation = CreateCompilationWithMscorlib40({syntaxTree}, options:=TestOptions.SigningReleaseDll)
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments("..\" & keyFileName, CodeAnalysisResources.FileNotFound))

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with keyFileDir\TempSubDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:={MscorlibRef},
            syntaxTrees:={syntaxTree},
            options:=TestOptions.ReleaseDll.WithStrongNameProvider(GetDesktopProviderWithPath(PathUtilities.CombineAbsoluteAndRelativePaths(keyFileDir, "TempSubDir\"))))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyContainerAttribute(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyFileOptions(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        other.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyFileOptions_ReferenceResolver(parseOptions As VisualBasicParseOptions)
        Dim keyFileDir = Path.GetDirectoryName(s_keyPairFile)
        Dim keyFileName = Path.GetFileName(s_keyPairFile)

        Dim source = <![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
        Dim references = {MscorlibRef}
        Dim syntaxTrees = {ParseAndVerify(source, parseOptions)}

        ' verify failure with default resolver
        Dim comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(keyFileName))

        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(keyFileName, CodeAnalysisResources.FileNotFound))

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with keyFileDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.ReleaseDll.WithCryptoKeyFile(keyFileName).WithStrongNameProvider(GetDesktopProviderWithPath(keyFileDir)))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyFileOptionsJustPublicKey(parseOptions As VisualBasicParseOptions)
        Dim s =
            <compilation>
                <file name="Clavelle.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
                </file>
            </compilation>
        Dim other = CreateCompilationWithMscorlib40(s, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(True), parseOptions:=parseOptions)

        Assert.Empty(other.GetDiagnostics())
        Assert.True(ByteSequenceComparer.Equals(TestResources.General.snPublicKey.AsImmutableOrNull(), other.Assembly.Identity.PublicKey))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFromKeyFileOptionsJustPublicKey_ReferenceResolver(parseOptions As VisualBasicParseOptions)
        Dim publicKeyFileDir = Path.GetDirectoryName(s_publicKeyFile)
        Dim publicKeyFileName = Path.GetFileName(s_publicKeyFile)

        Dim source = <![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>

        Dim references = {MscorlibRef}
        Dim syntaxTrees = {ParseAndVerify(source, parseOptions)}

        ' verify failure with default resolver
        Dim comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(True))

        ' error CS7027: Error extracting public key from file 'PublicKeyFile.snk' -- File not found.
        ' warning CS7033: Delay signing was specified and requires a public key, but no public key was specified
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(publicKeyFileName, CodeAnalysisResources.FileNotFound),
            Diagnostic(ERRID.WRN_DelaySignButNoKey))
        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with publicKeyFileDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.ReleaseDll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(True).WithStrongNameProvider(GetDesktopProviderWithPath(publicKeyFileDir)))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFileNotFoundOptions(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseExe.WithCryptoKeyFile("goo"), parseOptions:=parseOptions)

        CompilationUtils.AssertTheseDeclarationDiagnostics(other,
            <errors>
BC36980: Error extracting public key from file 'goo': <%= CodeAnalysisResources.FileNotFound %>
            </errors>)
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub KeyFileAttributeEmpty(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyFile("")>
Public Class C
 Friend Sub Goo()
    End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub KeyContainerEmpty(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("")>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicKeyFromOptions_DelaySigned(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyDelaySign(True)>
Public Class C 
End Class
]]>
    </file>
</compilation>

        Dim c = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithCryptoPublicKey(s_publicKey), parseOptions:=parseOptions)
        c.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, c.Assembly.Identity.PublicKey))

        Dim Metadata = ModuleMetadata.CreateFromImage(c.EmitToArray())
        Dim identity = Metadata.Module.ReadAssemblyIdentityOrThrow()

        Assert.True(identity.HasPublicKey)
        AssertEx.Equal(identity.PublicKey, s_publicKey)
        Assert.Equal(CorFlags.ILOnly, Metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    <WorkItem(11427, "https://github.com/dotnet/roslyn/issues/11427")>
    Public Sub PublicKeyFromOptions_PublicSign(parseOptions As VisualBasicParseOptions)
        ' attributes are ignored
        Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
<assembly: System.Reflection.AssemblyKeyFile("some file")>
Public Class C
End Class
]]>
    </file>
</compilation>

        Dim c = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithCryptoPublicKey(s_publicKey).WithPublicSign(True), parseOptions:=parseOptions)
        c.AssertTheseDiagnostics(
            <expected>
BC42379: Attribute 'System.Reflection.AssemblyKeyFileAttribute' is ignored when public signing is specified.
BC42379: Attribute 'System.Reflection.AssemblyKeyNameAttribute' is ignored when public signing is specified.
            </expected>
        )
        Assert.True(ByteSequenceComparer.Equals(s_publicKey, c.Assembly.Identity.PublicKey))

        Dim Metadata = ModuleMetadata.CreateFromImage(c.EmitToArray())
        Dim identity = Metadata.Module.ReadAssemblyIdentityOrThrow()

        Assert.True(identity.HasPublicKey)
        AssertEx.Equal(identity.PublicKey, s_publicKey)
        Assert.Equal(CorFlags.ILOnly Or CorFlags.StrongNameSigned, Metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags)

        c = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseModule.WithCryptoPublicKey(s_publicKey).WithPublicSign(True), parseOptions:=parseOptions)
        c.AssertTheseDiagnostics(
            <expected>
BC37282: Public signing is not supported for netmodules.
            </expected>
        )

        c = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseModule.WithCryptoKeyFile(s_publicKeyFile).WithPublicSign(True), parseOptions:=parseOptions)
        c.AssertTheseDiagnostics(
            <expected>
BC37207: Attribute 'System.Reflection.AssemblyKeyFileAttribute' given in a source file conflicts with option 'CryptoKeyFile'.
BC37282: Public signing is not supported for netmodules.
            </expected>
        )

        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey)

        Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
<assembly: System.Reflection.AssemblyKeyFile("]]><%= snk.Path %><![CDATA[")>
Public Class C
End Class
]]>
    </file>
</compilation>

        c = CreateCompilationWithMscorlib40(source1, options:=TestOptions.ReleaseModule.WithCryptoKeyFile(snk.Path).WithPublicSign(True), parseOptions:=parseOptions)
        c.AssertTheseDiagnostics(
            <expected>
BC37282: Public signing is not supported for netmodules.
            </expected>
        )
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicKeyFromOptions_InvalidCompilationOptions(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C 
End Class
]]>
    </file>
</compilation>

        Dim c = CreateCompilationWithMscorlib40(source, options:=TestOptions.SigningReleaseDll.
            WithCryptoPublicKey(ImmutableArray.Create(Of Byte)(1, 2, 3)).
            WithCryptoKeyContainer("roslynTestContainer").
            WithCryptoKeyFile("file.snk"), parseOptions:=parseOptions)

        AssertTheseDiagnostics(c,
<error>
BC2014: the value '01-02-03' is invalid for option 'CryptoPublicKey'
BC2046: Compilation options 'CryptoPublicKey' and 'CryptoKeyContainer' can't both be specified at the same time.
BC2046: Compilation options 'CryptoPublicKey' and 'CryptoKeyFile' can't both be specified at the same time.
</error>)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyFileBogusOptions(parseOptions As VisualBasicParseOptions)
        Dim tmp = Temp.CreateFile().WriteAllBytes(New Byte() {1, 2, 3, 4})
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file>
        <![CDATA[
Public Class C
Friend Sub Goo()
End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(tmp.Path).WithStrongNameProvider(New DesktopStrongNameProvider()),
        parseOptions:=parseOptions)

        other.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(tmp.Path, CodeAnalysisResources.InvalidPublicKey))

        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PubKeyContainerBogusOptions(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseExe.WithCryptoKeyContainer("goo"), parseOptions:=parseOptions)

        '        CompilationUtils.AssertTheseDeclarationDiagnostics(other,
        '            <errors>
        'BC36981: Error extracting public key from container 'goo': Keyset does not exist (Exception from HRESULT: 0x80090016)                    
        '                </errors>)
        Dim err = other.GetDeclarationDiagnostics().Single()

        Assert.Equal(ERRID.ERR_PublicKeyContainerFailure, err.Code)
        Assert.Equal(2, err.Arguments.Count)
        Assert.Equal("goo", DirectCast(err.Arguments(0), String))
        Dim errorText = DirectCast(err.Arguments(1), String)
        Assert.True(
            errorText.Contains("HRESULT") AndAlso
            errorText.Contains("0x80090016"))

        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub
#End Region

#Region "IVT Access checking"
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTBasicCompilation(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="HasIVTToCompilation">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccess")>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim c As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="WantsIVTAccessButCantHave">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Goo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        'compilation should not succeed, and internals should not be imported.
        c.GetDiagnostics()

        CompilationUtils.AssertTheseDiagnostics(c, <error>
BC30390: 'C.Friend Sub Goo()' is not accessible in this context because it is 'Friend'.
          o.Goo()
          ~~~~~
</error>)

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Goo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        c2.VerifyDiagnostics()
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTBasicMetadata(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="HasIVTToCompilation">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccess")>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim otherImage = other.EmitToArray()

        Dim c As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="WantsIVTAccessButCantHave">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Goo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {MetadataReference.CreateFromImage(otherImage)}, TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        'compilation should not succeed, and internals should not be imported.
        c.GetDiagnostics()

        'gives "is not a member" error because internals were not imported because no IVT was found
        'on HasIVTToCompilation that referred to WantsIVTAccessButCantHave
        CompilationUtils.AssertTheseDiagnostics(c, <error>
BC30456: 'Goo' is not a member of 'C'.
          o.Goo()
          ~~~~~
</error>)

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Goo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {MetadataReference.CreateFromImage(otherImage)}, TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        c2.VerifyDiagnostics()
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyContainerBogus(parseOptions As VisualBasicParseOptions)
        Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("bogus")>
Public Class A
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseModule, parseOptions:=parseOptions)

        'shouldn't have an error. The attribute's contents are checked when the module is added.
        Dim reference = c1.EmitToImageReference()

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
(<compilation name="WantsIVTAccess">
     <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
     </file>
 </compilation>), {reference}, TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        'BC36981: Error extracting public key from container 'bogus': Keyset does not exist (Exception from HRESULT: 0x80090016)
        'c2.VerifyDiagnostics(Diagnostic(ERRID.ERR_PublicKeyContainerFailure).WithArguments("bogus", "Keyset does not exist (Exception from HRESULT: 0x80090016)"))

        Dim err = c2.GetDiagnostics(CompilationStage.Emit).Single()
        Assert.Equal(ERRID.ERR_PublicKeyContainerFailure, err.Code)
        Assert.Equal(2, err.Arguments.Count)
        Assert.Equal("bogus", DirectCast(err.Arguments(0), String))
        Dim errorText = DirectCast(err.Arguments(1), String)
        Assert.True(
            errorText.Contains("HRESULT") AndAlso
            errorText.Contains("0x80090016"))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyFileBogus(parseOptions As VisualBasicParseOptions)
        Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyFile("bogus")>
Public Class A
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseModule, parseOptions:=parseOptions)

        'shouldn't have an error. The attribute's contents are checked when the module is added.
        Dim reference = c1.EmitToImageReference()

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
(<compilation name="WantsIVTAccess">
     <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
     </file>
 </compilation>), {reference}, TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        c2.VerifyDiagnostics(Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments("bogus", CodeAnalysisResources.FileNotFound))
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTSigned(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithDelaySign(True), parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class A
    Private Sub New(o As C)
        o.Goo()
    End Sub
End Class
]]>
    </file>
</compilation>,
{New VisualBasicCompilationReference(other)}, TestOptions.SigningReleaseDll.WithCryptoKeyContainer("roslynTestContainer"), parseOptions:=parseOptions)

        Dim unused = requestor.Assembly.Identity
        requestor.VerifyDiagnostics()
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTErrorNotBothSigned_VBtoVB(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class A
    Private Sub New(o As C)
        o.Goo()
    End Sub
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithDelaySign(True), parseOptions:=parseOptions)

        Dim unused = requestor.Assembly.Identity
        'gives "is not accessible" error because internals were imported because IVT was found
        CompilationUtils.AssertTheseDiagnostics(requestor, <error>BC30389: 'C' is not accessible in this context because it is 'Friend'.
    Private Sub New(o As C)
                         ~
</error>)

    End Sub

    <WorkItem(781312, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/781312")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub Bug781312(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class A
    Private Sub New(o As C)
        o.Goo()
    End Sub
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, TestOptions.SigningReleaseModule, parseOptions:=parseOptions)

        Dim unused = requestor.Assembly.Identity
        CompilationUtils.AssertTheseDiagnostics(requestor, <error></error>)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTErrorNotBothSigned_CStoVB(parseOptions As VisualBasicParseOptions)
        Dim cSource = "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            public class C { internal void Goo() {} }"
        Dim other As CSharp.CSharpCompilation = CSharp.CSharpCompilation.Create(
            assemblyName:="Paul",
            syntaxTrees:={CSharp.CSharpSyntaxTree.ParseText(cSource)},
            references:={MscorlibRef_v4_0_30316_17626},
            options:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithStrongNameProvider(DefaultDesktopStrongNameProvider))

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class A
    Private Sub New(o As C)
        o.Goo()
    End Sub
End Class
]]>
    </file>
</compilation>, {MetadataReference.CreateFromImage(other.EmitToArray())}, TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithDelaySign(True), parseOptions:=parseOptions)

        Dim unused = requestor.Assembly.Identity
        'gives "is not accessible" error because internals were imported because IVT was found
        CompilationUtils.AssertTheseDiagnostics(requestor, <error>BC30390: 'C.Friend Overloads Sub Goo()' is not accessible in this context because it is 'Friend'.
        o.Goo()
        ~~~~~
</error>)

    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTDeferredSuccess(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithDelaySign(True), parseOptions:=parseOptions)
        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C 'causes optimistic granting
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim unused = requestor.Assembly.Identity
        Assert.True(DirectCast(other.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
        requestor.AssertNoDiagnostics()
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTDeferredFailSignMismatch(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim unused = requestor.Assembly.Identity
        CompilationUtils.AssertTheseDiagnostics(requestor,
            <error>BC36958: Friend access was granted by 'Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null', but the strong name signing state of the output assembly does not match that of the granting assembly.</error>)
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTDeferredFailKeyMismatch(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
'key is wrong in the first digit
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=10240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll.WithCryptoKeyContainer("roslynTestContainer"), parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim unused = requestor.Assembly.Identity
        CompilationUtils.AssertTheseDiagnostics(requestor, <errors>BC36957: Friend access was granted by 'Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2', but the public key of the output assembly does not match that specified by the attribute in the granting assembly.</errors>)

    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTSuccessThroughIAssembly(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithDelaySign(True), parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C 'causes optimistic granting
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Assert.True(DirectCast(other.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTFailSignMismatchThroughIAssembly(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Assert.False(DirectCast(other.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
    End Sub

    <WorkItem(820450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820450")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTGivesAccessToUsingDifferentKeys(parseOptions As VisualBasicParseOptions)
        Dim giver As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Namespace ClassLibrary
    Friend Class FriendClass
     Public Sub Goo()
     End Sub
    End Class
end Namespace
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(SigningTestHelpers.KeyPairFile2), parseOptions:=parseOptions)

        giver.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class ClassWithFriendMethod
    Friend Sub Test(A as ClassLibrary.FriendClass)
    End Sub
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(giver)}, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Assert.True(DirectCast(giver.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
        Assert.Empty(requestor.GetDiagnostics())
    End Sub
#End Region

#Region "IVT instantiations"
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTHasCulture(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("WantsIVTAccess, Culture=neutral")>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim expectedErrors = <error><![CDATA[
BC31534: Friend assembly reference 'WantsIVTAccess, Culture=neutral' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
<Assembly: InternalsVisibleTo("WantsIVTAccess, Culture=neutral")>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></error>
        CompilationUtils.AssertTheseDeclarationDiagnostics(other, expectedErrors)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub IVTNoKey(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("WantsIVTAccess")>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Dim expectedErrors = <error><![CDATA[
BC31535: Friend assembly reference 'WantsIVTAccess' is invalid. Strong-name signed assemblies must specify a public key in their InternalsVisibleTo declarations.
<Assembly: InternalsVisibleTo("WantsIVTAccess")>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></error>
        CompilationUtils.AssertTheseDeclarationDiagnostics(other, expectedErrors)
    End Sub
#End Region

#Region "Signing"

    <ConditionalTheory(GetType(DesktopOnly), Reason:="https://github.com/dotnet/coreclr/issues/21723")>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub MaxSizeKey(parseOptions As VisualBasicParseOptions)
        Dim pubKey = TestResources.General.snMaxSizePublicKeyString
        Const pubKeyToken = "1540923db30520b2"
        Dim pubKeyTokenBytes As Byte() = {&H15, &H40, &H92, &H3D, &HB3, &H5, &H20, &HB2}

        Dim comp = CreateCompilation(
<compilation>
    <file name="c.vb">
Imports System
Imports System.Runtime.CompilerServices

&lt;Assembly:InternalsVisibleTo("MaxSizeComp2, PublicKey=<%= pubKey %>, PublicKeyToken=<%= pubKeyToken %>")&gt;

Friend Class C
    Public Shared Sub M()
        Console.WriteLine("Called M")
    End Sub
End Class
    </file>
</compilation>,
                options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(SigningTestHelpers.MaxSizeKeyFile), parseOptions:=parseOptions)

        comp.VerifyEmitDiagnostics()

        Assert.True(comp.IsRealSigned)
        VerifySigned(comp)
        Assert.Equal(TestResources.General.snMaxSizePublicKey, comp.Assembly.Identity.PublicKey)
        Assert.Equal(Of Byte)(pubKeyTokenBytes, comp.Assembly.Identity.PublicKeyToken)

        Dim src =
<compilation name="MaxSizeComp2">
    <file name="c.vb">
Class D
    Public Shared Sub Main()
        C.M()
    End Sub
End Class
    </file>
</compilation>

        Dim comp2 = CreateCompilation(src, references:={comp.ToMetadataReference()},
            options:=TestOptions.SigningReleaseExe.WithCryptoKeyFile(SigningTestHelpers.MaxSizeKeyFile), parseOptions:=parseOptions)

        CompileAndVerify(comp2, expectedOutput:="Called M")
        Assert.Equal(TestResources.General.snMaxSizePublicKey, comp2.Assembly.Identity.PublicKey)
        Assert.Equal(Of Byte)(pubKeyTokenBytes, comp2.Assembly.Identity.PublicKeyToken)

        Dim comp3 = CreateCompilation(src, references:={comp.EmitToImageReference()},
            options:=TestOptions.SigningReleaseExe.WithCryptoKeyFile(SigningTestHelpers.MaxSizeKeyFile), parseOptions:=parseOptions)

        CompileAndVerify(comp3, expectedOutput:="Called M")
        Assert.Equal(TestResources.General.snMaxSizePublicKey, comp3.Assembly.Identity.PublicKey)
        Assert.Equal(Of Byte)(pubKeyTokenBytes, comp3.Assembly.Identity.PublicKeyToken)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignIt(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Dim peHeaders = New PEHeaders(other.EmitToStream())
        Assert.Equal(CorFlags.StrongNameSigned, peHeaders.CorHeader.Flags And CorFlags.StrongNameSigned)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignItWithOnlyPublicKey(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_publicKeyFile), parseOptions:=parseOptions)

        Using outStrm = New MemoryStream()
            Dim emitResult = other.Emit(outStrm)

            CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics,
<errors>
BC36961: Key file '<%= s_publicKeyFile %>' is missing the private key needed for signing.
</errors>)
        End Using

        other = other.WithOptions(TestOptions.ReleaseModule.WithCryptoKeyFile(s_publicKeyFile))

        Dim assembly As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="Sam2">
    <file name="a.vb">
    </file>
</compilation>,
        {other.EmitToImageReference()},
        options:=TestOptions.SigningReleaseDll,
        parseOptions:=parseOptions)

        Using outStrm = New MemoryStream()
            Dim emitResult = assembly.Emit(outStrm)

            CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics,
<errors>
BC36961: Key file '<%= s_publicKeyFile %>' is missing the private key needed for signing.
</errors>)
        End Using
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub DelaySignItWithOnlyPublicKey(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDelaySign(True)>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_publicKeyFile), parseOptions:=parseOptions)

        CompileAndVerify(other)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub DelaySignButNoKey(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDelaySign(True)>
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)
        ' Dev11: vbc : warning BC40010: Possible problem detected while building assembly 'VBTestD': Delay signing was requested, but no key was given
        '              warning BC41008: Use command-line option '/delaysign' or appropriate project settings instead of 'System.Reflection.AssemblyDelaySignAttribute'.
        CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics, <errors>BC40060: Delay signing was specified and requires a public key, but no public key was specified.</errors>)
        Assert.True(emitResult.Success)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignInMemory(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)
        Assert.True(emitResult.Success)
        Assert.True(ILValidation.IsStreamFullSigned(outStrm))
    End Sub

    <WorkItem(545720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545720")>
    <WorkItem(530050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530050")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub InvalidAssemblyName(parseOptions As VisualBasicParseOptions)

        Dim il = <![CDATA[
.assembly extern mscorlib { }
.assembly asm1
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 09 2F 5C 3A 2A 3F 27 3C 3E 7C 00 00 ) // .../\:*?'<>|..
}

.class private auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
]]>

        Dim vb = <compilation>
                     <file name="a.vb"><![CDATA[
Public Class Derived
    Inherits Base
End Class
]]>
                     </file>
                 </compilation>

        Dim ilRef = CompileIL(il.Value, prependDefaultHeader:=False)

        Dim comp = CreateCompilationWithMscorlib40AndReferences(vb, {ilRef}, TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        ' NOTE: dev10 reports ERR_FriendAssemblyNameInvalid, but Roslyn won't (DevDiv #15099).
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_InaccessibleSymbol2, "Base").WithArguments("Base", "Friend"))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub DelaySignWithAssemblySignatureKey(parseOptions As VisualBasicParseOptions)
        '//Note that this SignatureKey is some random one that I found in the devdiv build.
        '//It is not related to the other keys we use in these tests.

        '//In the native compiler, when the AssemblySignatureKey attribute is present, and
        '//the binary is configured for delay signing, the contents of the assemblySignatureKey attribute
        '//(rather than the contents of the keyfile or container) are used to compute the size needed to 
        '//reserve in the binary for its signature. Signing using this key is only supported via sn.exe

        Dim other = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDelaySign(True)>
<Assembly: System.Reflection.AssemblySignatureKey("002400000c800000140100000602000000240000525341310008000001000100613399aff18ef1a2c2514a273a42d9042b72321f1757102df9ebada69923e2738406c21e5b801552ab8d200a65a235e001ac9adc25f2d811eb09496a4c6a59d4619589c69f5baf0c4179a47311d92555cd006acc8b5959f2bd6e10e360c34537a1d266da8085856583c85d81da7f3ec01ed9564c58d93d713cd0172c8e23a10f0239b80c96b07736f5d8b022542a4e74251a5f432824318b3539a5a087f8e53d2f135f9ca47f3bb2e10aff0af0849504fb7cea3ff192dc8de0edad64c68efde34c56d302ad55fd6e80f302d5efcdeae953658d3452561b5f36c542efdbdd9f888538d374cef106acf7d93a4445c3c73cd911f0571aaf3d54da12b11ddec375b3", "a5a866e1ee186f807668209f3b11236ace5e21f117803a3143abb126dd035d7d2f876b6938aaf2ee3414d5420d753621400db44a49c486ce134300a2106adb6bdb433590fef8ad5c43cba82290dc49530effd86523d9483c00f458af46890036b0e2c61d077d7fbac467a506eba29e467a87198b053c749aa2a4d2840c784e6d")>
Public Class C
 Friend Sub Goo()
    End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626}, TestOptions.SigningReleaseDll.WithDelaySign(True).WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        ' confirm header has expected SN signature size
        Dim peHeaders = New PEHeaders(other.EmitToStream())
        Assert.Equal(256, peHeaders.CorHeader.StrongNameSignatureDirectory.Size)
        Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags)
    End Sub

    ''' <summary>
    ''' Won't fix (easy to be tested here)
    ''' </summary>
    <WorkItem(529953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529953"), WorkItem(530112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530112")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub DeclareAssemblyKeyNameAndFile_BC41008(parseOptions As VisualBasicParseOptions)

        Dim src = "<Assembly: System.Reflection.AssemblyKeyName(""Key1"")>" & vbCrLf &
                "<Assembly: System.Reflection.AssemblyKeyFile(""" & s_keyPairFile & """)>" & vbCrLf &
              "Public Class C" & vbCrLf &
              "End Class"

        Dim tree = ParseAndVerify(src, parseOptions)
        Dim comp = CreateCompilationWithMscorlib40({tree}, options:=TestOptions.SigningReleaseDll)

        ' Native Compiler:
        'warning BC41008: Use command-line option '/keycontainer' or appropriate project settings instead of 'System.Reflection.AssemblyKeyNameAttribute() '.
        ' <Assembly: System.Reflection.AssemblyKeyName("Key1")>
        '            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        'warning BC41008: Use command-line option '/keyfile' or appropriate project settings instead of 'System.Reflection.AssemblyKeyFileAttribute() '.
        '<Assembly: System.Reflection.AssemblyKeyFile("Key2.snk")>
        '  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        comp.VerifyDiagnostics()
        '   Diagnostic(ERRID.WRN_UseSwitchInsteadOfAttribute, "System.Reflection.AssemblyKeyName(""Key1""").WithArguments("/keycontainer"),
        '   Diagnostic(ERRID.WRN_UseSwitchInsteadOfAttribute, "System.Reflection.AssemblyKeyFile(""Key2.snk""").WithArguments("/keyfile"))

        Dim outStrm = New MemoryStream()
        Dim emitResult = comp.Emit(outStrm)
        Assert.True(emitResult.Success)
    End Sub

    Private Sub ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
        moduleContents As Stream,
        expectedModuleAttr As AttributeDescription,
        parseOptions As VisualBasicParseOptions)
        ' a module doesn't get signed for real. It should have either a keyfile or keycontainer attribute
        ' parked on a typeRef named 'AssemblyAttributesGoHere.' When the module is added to an assembly, the
        ' resulting assembly is signed with the key referred to by the aforementioned attribute.

        Dim success As EmitResult
        Dim tempFile = Temp.CreateFile()
        moduleContents.Position = 0

        Using metadata = ModuleMetadata.CreateFromStream(moduleContents)
            Dim flags = metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags
            ' confirm file does not claim to be signed
            Assert.Equal(0, CInt(flags And CorFlags.StrongNameSigned))

            Dim token As EntityHandle = metadata.Module.GetTypeRef(metadata.Module.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHere")
            Assert.False(token.IsNil)   ' could the magic type ref be located? If not then the attribute's not there.
            Dim attrInfos = metadata.Module.FindTargetAttributes(token, expectedModuleAttr)
            Assert.Equal(1, attrInfos.Count())

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Z
End Class
]]>
    </file>
</compilation>

            ' now that the module checks out, ensure that adding it to a compilation outputting a dll
            ' results in a signed assembly.
            Dim assemblyComp = CreateCompilationWithMscorlib40AndReferences(
                source,
                {metadata.GetReference()},
                TestOptions.SigningReleaseDll,
                parseOptions)

            Using finalStrm = tempFile.Open()
                success = assemblyComp.Emit(finalStrm)
            End Using
        End Using

        success.Diagnostics.Verify()

        Assert.True(success.Success)
        AssertFileIsSigned(tempFile)
    End Sub

    Private Shared Sub AssertFileIsSigned(file As TempFile)
        Using peStream = New FileStream(file.Path, FileMode.Open)
            Assert.True(ILValidation.IsStreamFullSigned(peStream))
        End Using
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyContainerAttr(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(source, options:=TestOptions.SigningReleaseModule)

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
            outStrm,
            AttributeDescription.AssemblyKeyNameAttribute,
            parseOptions)
    End Sub

    <WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")>
    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyContainerCmdLine(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb">
Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(source, options:=TestOptions.SigningReleaseModule.WithCryptoKeyContainer("roslynTestContainer"))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
            outStrm,
            AttributeDescription.AssemblyKeyNameAttribute,
            parseOptions)
    End Sub

    <WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")>
    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyContainerCmdLine_1(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>

Public Class C
End Class
    ]]></file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(
            source,
            options:=TestOptions.SigningReleaseModule.WithCryptoKeyContainer("roslynTestContainer"))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
            outStrm,
            AttributeDescription.AssemblyKeyNameAttribute,
            parseOptions)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyFileAttr(parseOptions As VisualBasicParseOptions)
        Dim x = s_keyPairFile

        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>Assembly: System.Reflection.AssemblyKeyFile("<%= x %>")>

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(
            source,
            options:=TestOptions.SigningReleaseModule)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
            other.EmitToStream(),
            AttributeDescription.AssemblyKeyFileAttribute,
            parseOptions)
    End Sub

    <WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyContainerCmdLine_2(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("bogus")>

Public Class C
End Class
    ]]></file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(source, options:=TestOptions.SigningReleaseModule.WithCryptoKeyContainer("roslynTestContainer"), parseOptions:=parseOptions)

        AssertTheseDiagnostics(other,
<expected>
BC37207: Attribute 'System.Reflection.AssemblyKeyNameAttribute' given in a source file conflicts with option 'CryptoKeyContainer'.
</expected>)
    End Sub

    <WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyFileCmdLine(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb">
Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(
            source,
            options:=TestOptions.SigningReleaseModule.WithCryptoKeyFile(s_keyPairFile))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
            outStrm,
            AttributeDescription.AssemblyKeyFileAttribute,
            parseOptions)
    End Sub

    <WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyFileCmdLine_1(parseOptions As VisualBasicParseOptions)
        Dim x = s_keyPairFile
        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>assembly: System.Reflection.AssemblyKeyFile("<%= x %>")>        

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(
            source,
            options:=TestOptions.SigningReleaseModule.WithCryptoKeyFile(s_keyPairFile))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
            outStrm,
            AttributeDescription.AssemblyKeyFileAttribute,
            parseOptions)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub SignModuleKeyFileCmdLine_2(parseOptions As VisualBasicParseOptions)
        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>assembly: System.Reflection.AssemblyKeyFile("bogus")>        

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib40(source, options:=TestOptions.SigningReleaseModule.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        AssertTheseDiagnostics(other,
<expected>
BC37207: Attribute 'System.Reflection.AssemblyKeyFileAttribute' given in a source file conflicts with option 'CryptoKeyFile'.
</expected>)
    End Sub

    <WorkItem(529779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529779")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub Bug529779_1(parseOptions As VisualBasicParseOptions)

        Dim unsigned As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C1
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
    Dim x as New System.Guid()
    System.Console.WriteLine(x)
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        CompileAndVerify(other.WithReferences({other.References(0), New VisualBasicCompilationReference(unsigned)})).VerifyDiagnostics()

        CompileAndVerify(other.WithReferences({other.References(0), MetadataReference.CreateFromImage(unsigned.EmitToArray)})).VerifyDiagnostics()
    End Sub

    <WorkItem(529779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529779")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub Bug529779_2(parseOptions As VisualBasicParseOptions)

        Dim unsigned As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation name="Unsigned">
    <file name="a.vb"><![CDATA[
Public Class C1
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
    Dim x as New C1()
    System.Console.WriteLine(x)
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Dim comps = {other.WithReferences({other.References(0), New VisualBasicCompilationReference(unsigned)}),
                     other.WithReferences({other.References(0), MetadataReference.CreateFromImage(unsigned.EmitToArray)})}

        For Each comp In comps
            Dim outStrm = New MemoryStream()
            Dim emitResult = comp.Emit(outStrm)

            ' Dev12 reports an error
            Assert.True(emitResult.Success)

            AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC41997: Referenced assembly 'Unsigned, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' does not have a strong name.
</expected>)
        Next
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub AssemblySignatureKeyAttribute_1(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Dim peHeaders = New PEHeaders(other.EmitToStream())
        Assert.Equal(CorFlags.StrongNameSigned, peHeaders.CorHeader.Flags And CorFlags.StrongNameSigned)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub AssemblySignatureKeyAttribute_2(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC37209: Invalid signature public key specified in AssemblySignatureKeyAttribute.
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
    End Sub

    <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub AssemblySignatureKeyAttribute_3(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"FFFFbc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile), parseOptions:=parseOptions)

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        '        AssertTheseDiagnostics(emitResult.Diagnostics,
        '<expected>
        'BC36980: Error extracting public key from file '<%= KeyPairFile %>': Invalid countersignature specified in AssemblySignatureKeyAttribute. (Exception from HRESULT: 0x80131423)
        '</expected>)
        Dim err = emitResult.Diagnostics.Single()

        Assert.Equal(ERRID.ERR_PublicKeyFileFailure, err.Code)
        Assert.Equal(2, err.Arguments.Count)
        Assert.Equal(s_keyPairFile, DirectCast(err.Arguments(0), String))
        Dim errorText = DirectCast(err.Arguments(1), String)
        Assert.True(
            errorText.Contains("HRESULT") AndAlso
            errorText.Contains("0x80131423"))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub AssemblySignatureKeyAttribute_4(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(True), parseOptions:=parseOptions)

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC37209: Invalid signature public key specified in AssemblySignatureKeyAttribute.
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub AssemblySignatureKeyAttribute_5(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"FFFFbc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(True), parseOptions:=parseOptions)

        CompileAndVerify(other)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub AssemblySignatureKeyAttribute_6(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
Nothing,
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(True), parseOptions:=parseOptions)

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC37209: Invalid signature public key specified in AssemblySignatureKeyAttribute.
Nothing,
~~~~~~~
</expected>)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub AssemblySignatureKeyAttribute_7(parseOptions As VisualBasicParseOptions)
        Dim other As VisualBasicCompilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
Nothing)>

Public Class C
 Friend Sub Goo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(True), parseOptions:=parseOptions)

        CompileAndVerify(other)
    End Sub

#End Region

    Public Sub PublicSignCore(options As VisualBasicCompilationOptions, parseOptions As VisualBasicParseOptions)
        Dim source =
            <compilation>
                <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
                </file>
            </compilation>

        Dim compilation = CreateCompilationWithMscorlib40(source, options:=options, parseOptions:=parseOptions)
        PublicSignCore(compilation)
    End Sub

    Public Sub PublicSignCore(compilation As Compilation, Optional assertNoDiagnostics As Boolean = True)
        Assert.True(compilation.Options.PublicSign)
        Assert.Null(compilation.Options.DelaySign)

        Dim stream As New MemoryStream()
        Dim emitResult = compilation.Emit(stream)
        Assert.True(emitResult.Success)
        If assertNoDiagnostics Then
            Assert.True(emitResult.Diagnostics.IsEmpty)
        End If

        stream.Position = 0

        Using reader As New PEReader(stream)
            Assert.True(reader.HasMetadata)
            Dim flags = reader.PEHeaders.CorHeader.Flags
            Assert.True(flags.HasFlag(CorFlags.StrongNameSigned))
        End Using
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_NoKey(parseOptions As VisualBasicParseOptions)
        Dim options = TestOptions.ReleaseDll.WithPublicSign(True)
        Dim comp = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
                </file>
            </compilation>, options:=options, parseOptions:=parseOptions)

        AssertTheseDiagnostics(comp,
<errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
</errors>)
        Assert.True(comp.Options.PublicSign)
        Assert.True(comp.Assembly.PublicKey.IsDefaultOrEmpty)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub KeyFileFromAttributes_PublicSign(parseOptions As VisualBasicParseOptions)
        Dim source = <compilation>
                         <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyKeyFile("test.snk")>
Public Class C
End Class
]]>
                         </file>
                     </compilation>
        Dim c = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithPublicSign(True), parseOptions:=parseOptions)
        AssertTheseDiagnostics(c,
                               <errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
BC42379: Attribute 'System.Reflection.AssemblyKeyFileAttribute' is ignored when public signing is specified.
                               </errors>)

        Assert.True(c.Options.PublicSign)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub KeyContainerFromAttributes_PublicSign(parseOptions As VisualBasicParseOptions)
        Dim source = <compilation>
                         <file name="a.vb"><![CDATA[
<assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class C
End Class
]]>
                         </file>
                     </compilation>
        Dim c = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithPublicSign(True), parseOptions:=parseOptions)
        AssertTheseDiagnostics(c,
                               <errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
BC42379: Attribute 'System.Reflection.AssemblyKeyNameAttribute' is ignored when public signing is specified.
                               </errors>)

        Assert.True(c.Options.PublicSign)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_FromKeyFileNoStrongNameProvider(parseOptions As VisualBasicParseOptions)
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey)
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True)
        PublicSignCore(options, parseOptions)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_FromPublicKeyFileNoStrongNameProvider(parseOptions As VisualBasicParseOptions)
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snPublicKey)
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True)
        PublicSignCore(options, parseOptions)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_FromKeyFileAndStrongNameProvider(parseOptions As VisualBasicParseOptions)
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey2)
        Dim options = TestOptions.SigningReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True)
        PublicSignCore(options, parseOptions)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_FromKeyFileAndNoStrongNameProvider(parseOptions As VisualBasicParseOptions)
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snPublicKey2)
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True)
        PublicSignCore(options, parseOptions)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_KeyContainerOnly(parseOptions As VisualBasicParseOptions)
        Dim source =
            <compilation>
                <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
                </file>
            </compilation>
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyContainer("testContainer").WithPublicSign(True)
        Dim compilation = CreateCompilationWithMscorlib40(source, options:=options, parseOptions:=parseOptions)
        AssertTheseDiagnostics(compilation, <errors>
BC2046: Compilation options 'PublicSign' and 'CryptoKeyContainer' can't both be specified at the same time.
BC37254: Public sign was specified and requires a public key, but no public key was specified
                                            </errors>)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_IgnoreSourceAttributes(parseOptions As VisualBasicParseOptions)
        Dim source =
            <compilation>
                <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")> 
<Assembly: System.Reflection.AssemblyKeyFile("some file")> 

Public Class C
End Class
]]>
                </file>
            </compilation>
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey)
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True)
        Dim compilation = CreateCompilationWithMscorlib40(source, options:=options, parseOptions:=parseOptions)

        AssertTheseDiagnostics(compilation,
                               <errors>
BC42379: Attribute 'System.Reflection.AssemblyKeyFileAttribute' is ignored when public signing is specified.
BC42379: Attribute 'System.Reflection.AssemblyKeyNameAttribute' is ignored when public signing is specified.
                               </errors>)

        PublicSignCore(compilation, assertNoDiagnostics:=False)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSign_DelaySignAttribute(parseOptions As VisualBasicParseOptions)
        Dim source =
            <compilation>
                <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDelaySign(True)>
Public Class C
End Class
]]>
                </file>
            </compilation>
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey)
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True)
        Dim comp = CreateCompilationWithMscorlib40(source, options:=options, parseOptions:=parseOptions)

        AssertTheseDiagnostics(comp,
<errors>
BC37207: Attribute 'System.Reflection.AssemblyDelaySignAttribute' given in a source file conflicts with option 'PublicSign'.
</errors>)
        Assert.True(comp.Options.PublicSign)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSignAndDelaySign(parseOptions As VisualBasicParseOptions)
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey)
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True).WithDelaySign(True)

        Dim comp = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
                </file>
            </compilation>,
            options:=options, parseOptions:=parseOptions)

        AssertTheseDiagnostics(comp,
<errors>
BC2046: Compilation options 'PublicSign' and 'DelaySign' can't both be specified at the same time.
</errors>)

        Assert.True(comp.Options.PublicSign)
        Assert.True(comp.Options.DelaySign)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub PublicSignAndDelaySignFalse(parseOptions As VisualBasicParseOptions)
        Dim snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey)
        Dim options = TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(True).WithDelaySign(False)

        Dim comp = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
                </file>
            </compilation>,
            options:=options,
            parseOptions:=parseOptions)

        AssertTheseDiagnostics(comp)

        Assert.True(comp.Options.PublicSign)
        Assert.False(comp.Options.DelaySign)
    End Sub

    <WorkItem(769840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769840")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub Bug769840(parseOptions As VisualBasicParseOptions)
        Dim ca = CreateCompilationWithMscorlib40(
<compilation name="Bug769840_A">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Bug769840_B, PublicKey=0024000004800000940000000602000000240000525341310004000001000100458a131798af87d9e33088a3ab1c6101cbd462760f023d4f41d97f691033649e60b42001e94f4d79386b5e087b0a044c54b7afce151b3ad19b33b332b83087e3b8b022f45b5e4ff9b9a1077b0572ff0679ce38f884c7bd3d9b4090e4a7ee086b7dd292dc20f81a3b1b8a0b67ee77023131e59831c709c81d11c6856669974cc4")>

Friend Class A
    Public Value As Integer = 3
End Class
]]></file>
</compilation>, options:=TestOptions.SigningReleaseDll, parseOptions:=parseOptions)

        CompileAndVerify(ca)

        Dim cb = CreateCompilationWithMscorlib40AndReferences(
<compilation name="Bug769840_B">
    <file name="a.vb"><![CDATA[
Friend Class B
    Public Function GetA() As A
        Return New A()
    End Function
End Class
]]></file>
</compilation>, {New VisualBasicCompilationReference(ca)}, options:=TestOptions.SigningReleaseModule, parseOptions:=parseOptions)

        CompileAndVerify(cb, verify:=Verification.Fails).Diagnostics.Verify()
    End Sub

    <WorkItem(1072350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072350")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub Bug1072350(parseOptions As VisualBasicParseOptions)
        Dim sourceA As XElement =
<compilation name="ClassLibrary2">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("X ")>
Friend Class A
    Friend Shared I As Integer = 42
End Class]]>
    </file>
</compilation>

        Dim sourceB As XElement =
<compilation name="X">
    <file name="b.vb"><![CDATA[
Class B
    Shared Sub Main()
        System.Console.Write(A.I)
    End Sub
End Class]]>
    </file>
</compilation>

        Dim ca = CreateCompilationWithMscorlib40(sourceA, options:=TestOptions.ReleaseDll, parseOptions:=parseOptions)
        CompileAndVerify(ca)

        Dim cb = CreateCompilationWithMscorlib40(sourceB, options:=TestOptions.ReleaseExe, references:={New VisualBasicCompilationReference(ca)}, parseOptions:=parseOptions)
        CompileAndVerify(cb, expectedOutput:="42").Diagnostics.Verify()
    End Sub

    <WorkItem(1072339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072339")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub Bug1072339(parseOptions As VisualBasicParseOptions)
        Dim sourceA As XElement =
<compilation name="ClassLibrary2">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("x")>
Friend Class A
    Friend Shared I As Integer = 42
End Class]]>
    </file>
</compilation>

        Dim sourceB As XElement =
<compilation name="x">
    <file name="b.vb"><![CDATA[
Class B
    Shared Sub Main()
        System.Console.Write(A.I)
    End Sub
End Class]]>
    </file>
</compilation>

        Dim ca = CreateCompilationWithMscorlib40(sourceA, options:=TestOptions.ReleaseDll, parseOptions:=parseOptions)
        CompileAndVerify(ca)

        Dim cb = CreateCompilationWithMscorlib40(sourceB, options:=TestOptions.ReleaseExe, references:={New VisualBasicCompilationReference(ca)}, parseOptions:=parseOptions)
        CompileAndVerify(cb, expectedOutput:="42").Diagnostics.Verify()
    End Sub

    <WorkItem(1095618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1095618")>
    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    Public Sub Bug1095618(parseOptions As VisualBasicParseOptions)
        Dim source As XElement =
<compilation name="a">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000")>
    ]]></file>
</compilation>

        CreateCompilationWithMscorlib40(source, parseOptions:=parseOptions).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_FriendAssemblyNameInvalid, "Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000"")").WithArguments("System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000").WithLocation(1, 2))
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
    Public Sub ConsistentErrorMessageWhenProvidingNullKeyFile(parseOptions As VisualBasicParseOptions)
        Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoKeyFile:=Nothing)
        Dim compilation = CreateCompilationWithMscorlib40(String.Empty, options:=options, parseOptions:=parseOptions).VerifyEmitDiagnostics()

        VerifySigned(compilation, expectedToBeSigned:=False)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
    Public Sub ConsistentErrorMessageWhenProvidingEmptyKeyFile(parseOptions As VisualBasicParseOptions)
        Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoKeyFile:=String.Empty)
        Dim compilation = CreateCompilationWithMscorlib40(String.Empty, options:=options, parseOptions:=parseOptions).VerifyEmitDiagnostics()

        VerifySigned(compilation, expectedToBeSigned:=False)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
    Public Sub ConsistentErrorMessageWhenProvidingNullKeyFile_PublicSign(parseOptions As VisualBasicParseOptions)
        Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoKeyFile:=Nothing, publicSign:=True)
        Dim compilation = CreateCompilationWithMscorlib40(String.Empty, options:=options, parseOptions:=parseOptions)

        CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
</errors>)
    End Sub

    <Theory>
    <MemberData(NameOf(AllProviderParseOptions))>
    <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
    Public Sub ConsistentErrorMessageWhenProvidingEmptyKeyFile_PublicSign(parseOptions As VisualBasicParseOptions)
        Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoKeyFile:=String.Empty, publicSign:=True)
        Dim compilation = CreateCompilationWithMscorlib40(String.Empty, options:=options, parseOptions:=parseOptions)

        CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
</errors>)
    End Sub

End Class
