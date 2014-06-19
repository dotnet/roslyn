Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text

Namespace Roslyn.Compilers.VisualBasic.Emit

    Friend Class ModulePropertiesForSerialization
        Public FileAlignment As UInteger = 512
        Public TargetRuntimeVersion As String = "v4.0.30319"
        Public Requires64Bits As Boolean
        Public MetadataFormatMajorVersion As Byte = 2
        Public MetadataFormatMinorVersion As Byte
        Public PersistentIdentifier As Guid = Guid.NewGuid()
        Public ILOnly As Boolean = True
        Public Requires32bits As Boolean
        Public TrackDebugData As Boolean
        Public BaseAddress As ULong = &H400000
        Public SizeOfHeapReserve As ULong = &H100000
        Public SizeOfHeapCommit As ULong = &H1000
        Public SizeOfStackReserve As ULong = &H100000
        Public SizeOfStackCommit As ULong = &H1000
    End Class
End Namespace
