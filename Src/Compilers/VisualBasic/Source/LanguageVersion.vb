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
        Experimental = Integer.MaxValue
    End Enum

    Friend Module LanguageVersionEnumBounds
        <Extension>
        Friend Function IsValid(value As LanguageVersion) As Boolean
            Return (value >= LanguageVersion.VisualBasic9 AndAlso value <= LanguageVersion.VisualBasic12) OrElse
                   value = LanguageVersion.Experimental
        End Function
    End Module
End Namespace
