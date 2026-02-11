' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Supported Visual Basic language versions.
    ''' </summary>
    Public Enum LanguageVersion
        [Default] = 0
        VisualBasic9 = 9
        VisualBasic10 = 10
        VisualBasic11 = 11
        VisualBasic12 = 12
        VisualBasic14 = 14
        VisualBasic15 = 15
        VisualBasic15_3 = 1503
        VisualBasic15_5 = 1505
        VisualBasic16 = 1600
        VisualBasic16_9 = 1609
        VisualBasic17_13 = 1713

        Latest = Integer.MaxValue
    End Enum

    Friend Module LanguageVersionEnumBounds
        <Extension>
        Friend Function IsValid(value As LanguageVersion) As Boolean

            Select Case value
                Case LanguageVersion.VisualBasic9,
                    LanguageVersion.VisualBasic10,
                    LanguageVersion.VisualBasic11,
                    LanguageVersion.VisualBasic12,
                    LanguageVersion.VisualBasic14,
                    LanguageVersion.VisualBasic15,
                    LanguageVersion.VisualBasic15_3,
                    LanguageVersion.VisualBasic15_5,
                    LanguageVersion.VisualBasic16,
                    LanguageVersion.VisualBasic16_9,
                    LanguageVersion.VisualBasic17_13

                    Return True
            End Select

            Return False
        End Function

        <Extension>
        Friend Function GetErrorName(value As LanguageVersion) As String

            Select Case value
                Case LanguageVersion.VisualBasic9
                    Return "9.0"
                Case LanguageVersion.VisualBasic10
                    Return "10.0"
                Case LanguageVersion.VisualBasic11
                    Return "11.0"
                Case LanguageVersion.VisualBasic12
                    Return "12.0"
                Case LanguageVersion.VisualBasic14
                    Return "14.0"
                Case LanguageVersion.VisualBasic15
                    Return "15.0"
                Case LanguageVersion.VisualBasic15_3
                    Return "15.3"
                Case LanguageVersion.VisualBasic15_5
                    Return "15.5"
                Case LanguageVersion.VisualBasic16
                    Return "16"
                Case LanguageVersion.VisualBasic16_9
                    Return "16.9"
                Case LanguageVersion.VisualBasic17_13
                    Return "17.13"
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(value)
            End Select

        End Function
    End Module

    Public Module LanguageVersionFacts

        ''' <summary>
        ''' Map a language version (such as Default, Latest, Or VisualBasicN) to a specific version (VisualBasicN).
        ''' </summary>
        <Extension>
        Public Function MapSpecifiedToEffectiveVersion(version As LanguageVersion) As LanguageVersion
            Select Case version
                Case LanguageVersion.Latest
                    Return LanguageVersion.VisualBasic17_13
                Case LanguageVersion.Default
                    Return LanguageVersion.VisualBasic17_13
                Case Else
                    Return version
            End Select
        End Function

        Friend ReadOnly Property CurrentVersion As LanguageVersion
            Get
                Return LanguageVersion.VisualBasic17_13
            End Get
        End Property

        ''' <summary>
        ''' Displays the version number in the format understood on the command-line (/langver flag).
        ''' For instance, "9", "15", "latest".
        ''' </summary>
        <Extension>
        Public Function ToDisplayString(version As LanguageVersion) As String
            Select Case version
                Case LanguageVersion.VisualBasic9
                    Return "9"
                Case LanguageVersion.VisualBasic10
                    Return "10"
                Case LanguageVersion.VisualBasic11
                    Return "11"
                Case LanguageVersion.VisualBasic12
                    Return "12"
                Case LanguageVersion.VisualBasic14
                    Return "14"
                Case LanguageVersion.VisualBasic15
                    Return "15"
                Case LanguageVersion.VisualBasic15_3
                    Return "15.3"
                Case LanguageVersion.VisualBasic15_5
                    Return "15.5"
                Case LanguageVersion.VisualBasic16
                    Return "16"
                Case LanguageVersion.VisualBasic16_9
                    Return "16.9"
                Case LanguageVersion.VisualBasic17_13
                    Return "17.13"
                Case LanguageVersion.Default
                    Return "default"
                Case LanguageVersion.Latest
                    Return "latest"
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(version)
            End Select
        End Function

        ''' <summary>
        ''' Parse a LanguageVersion from a string input, as the command-line compiler does.
        ''' </summary>
        Public Function TryParse(version As String, ByRef result As LanguageVersion) As Boolean
            If version Is Nothing Then
                result = LanguageVersion.Default
                Return False
            End If

            Select Case version.ToLowerInvariant()
                Case "9", "9.0"
                    result = LanguageVersion.VisualBasic9
                Case "10", "10.0"
                    result = LanguageVersion.VisualBasic10
                Case "11", "11.0"
                    result = LanguageVersion.VisualBasic11
                Case "12", "12.0"
                    result = LanguageVersion.VisualBasic12
                Case "14", "14.0"
                    result = LanguageVersion.VisualBasic14
                Case "15", "15.0"
                    result = LanguageVersion.VisualBasic15
                Case "15.3"
                    result = LanguageVersion.VisualBasic15_3
                Case "15.5"
                    result = LanguageVersion.VisualBasic15_5
                Case "16", "16.0"
                    result = LanguageVersion.VisualBasic16
                Case "16.9"
                    result = LanguageVersion.VisualBasic16_9
                Case "17.13"
                    result = LanguageVersion.VisualBasic17_13
                Case "default"
                    result = LanguageVersion.Default
                Case "latest"
                    result = LanguageVersion.Latest
                Case Else
                    result = LanguageVersion.Default
                    Return False
            End Select
            Return True
        End Function

        ''' <summary>Inference of tuple element names was added in VB 15.3</summary>
        <Extension>
        Friend Function DisallowInferredTupleElementNames(self As LanguageVersion) As Boolean
            Return self < Syntax.InternalSyntax.Feature.InferredTupleNames.GetLanguageVersion()
        End Function

        <Extension>
        Friend Function AllowNonTrailingNamedArguments(self As LanguageVersion) As Boolean
            Return self >= Syntax.InternalSyntax.Feature.NonTrailingNamedArguments.GetLanguageVersion()
        End Function
    End Module

    Friend Class VisualBasicRequiredLanguageVersion
        Inherits RequiredLanguageVersion

        Friend ReadOnly Property Version As LanguageVersion

        Friend Sub New(version As LanguageVersion)
            Me.Version = version
        End Sub

        Public Overrides Function ToString() As String
            Return Version.ToDisplayString()
        End Function
    End Class
End Namespace
