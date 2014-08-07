' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' TODO: Strict Module
Public Class TestOptions
    Public Shared ReadOnly Script As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Interactive As New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
    Public Shared ReadOnly Regular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)

    Public Shared ReadOnly ReleaseDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)
    Public Shared ReadOnly ReleaseExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)

    Public Shared ReadOnly DebuggableReleaseDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize:=True, debugInformationKind:=DebugInformationKind.Full)
    Public Shared ReadOnly DebuggableReleaseExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimize:=True, debugInformationKind:=DebugInformationKind.Full)

    Public Shared ReadOnly DebugDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize:=False, debugInformationKind:=DebugInformationKind.Full)
    Public Shared ReadOnly DebugExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimize:=False, debugInformationKind:=DebugInformationKind.Full)

    Public Shared ReadOnly ReleaseModule As New VisualBasicCompilationOptions(OutputKind.NetModule, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)
    Public Shared ReadOnly ReleaseWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)
End Class
