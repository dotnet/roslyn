' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

Public Class TestOptions
    Shared Sub New()
        ReleaseDebugDll = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release).WithDebugPlusMode(True)
        ReleaseDebugExe = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release).WithDebugPlusMode(True)
    End Sub

    Public Shared ReadOnly ReleaseDebugDll As VisualBasicCompilationOptions
    Public Shared ReadOnly ReleaseDebugExe As  VisualBasicCompilationOptions
    Public Shared ReadOnly ReleaseDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseModule As New VisualBasicCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Release)

    Public Shared ReadOnly DebugWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Debug)
    Public Shared ReadOnly DebugDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Debug)
    Public Shared ReadOnly DebugExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Debug)

    Public Shared ReadOnly Script As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Regular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)

    Public Shared ReadOnly RegularWithIOperationFeature As VisualBasicParseOptions = Regular.WithIOperationFeature()

End Class

Friend Module TestOptionExtensions
    <Extension()>
    Public Function WithStrictFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return options.WithFeatures(options.Features.Concat(New KeyValuePair(Of String, String)() {New KeyValuePair(Of String, String)("Strict", "true")}))
    End Function

    <Extension()>
    Friend Function WithExperimental(options As VisualBasicParseOptions, ParamArray features As Feature()) As VisualBasicParseOptions
        If features.Length = 0 Then
            Throw New InvalidOperationException("Need at least one feature to enable")
        End If

        Dim list As New List(Of KeyValuePair(Of String, String))
        For Each feature In features
            Dim flagName = feature.GetFeatureFlag()
            If flagName Is Nothing Then
                Throw New InvalidOperationException($"{feature} is not an experimental feature")
            End If

            list.Add(New KeyValuePair(Of String, String)(flagName, "True"))
        Next

        Return options.WithFeatures(options.Features.Concat(list))
    End Function

    <Extension()>
    Public Function WithIOperationFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return options.WithFeatures(options.Features.Concat(New KeyValuePair(Of String, String)() {New KeyValuePair(Of String, String)("IOperation", "true")}))
    End Function
End Module
