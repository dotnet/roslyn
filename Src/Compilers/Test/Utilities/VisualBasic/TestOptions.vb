' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' TODO: Strict Module
Public Class TestOptions
    Public Shared ReadOnly Script As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Interactive As New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
    Public Shared ReadOnly Regular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)

    Public Shared ReadOnly ReleaseDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release)

    Public Shared ReadOnly DebugDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Debug)
    Public Shared ReadOnly DebugExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Debug)

    Public Shared ReadOnly ReleaseModule As New VisualBasicCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Release)
End Class
