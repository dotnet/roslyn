// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
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
    }
}
