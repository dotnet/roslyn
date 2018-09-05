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

        Friend Overrides ReadOnly Property Capability As SigningCapability = SigningCapability.SignsStream

        Public Sub New(underlyingProvider As StrongNameProvider)
            _underlyingProvider = underlyingProvider
        End Sub


        Public Overrides Function Equals(other As Object) As Boolean
            Return Me Is other
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _underlyingProvider.GetHashCode()
        End Function

        Friend Overrides Function CreateInputStream() As Stream
            ThrownException = New IOException("This is a test IOException")
            Throw ThrownException
        End Function

        Friend Overrides Function CreateKeys(keyFilePath As String, keyContainerName As String, messageProvider As CommonMessageProvider) As StrongNameKeys
            Return _underlyingProvider.CreateKeys(keyFilePath, keyContainerName, messageProvider)
        End Function

        Friend Overrides Sub SignStream(keys As StrongNameKeys, inputStream As Stream, outputStream As Stream)
            _underlyingProvider.SignStream(keys, inputStream, outputStream)
        End Sub

        Friend Overrides Sub SignPeBuilder(peWriter As ExtendedPEBuilder, peBlob As BlobBuilder, privkey As RSAParameters)
            Throw ThrownException
        End Sub
    End Class

    <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    Public Sub BadInputStream()
        SigningTestHelpers.InstallKey()
        Dim testProvider = New StrongNameProviderWithBadInputStream(s_defaultDesktopProvider)
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
