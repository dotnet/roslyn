' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports Roslyn.Test.Utilities

Public Class TestOptions
    Public Shared ReadOnly Script As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Regular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)
    Public Shared ReadOnly Regular15_5 As VisualBasicParseOptions = Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5)
    Public Shared ReadOnly Regular16 As VisualBasicParseOptions = Regular.WithLanguageVersion(LanguageVersion.VisualBasic16)
    Public Shared ReadOnly Regular16_9 As VisualBasicParseOptions = Regular.WithLanguageVersion(LanguageVersion.VisualBasic16_9)
    Public Shared ReadOnly RegularLatest As VisualBasicParseOptions = Regular.WithLanguageVersion(LanguageVersion.Latest)
    Public Shared ReadOnly RegularWithLegacyStrongName As VisualBasicParseOptions = Regular.WithFeature("UseLegacyStrongNameProvider")

    Public Shared ReadOnly ReleaseDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release).WithParseOptions(Regular)
    Public Shared ReadOnly ReleaseExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release).WithParseOptions(Regular)

    Public Shared ReadOnly ReleaseDebugDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release).
        WithDebugPlusMode(True).WithParseOptions(Regular)

    Public Shared ReadOnly ReleaseDebugExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release).
        WithDebugPlusMode(True).WithParseOptions(Regular)

    Public Shared ReadOnly DebugDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Debug).WithParseOptions(Regular)
    Public Shared ReadOnly DebugExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Debug).WithParseOptions(Regular)

    Public Shared ReadOnly ReleaseModule As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Release).WithParseOptions(Regular)
    Public Shared ReadOnly ReleaseWinMD As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Release).WithParseOptions(Regular)
    Public Shared ReadOnly DebugWinMD As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Debug).WithParseOptions(Regular)

    Public Shared ReadOnly SigningReleaseDll As VisualBasicCompilationOptions = ReleaseDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider)
    Public Shared ReadOnly SigningReleaseExe As VisualBasicCompilationOptions = ReleaseExe.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider)
    Public Shared ReadOnly SigningDebugDll As VisualBasicCompilationOptions = DebugDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider)
    Public Shared ReadOnly SigningDebugExe As VisualBasicCompilationOptions = DebugExe.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider)
    Public Shared ReadOnly SigningReleaseModule As VisualBasicCompilationOptions = ReleaseModule.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider)
End Class

Friend Module TestOptionExtensions
    <Extension()>
    Public Function WithStrictFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return WithFeature(options, "Strict")
    End Function

    <Extension()>
    Public Function WithFeature(options As VisualBasicParseOptions, feature As String, Optional value As String = "True") As VisualBasicParseOptions
        Return options.WithFeatures(options.Features.Concat(New KeyValuePair(Of String, String)() {New KeyValuePair(Of String, String)(feature, value)}))
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
End Module
