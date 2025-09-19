// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.UnitTests;

internal static class CodeSnippets
{
    public const string FormattableStringType = """
        namespace System
        {
            public abstract class FormattableString : IFormattable
            {
                public abstract string Format { get; }
                public abstract object[] GetArguments();
                public abstract int ArgumentCount { get; }
                public abstract object GetArgument(int index);
                public abstract string ToString(IFormatProvider formatProvider);

                string IFormattable.ToString(string ignored, IFormatProvider formatProvider) => ToString(formatProvider);
                public static string Invariant(FormattableString formattable) => formattable.ToString(Globalization.CultureInfo.InvariantCulture);
                public override string ToString() => ToString(Globalization.CultureInfo.CurrentCulture);
            }
        }

        namespace System.Runtime.CompilerServices
        {
            public static class FormattableStringFactory
            {
                public static FormattableString Create(string format, params object[] arguments) => new ConcreteFormattableString(format, arguments);

                private sealed class ConcreteFormattableString : FormattableString
                {
                    private readonly string _format;
                    private readonly object[] _arguments;

                    internal ConcreteFormattableString(string format, object[] arguments)
                    {
                        _format = format;
                        _arguments = arguments;
                    }

                    public override string Format => _format;
                    public override object[] GetArguments() => _arguments;
                    public override int ArgumentCount => _arguments.Length;
                    public override object GetArgument(int index) => _arguments[index];
                    public override string ToString(IFormatProvider formatProvider) => string.Format(formatProvider, _format, _arguments);
                }
            }
        }
        """;

    public const string VBFormattableStringType = """
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
        """;
}
