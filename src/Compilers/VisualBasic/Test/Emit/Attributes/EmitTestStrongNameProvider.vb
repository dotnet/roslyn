' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection.Metadata
Imports System.Security.Cryptography
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.SigningTestHelpers

Partial Public Class InternalsVisibleToAndStrongNameTests
    Inherits BasicTestBase

    Private Class StrongNameProviderWithBadInputStream
        Inherits StrongNameProvider
        Private _underlyingProvider As StrongNameProvider
        Public Property ThrownException As Exception

        Friend Overrides ReadOnly Property FileSystem As StrongNameFileSystem = ThrowingStrongNameFileSystem.Instance

        Public Sub New(underlyingProvider As StrongNameProvider, thrownException As Exception)
            _underlyingProvider = underlyingProvider
            Me.ThrownException = thrownException
        End Sub

        Public Overrides Function Equals(other As Object) As Boolean
            Return Me Is other
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _underlyingProvider.GetHashCode()
        End Function

        Friend Overrides Function CreateKeys(keyFilePath As String, keyContainerName As String, hasCounterSigature As Boolean, messageProvider As CommonMessageProvider) As StrongNameKeys
            Return _underlyingProvider.CreateKeys(keyFilePath, keyContainerName, hasCounterSigature, messageProvider)
        End Function

        Friend Overrides Sub SignFile(keys As StrongNameKeys, filePath As String)
            Throw ThrownException
        End Sub

        Friend Overrides Sub SignBuilder(peWriter As ExtendedPEBuilder, peBlob As BlobBuilder, privkey As RSAParameters)
            Throw ThrownException
        End Sub
    End Class

    <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    Public Sub BadInputStream()
        SigningTestHelpers.InstallKey()
        Dim thrownException = New IOException("This is a test IOException")
        Dim testProvider = New StrongNameProviderWithBadInputStream(DefaultDesktopStrongNameProvider, thrownException)
        Dim options = TestOptions.DebugDll.WithStrongNameProvider(testProvider).WithCryptoKeyContainer("RoslynTestContainer")

        Dim comp = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
Public Class C
    Public Sub M()
    End Sub
End Class
]]>
                </file>
            </compilation>, options:=options)

        comp.Emit(New MemoryStream()).Diagnostics.Verify(
            Diagnostic(ERRID.ERR_PeWritingFailure).WithArguments(testProvider.ThrownException.ToString()).WithLocation(1, 1))

    End Sub

End Class
