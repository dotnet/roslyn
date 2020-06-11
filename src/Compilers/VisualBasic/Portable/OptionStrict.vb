' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents the state of Option Strict checking.
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum OptionStrict As Byte
        ''' <summary>
        ''' Option Strict is Off. No Option Strict checks are in effect.
        ''' </summary>
        Off = 0

        ''' <summary>
        ''' The Option Strict checks generate warnings. (Note that other
        ''' compile options may hide these warnings, or turn them into errors.)
        ''' </summary>
        Custom = 1

        ''' <summary>
        ''' Option Strict is On. All Option Strict checks are in effect and produce errors.
        ''' </summary>
        [On] = 2
    End Enum

    Friend Module OptionStrictEnumBounds
        <Extension()>
        Friend Function IsValid(value As OptionStrict) As Boolean
            Return value >= OptionStrict.Off AndAlso value <= OptionStrict.[On]
        End Function
    End Module
End Namespace
