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

        <Extension>
        Friend Function MapSpecifiedToEffectiveVersion(version As LanguageVersion) As LanguageVersion
            Select Case version
                Case LanguageVersion.Latest, LanguageVersion.Default
                    Return LanguageVersion.VisualBasic15
                Case Else
                    Return version
            End Select
        End Function

    End Module
End Namespace
