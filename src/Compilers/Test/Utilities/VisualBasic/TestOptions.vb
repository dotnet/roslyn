' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Public Class TestOptions
    Public Shared ReadOnly Script As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Interactive As New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
    Public Shared ReadOnly Regular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)

    Public Shared ReadOnly ReleaseDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release).WithExtendedCustomDebugInformation(True)
    Public Shared ReadOnly ReleaseExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release).WithExtendedCustomDebugInformation(True)

    Private Shared ReadOnly s_features As New Dictionary(Of String, String) ' No experimental features to enable at this time
    Public Shared ReadOnly ExperimentalReleaseExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication,
                                                                                       optimizationLevel:=OptimizationLevel.Release,
                                                                                       parseOptions:=New VisualBasicParseOptions(kind:=SourceCodeKind.Regular).WithFeatures(s_features))

    Public Shared ReadOnly DebugDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Debug).WithExtendedCustomDebugInformation(True)
    Public Shared ReadOnly DebugExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Debug).WithExtendedCustomDebugInformation(True)

    Public Shared ReadOnly ReleaseModule As New VisualBasicCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Release)
End Class

Friend Module TestOptionExtensions
    <Extension()>
    Public Function WithStrictMode(options As VisualBasicCompilationOptions) As VisualBasicCompilationOptions
        Return options.WithFeatures(options.Features.Add("strict"))
    End Function

    <Extension()>
    Public Function WithStrictMode(compilation As VisualBasicCompilation) As VisualBasicCompilation
        Return compilation.WithOptions(compilation.Options.WithStrictMode())
    End Function
End Module
