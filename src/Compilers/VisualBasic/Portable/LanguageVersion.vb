' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

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
                    LanguageVersion.VisualBasic15

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
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(value)
            End Select

        End Function
    End Module

    Public Module LanguageVersionExtensions

        ''' <summary>
        ''' Map a language version (such as Default, Latest, Or VisualBasicN) to a specific version (VisualBasicN).
        ''' </summary>
        <Extension>
        Public Function MapSpecifiedToEffectiveVersion(version As LanguageVersion) As LanguageVersion
            Select Case version
                Case LanguageVersion.Latest, LanguageVersion.Default
                    Return LanguageVersion.VisualBasic15
                Case Else
                    Return version
            End Select
        End Function

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
                Case LanguageVersion.Default
                    Return "default"
                Case LanguageVersion.Latest
                    Return "latest"
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(version)
            End Select
        End Function
    End Module

    ''' <summary>
    ''' This type is attached to diagnostics for required language version and should only be used
    ''' on such diagnostics, as they are recognized by <see cref="VisualBasicCompilation.GetRequiredLanguageVersion"/>.
    ''' </summary>
    Friend Class RequiredLanguageVersion
        Implements IMessageSerializable

        Friend ReadOnly Property Version As LanguageVersion

        Friend Sub New(version As LanguageVersion)
            Me.Version = version
        End Sub

        Public Overrides Function ToString() As String
            Return Version.ToDisplayString()
        End Function
    End Class
End Namespace
