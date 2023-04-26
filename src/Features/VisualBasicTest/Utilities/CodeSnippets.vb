' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests
    Friend Module CodeSnippets

        Public Const FormattableStringType = "
Namespace System
    Public MustInherit Class FormattableString
        Implements IFormattable

        Public MustOverride ReadOnly Property Format As String
        Public MustOverride Function GetArguments() As Object()
        Public MustOverride ReadOnly Property ArgumentCount As Integer
        Public MustOverride Function GetArgument(index As Integer) As Object
        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Private Function IFormattable_ToString(ignored As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As string
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function

        Public Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function
    End Class
End Namespace

Namespace System.Runtime.CompilerServices
    Public MustInherit Class FormattableStringFactory
        Public Shared Function Create(format As String, ParamArray arguments As Object()) As FormattableString
            Return new ConcreteFormattableString(format, arguments)
        End Function

        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly _format As String
            Private ReadOnly _arguments As Object()

            Friend Sub New(format As String, arguments As Object())
                _format = format
                _arguments = arguments
            End Sub

            Public Overrides ReadOnly Property Format As String
                Get
                    Return _format
                End Get
            End Property

            Public Overrides Function GetArguments() As Object()
                Return _arguments
            End Function

            Public Overrides ReadOnly Property ArgumentCount As Integer
                Get
                    Return _arguments.Length
                End Get
            End Property

            Public Overrides Function GetArgument(index As Integer) As Object
                Return _arguments(index)
            End Function

            Public Overrides Function ToString(formatProvider As IFormatProvider) As String
                Return String.Format(formatProvider, _format, _arguments)
            End Function
        End Class
    End Class
End Namespace
"

    End Module
End Namespace
