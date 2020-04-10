﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Drawing
Imports Roslyn.Test.Utilities

Friend Class HasValidFonts
    Inherits ExecutionCondition

    Public Overrides ReadOnly Property ShouldSkip As Boolean
        Get
            Try
                Dim result = SystemFonts.DefaultFont
                Return result Is Nothing
            Catch ex As Exception
                ' Motivating issue: https://github.com/dotnet/roslyn/issues/11278
                '
                ' On a small percentage of Jenkins runs we see that fonts are corrupted on the machine.  That causes any
                ' font based test to fail with no reasonable way to recover (other than manually re-installing fonts). 
                '
                ' This at least gives us a mechanism for skipping those tests when fonts are in a bad state.
                Return True
            End Try
        End Get
    End Property

    Public Overrides ReadOnly Property SkipReason As String
        Get
            Return "Fonts are unavailable"
        End Get
    End Property
End Class
