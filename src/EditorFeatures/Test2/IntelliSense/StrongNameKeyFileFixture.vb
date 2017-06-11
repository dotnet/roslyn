Imports System.IO

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class StrongNameKeyFileFixture
        Implements IDisposable

        Private ReadOnly StrongKeyFileValue As String

        Public Sub New()
            StrongKeyFileValue = Path.GetTempFileName()
            Dim keyFileContent = Convert.FromBase64String("
BwIAAAAkAABSU0EyAAQAAAEAAQDFwm1f+PWbR6zFXU/rXBkAkxcTflWjTKKhB6wps7rcaHDYWMsv
vwpxoEyvdJ8FF9wxqJuFsATwMO0+eFu0UJBJloL7Cyp44CqblN4ed44GEWSNCSXzwebHbQmfZop5
sYaJQKIKSMdpX/z7df4JRmkqqN7a/nJ+PLwH9k1kbS729Z+yZc8NsXysHla8QFo6nHeb8MPncf2y
qbkWOd/f7c25Oip36YQI8GfME+664yn+PLge2PvJioHQ4S6hKacGVf0blJDlYFDm21c7WxsB4Ln9
+nTboUFX+3jUxhBuZ+574hazkPzrdFleDvRqh78lwiTalLbIXHHthdYmd07pSI34I6DvEwwMEJHw
tn3rrWx4Rsd0gxtyGlNDgdyAs2sFpo7b2MDGRkgjxw5159e+wajWTAe7KBKaEiTCQq3HpngyKKVN
dC5Jfr7SCyz3M/do+rU9xLUxl1Bv12zuOVKNflIWgG/C+ofMK3/QfD0dPnrkjeiPohs0NsHtZjF3
oeCWysHnRXN3CV/udwOKjofYQc9fyPB2ilmxM2Jwsvd7hEHFpUBgMT6R28vosIxd5neNFAIan85i
Y5ghinxfxvK1u0wWSfyovuFabD4Ez1Ez6UqlgL1b9sPLoPqV8SYj1TASPOvdu5fqe8bzlgXILSZB
xiDimS2uguQQ5qX4kDphCt8judqpxTZKYcTKuKHYFrjzOwmkREl1ve4XfHCZIhhUMMDpkvQG351F
xHe4HK9zaRZZXWf2uqzcnuo5LTzBHtLLHhY=")
            Using stream = File.OpenWrite(StrongKeyFileValue)
                stream.Write(keyFileContent, 0, keyFileContent.Length)
            End Using
        End Sub

        Public ReadOnly Property StrongKeyFile As String
            Get
                Return StrongKeyFileValue
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            If String.IsNullOrEmpty(StrongKeyFile) Then
                Return
            Else
                File.Delete(StrongKeyFile)
            End If
        End Sub

    End Class
End Namespace