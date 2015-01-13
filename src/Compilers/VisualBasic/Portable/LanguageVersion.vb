' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Supported Visual Basic language versions.
    ''' </summary>
    Public Enum LanguageVersion
        VisualBasic9 = 9
        VisualBasic10 = 10
        VisualBasic11 = 11
        VisualBasic12 = 12
        VisualBasic14 = 14
    End Enum

    Friend Module LanguageVersionEnumBounds
        <Extension>
        Friend Function IsValid(value As LanguageVersion) As Boolean

            Select Case value
                Case LanguageVersion.VisualBasic9,
                    LanguageVersion.VisualBasic10,
                    LanguageVersion.VisualBasic11,
                    LanguageVersion.VisualBasic12,
                    LanguageVersion.VisualBasic14

                    Return True
            End Select

            Return False
        End Function
    End Module
End Namespace
