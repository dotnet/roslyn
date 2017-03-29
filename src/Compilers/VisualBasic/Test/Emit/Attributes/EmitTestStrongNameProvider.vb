' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests

Partial Public Class InternalsVisibleToAndStrongNameTests
    Inherits BasicTestBase

    Private Class StrongNameProviderWithBadInputStream
        Inherits StrongNameProvider
        Private _underlyingProvider As StrongNameProvider
        Public Property ThrownException As Exception

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

        Friend Overrides Sub SignAssembly(keys As StrongNameKeys, inputStream As Stream, outputStream As Stream)
            _underlyingProvider.SignAssembly(keys, inputStream, outputStream)
        End Sub
    End Class

    <Fact>
    Public Sub BadInputStream()
        Dim testProvider = New StrongNameProviderWithBadInputStream(s_defaultProvider)
        Dim options = TestOptions.DebugDll.WithStrongNameProvider(testProvider).WithCryptoKeyContainer("RoslynTestContainer")

        Dim comp = CreateCompilationWithMscorlib(
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
