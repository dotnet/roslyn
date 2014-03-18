// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class DiagnosticDescription
    {
        public static readonly DiagnosticDescription[] None = new DiagnosticDescription[0];
        public static readonly DiagnosticDescription[] Any = null;

        // common fields for all DiagnosticDescriptions
        private readonly object code;
        private readonly bool isWarningAsError;
        private readonly string squiggledText;
        private readonly object[] arguments;
        private readonly LinePosition? startPosition; // May not have a value only in the case that we're constructed via factories
        private bool showPosition; // show start position in ToString if comparison fails
        private readonly bool argumentOrderDoesNotMatter;
        private readonly Type errorCodeType;
        private readonly bool ignoreArgumentsWhenComparing;

        // fields for DiagnosticDescriptions constructed via factories
        private readonly Func<SyntaxNode, bool> syntaxPredicate;
        private bool showPredicate; // show predicate in ToString if comparison fails

        // fields for DiagnosticDescriptions constructed from Diagnostics
        private readonly Location location;

        private IEnumerable<string> argumentsAsStrings;
        private IEnumerable<string> GetArgumentsAsStrings()
        {
            if (argumentsAsStrings == null)
            {
                // We'll use IFormattable here, because it is more explicit than just calling .ToString()
                // (and is closer to what the compiler actually does when displaying error messages)
                argumentsAsStrings = arguments.Select(o => String.Format("{0}", o));
            }
            return argumentsAsStrings;
        }

        public DiagnosticDescription(
            object code, 
            bool isWarningAsError,
            string squiggledText,
            object[] arguments, 
            LinePosition? startLocation,
            Func<SyntaxNode, bool> syntaxNodePredicate, 
            bool argumentOrderDoesNotMatter,
            Type errorCodeType = null)
        {
            this.code = code;
            this.isWarningAsError = isWarningAsError;
            this.squiggledText = squiggledText;
            this.arguments = arguments;
            startPosition = startLocation;
            syntaxPredicate = syntaxNodePredicate;
            this.argumentOrderDoesNotMatter = argumentOrderDoesNotMatter;
            this.errorCodeType = errorCodeType ?? code.GetType();
        }

        public DiagnosticDescription(
            object code,
            string squiggledText,
            object[] arguments,
            LinePosition? startLocation,
            Func<SyntaxNode, bool> syntaxNodePredicate,
            bool argumentOrderDoesNotMatter,
            Type errorCodeType = null)
        {
            this.code = code;
            this.isWarningAsError = false;
            this.squiggledText = squiggledText;
            this.arguments = arguments;
            startPosition = startLocation;
            syntaxPredicate = syntaxNodePredicate;
            this.argumentOrderDoesNotMatter = argumentOrderDoesNotMatter;
            this.errorCodeType = errorCodeType ?? code.GetType();
        }

        internal DiagnosticDescription(Diagnostic d, bool errorCodeOnly, bool showPosition = false)
        {
            this.code = d.Code;
            this.isWarningAsError = d.IsWarningAsError;
            this.location = d.Location;

            DiagnosticWithInfo dinfo = null;
            if (d.Code == 0)
            {
                code = d.Id;
                errorCodeType = typeof(string);
            }
            else
            {
                dinfo = d as DiagnosticWithInfo;
                if (dinfo == null)
                {
                    code = d.Code;
                    errorCodeType = typeof(int);
                }
                else
                {
                    errorCodeType = dinfo.Info.MessageProvider.ErrorCodeType;
                    code = d.Code;
                }
            }

            this.ignoreArgumentsWhenComparing = errorCodeOnly;
            this.showPosition = showPosition;

            if (!this.ignoreArgumentsWhenComparing)
            {
                if (this.location.IsInSource)
                {
                    // we don't just want to do SyntaxNode.GetText(), because getting the text via the SourceTree validates the public API
                    this.squiggledText = this.location.SourceTree.GetText().ToString(this.location.SourceSpan);
                }

                if (dinfo != null)
                {
                    arguments = dinfo.Info.Arguments;
                }
                else
                {
                    var args = d.Arguments;
                    if (args == null || args.Count == 0)
                    {
                        arguments = null;
                    }
                    else
                    {
                        arguments = d.Arguments.ToArray();
                    }
                }

                if (this.arguments != null && this.arguments.Length == 0)
                {
                    this.arguments = null;
                }
            }

            this.startPosition = this.location.GetMappedLineSpan().StartLinePosition;
        }

        public DiagnosticDescription WithArguments(params string[] arguments)
        {
            return new DiagnosticDescription(code, isWarningAsError, squiggledText, arguments, startPosition, syntaxPredicate, false, errorCodeType);
        }

        public DiagnosticDescription WithArgumentsAnyOrder(params string[] arguments)
        {
            return new DiagnosticDescription(code, isWarningAsError, squiggledText, arguments, startPosition, syntaxPredicate, true, errorCodeType);
        }

        public DiagnosticDescription WithWarningAsError(bool isWarningAsError)
        {
            return new DiagnosticDescription(code, isWarningAsError, squiggledText, arguments, startPosition, syntaxPredicate, true, errorCodeType);
        }

        /// <summary>
        /// Specialized syntaxPredicate that can be used to verify the start of the squiggled Span
        /// </summary>
        public DiagnosticDescription WithLocation(int line, int column)
        {
            return new DiagnosticDescription(code, isWarningAsError, squiggledText, arguments, new LinePosition(line - 1, column - 1), syntaxPredicate, argumentOrderDoesNotMatter, errorCodeType);
        }

        /// <summary>
        /// Can be used to unambiguously identify Diagnostics that can not be uniquely identified by code, squiggledText and arguments
        /// </summary>
        /// <param name="syntaxPredicate">The argument to syntaxPredicate will be the nearest SyntaxNode whose Span contains first squiggled character.</param>
        public DiagnosticDescription WhereSyntax(Func<SyntaxNode, bool> syntaxPredicate)
        {
            return new DiagnosticDescription(code, isWarningAsError, squiggledText, arguments, startPosition, syntaxPredicate, argumentOrderDoesNotMatter, errorCodeType);
        }

        public override bool Equals(object obj)
        {
            var d = obj as DiagnosticDescription;

            if (obj == null)
                return false;

            if (!code.Equals(d.code))
                return false;

            if (isWarningAsError != d.isWarningAsError)
                return false;

            if (!ignoreArgumentsWhenComparing)
            {
                if (squiggledText != d.squiggledText)
                    return false;
            }

            if (startPosition != null)
            {
                if (d.startPosition != null)
                {
                    if (startPosition.Value != d.startPosition.Value)
                    {
                        showPosition = true;
                        d.showPosition = true;
                        return false;
                    }

                    showPosition = false;
                    d.showPosition = false;
                }
            }

            if (syntaxPredicate != null)
            {
                if (d.location == null)
                    return false;

                if (!syntaxPredicate(d.location.SourceTree.GetRoot().FindToken(location.SourceSpan.Start, true).Parent))
                {
                    showPredicate = true;
                    return false;
                }

                showPredicate = false;
            }
            if (d.syntaxPredicate != null)
            {
                if (location == null)
                    return false;

                if (!d.syntaxPredicate(location.SourceTree.GetRoot().FindToken(location.SourceSpan.Start, true).Parent))
                {
                    d.showPredicate = true;
                    return false;
                }

                d.showPredicate = false;
            }

            // If ignoring arguments, we can skip the rest of this method.
            if (ignoreArgumentsWhenComparing || d.ignoreArgumentsWhenComparing)
                return true;

            // Only validation of arguments should happen between here and the end of this method.
            if (arguments == null)
            {
                if (d.arguments != null)
                    return false;
            }
            else // _arguments != null
            {
                if (d.arguments == null)
                    return false;

                // we'll compare the arguments as strings
                var args1 = GetArgumentsAsStrings();
                var args2 = d.GetArgumentsAsStrings();
                if (argumentOrderDoesNotMatter || d.argumentOrderDoesNotMatter)
                {
                    if (args1.Count() != args2.Count() || !args1.SetEquals(args2))
                        return false;
                }
                else
                {
                    if (!args1.SequenceEqual(args2))
                        return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode;
            hashCode = code.GetHashCode();
            hashCode = Hash.Combine(isWarningAsError.GetHashCode(), hashCode);
            hashCode = Hash.Combine(squiggledText, hashCode);
            hashCode = Hash.Combine(arguments, hashCode);
            if (startPosition != null)
                hashCode = Hash.Combine(hashCode, startPosition.Value.GetHashCode());
            return hashCode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Diagnostic(");
            if (errorCodeType == typeof(string))
            {
                sb.Append("\"").Append(code).Append("\"");
            }
            else
            {
                sb.Append(errorCodeType.Name);
                sb.Append(".");
                sb.Append(Enum.GetName(errorCodeType, code));
            }

            if (squiggledText != null)
            {
                if (squiggledText.Contains("\n") || squiggledText.Contains("\\") || squiggledText.Contains("\""))
                {
                    sb.Append(", @\"");
                    sb.Append(squiggledText.Replace("\"", "\"\""));
                }
                else
                {
                    sb.Append(", \"");
                    sb.Append(squiggledText);
                }

                sb.Append('"');
            }

            sb.Append(")");

            if (arguments != null)
            {
                sb.Append(".WithArguments(");
                var argumentStrings = GetArgumentsAsStrings().GetEnumerator();
                for (int i = 0; argumentStrings.MoveNext(); i++)
                {
                    sb.Append("\"");
                    sb.Append(argumentStrings.Current);
                    sb.Append("\"");
                    if (i < arguments.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(")");
            }

            if (startPosition != null && showPosition)
            {
                sb.Append(".WithLocation(");
                sb.Append(startPosition.Value.Line + 1);
                sb.Append(", ");
                sb.Append(startPosition.Value.Character + 1);
                sb.Append(")");
            }

            if (isWarningAsError)
            {
                sb.Append(".WithWarningAsError(true)");
            }

            if (syntaxPredicate != null && showPredicate)
            {
                sb.Append(".WhereSyntax(...)");
            }

            return sb.ToString();
        }

        public static string GetAssertText(DiagnosticDescription[] expected, IEnumerable<Diagnostic> actual)
        {
            var includeCompilerOutput = false;
            var includeDiagnosticMessagesAsComments = false;

            const int CSharp = 1;
            const int VisualBasic = 2;
            var language = actual.Any() && actual.First().Id.StartsWith("CS") ? CSharp : VisualBasic;
            
            if (language == CSharp)
            {
                includeDiagnosticMessagesAsComments = true;
            }

            StringBuilder assertText = new StringBuilder();
            assertText.AppendLine();

            // Write out the 'command line compiler output' including squiggles (easy to read debugging info in the case of a failure).
            // This will be useful for VB, because we can't do the inline comments.
            if (includeCompilerOutput)
            {
                assertText.AppendLine("Compiler output:");
                foreach (var d in actual)
                {
                    Indent(assertText, 1);
                    assertText.AppendLine(d.ToString());
                    var location = d.Location;
                    var lineText = location.SourceTree.GetText().Lines.GetLineFromPosition(location.SourceSpan.Start).ToString();
                    assertText.AppendLine(lineText);
                    var span = location.GetMappedLineSpan();
                    var startPosition = span.StartLinePosition;
                    var endPosition = span.EndLinePosition;
                    assertText.Append(' ', startPosition.Character);
                    var endCharacter = (startPosition.Line == endPosition.Line) ? endPosition.Character : lineText.Length;
                    assertText.Append('~', endCharacter - startPosition.Character);
                    assertText.AppendLine();
                }
            }

            // write out the error baseline as method calls
            int i;
            assertText.AppendLine("Expected:");
            var expectedText = new StringBuilder();
            for (i = 0; i < expected.Length; i++)
            {
                var d = expected[i];

                AppendDiagnosticDescription(expectedText, d);

                if (i < expected.Length - 1)
                {
                    expectedText.Append(",");
                }

                expectedText.AppendLine();
            }
            assertText.Append(expectedText);

            // write out the actual results as method calls (copy/paste this to update baseline)
            assertText.AppendLine("Actual:");
            var actualText = new StringBuilder();
            var e = actual.GetEnumerator();
            for (i = 0; e.MoveNext(); i++)
            {
                var d = e.Current;
                string message = d.ToString();
                if (Regex.Match(message, @"{\d+}").Success)
                {
                    Assert.True(false, "Diagnostic messages should never contain unsubstituted placeholders.\n    " + message);
                }

                if (i > 0)
                {
                    assertText.AppendLine(",");
                    actualText.AppendLine(",");
                }

                if (includeDiagnosticMessagesAsComments)
                {
                    Indent(assertText, 1);
                    assertText.Append("// ");
                    assertText.AppendLine(d.ToString());
                    var l = d.Location;
                    if (l.IsInSource)
                    {
                        Indent(assertText, 1);
                        assertText.Append("// ");
                        assertText.AppendLine(l.SourceTree.GetText().Lines.GetLineFromPosition(l.SourceSpan.Start).ToString());
                    }
                }

                var description = new DiagnosticDescription(d, errorCodeOnly: false, showPosition: true);
                AppendDiagnosticDescription(assertText, description);
                AppendDiagnosticDescription(actualText, description);
            }
            if (i > 0)
            {
                assertText.AppendLine();
                actualText.AppendLine();
            }

            assertText.AppendLine("Diff:");
            assertText.Append(DiffUtil.DiffReport(expectedText.ToString(), actualText.ToString()));

            return assertText.ToString();
        }

        private static void AppendDiagnosticDescription(StringBuilder sb, DiagnosticDescription d)
        {
            Indent(sb, 1);
            sb.Append(d.ToString());
        }

        private static void Indent(StringBuilder sb, int count)
        {
            sb.Append(' ', 4 * count);
        }
    }
}
