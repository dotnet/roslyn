' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Public Class TestOptions
    Public Shared ReadOnly Script As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Regular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)

    Public Shared ReadOnly RegularWithIOperationFeature As VisualBasicParseOptions = Regular.WithIOperationFeature()

    Public Shared ReadOnly ReleaseDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release).WithExtendedCustomDebugInformation(True)
    Public Shared ReadOnly ReleaseExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release).WithExtendedCustomDebugInformation(True)

    Public Shared ReadOnly ReleaseDebugDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release).
        WithExtendedCustomDebugInformation(True).
        WithDebugPlusMode(True)

    Public Shared ReadOnly ReleaseDebugExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release).
        WithExtendedCustomDebugInformation(True).
        WithDebugPlusMode(True)

    Private Shared ReadOnly s_features As New Dictionary(Of String, String) ' No experimental features to enable at this time
    Public Shared ReadOnly ExperimentalReleaseExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication,
                                                                                       optimizationLevel:=OptimizationLevel.Release,
                                                                                       parseOptions:=New VisualBasicParseOptions(kind:=SourceCodeKind.Regular).WithFeatures(s_features))

    Public Shared ReadOnly DebugDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Debug).WithExtendedCustomDebugInformation(True)
    Public Shared ReadOnly DebugExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Debug).WithExtendedCustomDebugInformation(True)

    Public Shared ReadOnly ReleaseModule As New VisualBasicCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly DebugWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Debug)
End Class

Friend Module TestOptionExtensions
    <Extension()>
    Public Function WithFeature(options As VisualBasicParseOptions, feature As String, value As String) As VisualBasicParseOptions
        Return options.WithFeatures(options.Features.Concat({New KeyValuePair(Of String, String)(feature, value)}))
    End Function

    <Extension()>
    Public Function WithStrictFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return options.WithFeature("Strict", "true")
    End Function

    <Extension()>
    Public Function WithDeterministicFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return options.WithFeature("Deterministic", "true")
    End Function

    <Extension()>
    Public Function WithIOperationFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return options.WithFeature("IOperation", "true")
    End Function
End Module
