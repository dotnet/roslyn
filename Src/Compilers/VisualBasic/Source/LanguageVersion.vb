' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Supported VB Language Versions
    ''' </summary>
    Public Enum LanguageVersion
        VisualBasic9 = 9
        VisualBasic10 = 10
        VisualBasic11 = 11
    End Enum

    Friend Module LanguageVersionEnumBounds
        <Extension()>
        Friend Function IsValid(value As LanguageVersion) As Boolean
            Return value >= LanguageVersion.VisualBasic9 AndAlso value <= LanguageVersion.VisualBasic11
        End Function
    End Module
End Namespace
