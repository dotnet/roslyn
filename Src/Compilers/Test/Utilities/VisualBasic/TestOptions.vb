' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' TODO: Strict Module
Public Class TestOptions
    Public Shared ReadOnly Script As New VBParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Interactive As New VBParseOptions(kind:=SourceCodeKind.Interactive)
    Public Shared ReadOnly Regular As New VBParseOptions(kind:=SourceCodeKind.Regular)

    Public Shared ReadOnly ReleaseDll As New VBCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseExe As New VBCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ExperimentalReleaseExe As New VBCompilationOptions(OutputKind.ConsoleApplication,
                                                                                       optimizationLevel:=OptimizationLevel.Release,
                                                                                       parseOptions:=New VBParseOptions(kind:=SourceCodeKind.Regular,
                                                                                                                                 languageVersion:=LanguageVersion.Experimental))

    Public Shared ReadOnly DebugDll As New VBCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Debug)
    Public Shared ReadOnly DebugExe As New VBCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Debug)

    Public Shared ReadOnly ReleaseModule As New VBCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseWinMD As New VBCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Release)
End Class
