// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class DiagnosticDescription
    {
        public static readonly DiagnosticDescription[] None = { };
        public static readonly DiagnosticDescription[] Any = null;

        // common fields for all DiagnosticDescriptions
        private readonly object _code;
        private readonly bool _isWarningAsError;
        private readonly string _squiggledText;
        private readonly object[] _arguments;
        private readonly LinePosition? _startPosition; // May not have a value only in the case that we're constructed via factories
        private readonly bool _argumentOrderDoesNotMatter;
        private readonly Type _errorCodeType;
        private readonly bool _ignoreArgumentsWhenComparing;
        private readonly DiagnosticSeverity? _defaultSeverityOpt;
        private readonly DiagnosticSeverity? _effectiveSeverityOpt;

        // fields for DiagnosticDescriptions constructed via factories
        private readonly Func<SyntaxNode, bool> _syntaxPredicate;
        private bool _showPredicate; // show predicate in ToString if comparison fails

        // fields for DiagnosticDescriptions constructed from Diagnostics
        private readonly Location _location;

        private IEnumerable<string> _argumentsAsStrings;
        private IEnumerable<string> GetArgumentsAsStrings()
        {
            if (_argumentsAsStrings == null)
            {
                // We'll use IFormattable here, because it is more explicit than just calling .ToString()
                // (and is closer to what the compiler actually does when displaying error messages)
                _argumentsAsStrings = _arguments.Select(o =>
                {
                    if (o is DiagnosticInfo embedded)
                    {
                        return embedded.GetMessage(EnsureEnglishUICulture.PreferredOrNull);
                    }

                    return string.Format(EnsureEnglishUICulture.PreferredOrNull, "{0}", o);
                });
            }
            return _argumentsAsStrings;
        }

        public DiagnosticDescription(
            object code,
            bool isWarningAsError,
            string squiggledText,
            object[] arguments,
            LinePosition? startLocation,
            Func<SyntaxNode, bool> syntaxNodePredicate,
            bool argumentOrderDoesNotMatter,
            Type errorCodeType = null,
            DiagnosticSeverity? defaultSeverityOpt = null,
            DiagnosticSeverity? effectiveSeverityOpt = null)
        {
            _code = code;
            _isWarningAsError = isWarningAsError;
            _squiggledText = squiggledText;
            _arguments = arguments;
            _startPosition = startLocation;
            _syntaxPredicate = syntaxNodePredicate;
            _argumentOrderDoesNotMatter = argumentOrderDoesNotMatter;
            _errorCodeType = errorCodeType ?? code.GetType();
            _defaultSeverityOpt = defaultSeverityOpt;
            _effectiveSeverityOpt = effectiveSeverityOpt;
        }

        public DiagnosticDescription(
            object code,
            string squiggledText,
            object[] arguments,
            LinePosition? startLocation,
            Func<SyntaxNode, bool> syntaxNodePredicate,
            bool argumentOrderDoesNotMatter,
            Type errorCodeType = null,
            DiagnosticSeverity? defaultSeverityOpt = null,
            DiagnosticSeverity? effectiveSeverityOpt = null)
        {
            _code = code;
            _isWarningAsError = false;
            _squiggledText = squiggledText;
            _arguments = arguments;
            _startPosition = startLocation;
            _syntaxPredicate = syntaxNodePredicate;
            _argumentOrderDoesNotMatter = argumentOrderDoesNotMatter;
            _errorCodeType = errorCodeType ?? code.GetType();
            _defaultSeverityOpt = defaultSeverityOpt;
            _effectiveSeverityOpt = effectiveSeverityOpt;
        }

        public DiagnosticDescription(Diagnostic d, bool errorCodeOnly, bool includeDefaultSeverity = false, bool includeEffectiveSeverity = false)
        {
            _code = d.Code;
            _isWarningAsError = d.IsWarningAsError;
            _location = d.Location;
            _defaultSeverityOpt = includeDefaultSeverity ? d.DefaultSeverity : (DiagnosticSeverity?)null;
            _effectiveSeverityOpt = includeEffectiveSeverity ? d.Severity : (DiagnosticSeverity?)null;

            DiagnosticWithInfo dinfo = null;
            if (d.Code == 0)
            {
                _code = d.Id;
                _errorCodeType = typeof(string);
            }
            else
            {
                dinfo = d as DiagnosticWithInfo;
                if (dinfo == null)
                {
                    _code = d.Code;
                    _errorCodeType = typeof(int);
                }
                else
                {
                    _errorCodeType = dinfo.Info.MessageProvider.ErrorCodeType;
                    _code = d.Code;
                }
            }

            _ignoreArgumentsWhenComparing = errorCodeOnly;

            if (!_ignoreArgumentsWhenComparing)
            {
                if (_location.IsInSource)
                {
                    // we don't just want to do SyntaxNode.GetText(), because getting the text via the SourceTree validates the public API
                    _squiggledText = _location.SourceTree.GetText().ToString(_location.SourceSpan);
                }

                if (dinfo != null)
                {
                    _arguments = dinfo.Info.Arguments;
                }
                else
                {
                    var args = d.Arguments;
                    if (args == null || args.Count == 0)
                    {
                        _arguments = null;
                    }
                    else
                    {
                        _arguments = d.Arguments.ToArray();
                    }
                }

                if (_arguments is { Length: 0 })
                {
                    _arguments = null;
                }
            }

            _startPosition = _location.GetMappedLineSpan().StartLinePosition;
        }

        public DiagnosticDescription WithArguments(params string[] arguments)
        {
            return new DiagnosticDescription(_code, _isWarningAsError, _squiggledText, arguments, _startPosition, _syntaxPredicate, false, _errorCodeType, _defaultSeverityOpt, _effectiveSeverityOpt);
        }

        public DiagnosticDescription WithArgumentsAnyOrder(params string[] arguments)
        {
            return new DiagnosticDescription(_code, _isWarningAsError, _squiggledText, arguments, _startPosition, _syntaxPredicate, true, _errorCodeType, _defaultSeverityOpt, _effectiveSeverityOpt);
        }

        public DiagnosticDescription WithWarningAsError(bool isWarningAsError)
        {
            return new DiagnosticDescription(_code, isWarningAsError, _squiggledText, _arguments, _startPosition, _syntaxPredicate, true, _errorCodeType, _defaultSeverityOpt, _effectiveSeverityOpt);
        }

        public DiagnosticDescription WithDefaultSeverity(DiagnosticSeverity defaultSeverity)
        {
            return new DiagnosticDescription(_code, _isWarningAsError, _squiggledText, _arguments, _startPosition, _syntaxPredicate, true, _errorCodeType, defaultSeverity, _effectiveSeverityOpt);
        }

        public DiagnosticDescription WithEffectiveSeverity(DiagnosticSeverity effectiveSeverity)
        {
            return new DiagnosticDescription(_code, _isWarningAsError, _squiggledText, _arguments, _startPosition, _syntaxPredicate, true, _errorCodeType, _defaultSeverityOpt, effectiveSeverity);
        }

        /// <summary>
        /// Specialized syntaxPredicate that can be used to verify the start of the squiggled Span
        /// </summary>
        public DiagnosticDescription WithLocation(int line, int column)
        {
            return new DiagnosticDescription(_code, _isWarningAsError, _squiggledText, _arguments, new LinePosition(line - 1, column - 1), _syntaxPredicate, _argumentOrderDoesNotMatter, _errorCodeType, _defaultSeverityOpt, _effectiveSeverityOpt);
        }

        /// <summary>
        /// Can be used to unambiguously identify Diagnostics that can not be uniquely identified by code, squiggledText and arguments
        /// </summary>
        /// <param name="syntaxPredicate">The argument to syntaxPredicate will be the nearest SyntaxNode whose Span contains first squiggled character.</param>
        public DiagnosticDescription WhereSyntax(Func<SyntaxNode, bool> syntaxPredicate)
        {
            return new DiagnosticDescription(_code, _isWarningAsError, _squiggledText, _arguments, _startPosition, syntaxPredicate, _argumentOrderDoesNotMatter, _errorCodeType, _defaultSeverityOpt, _effectiveSeverityOpt);
        }

        public object Code => _code;
        public bool HasLocation => _startPosition != null;
        public bool IsWarningAsError => _isWarningAsError;
        public DiagnosticSeverity? DefaultSeverity => _defaultSeverityOpt;
        public DiagnosticSeverity? EffectiveSeverity => _effectiveSeverityOpt;

        public override bool Equals(object obj)
        {
            var d = obj as DiagnosticDescription;

            if (d == null)
                return false;

            if (!_code.Equals(d._code))
                return false;

            if (_isWarningAsError != d._isWarningAsError)
                return false;

            if (!_ignoreArgumentsWhenComparing)
            {
                if (_squiggledText != d._squiggledText)
                    return false;
            }

            if (_startPosition != null)
            {
                if (d._startPosition != null)
                {
                    if (_startPosition.Value != d._startPosition.Value)
                    {
                        return false;
                    }
                }
            }

            if (_syntaxPredicate != null)
            {
                if (d._location == null)
                    return false;

                if (!_syntaxPredicate(d._location.SourceTree.GetRoot().FindToken(_location.SourceSpan.Start, true).Parent))
                {
                    _showPredicate = true;
                    return false;
                }

                _showPredicate = false;
            }
            if (d._syntaxPredicate != null)
            {
                if (_location == null)
                    return false;

                if (!d._syntaxPredicate(_location.SourceTree.GetRoot().FindToken(_location.SourceSpan.Start, true).Parent))
                {
                    d._showPredicate = true;
                    return false;
                }

                d._showPredicate = false;
            }

            // If ignoring arguments, we can skip the rest of this method.
            if (_ignoreArgumentsWhenComparing || d._ignoreArgumentsWhenComparing)
                return true;

            // Only validation of arguments should happen between here and the end of this method.
            if (_arguments == null)
            {
                if (d._arguments != null)
                    return false;
            }
            else // _arguments != null
            {
                if (d._arguments == null)
                    return false;

                // we'll compare the arguments as strings
                var args1 = GetArgumentsAsStrings();
                var args2 = d.GetArgumentsAsStrings();
                if (_argumentOrderDoesNotMatter || d._argumentOrderDoesNotMatter)
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

            if (_defaultSeverityOpt != d._defaultSeverityOpt ||
                _effectiveSeverityOpt != d._effectiveSeverityOpt)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode;
            hashCode = _code.GetHashCode();
            hashCode = Hash.Combine(_isWarningAsError.GetHashCode(), hashCode);

            // TODO: !!! This implementation isn't consistent with Equals, which might ignore inequality of some members based on ignoreArgumentsWhenComparing flag, etc.
            hashCode = Hash.Combine(_squiggledText, hashCode);
            hashCode = Hash.Combine(_arguments, hashCode);
            if (_startPosition != null)
                hashCode = Hash.Combine(hashCode, _startPosition.Value.GetHashCode());
            if (_defaultSeverityOpt != null)
                hashCode = Hash.Combine(hashCode, _defaultSeverityOpt.Value.GetHashCode());
            if (_effectiveSeverityOpt != null)
                hashCode = Hash.Combine(hashCode, _effectiveSeverityOpt.Value.GetHashCode());
            return hashCode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Diagnostic(");
            if (_errorCodeType == typeof(string))
            {
                sb.Append("\"").Append(_code).Append("\"");
            }
            else
            {
                sb.Append(_errorCodeType.Name);
                sb.Append(".");
                sb.Append(Enum.GetName(_errorCodeType, _code));
            }

            if (_squiggledText != null)
            {
                if (_squiggledText.Contains("\n") || _squiggledText.Contains("\\") || _squiggledText.Contains("\""))
                {
                    sb.Append(", @\"");
                    sb.Append(_squiggledText.Replace("\"", "\"\""));
                }
                else
                {
                    sb.Append(", \"");
                    sb.Append(_squiggledText);
                }

                sb.Append('"');
            }

            sb.Append(")");

            if (_arguments != null)
            {
                sb.Append(".WithArguments(");
                var argumentStrings = GetArgumentsAsStrings().GetEnumerator();
                for (int i = 0; argumentStrings.MoveNext(); i++)
                {
                    sb.Append("\"");
                    sb.Append(argumentStrings.Current);
                    sb.Append("\"");
                    if (i < _arguments.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(")");
            }

            if (_startPosition != null)
            {
                sb.Append(".WithLocation(");
                sb.Append(_startPosition.Value.Line + 1);
                sb.Append(", ");
                sb.Append(_startPosition.Value.Character + 1);
                sb.Append(")");
            }

            if (_isWarningAsError)
            {
                sb.Append(".WithWarningAsError(true)");
            }

            if (_defaultSeverityOpt != null)
            {
                sb.Append($".WithDefaultSeverity(DiagnosticSeverity.{_defaultSeverityOpt.Value.ToString()})");
            }

            if (_effectiveSeverityOpt != null)
            {
                sb.Append($".WithEffectiveSeverity(DiagnosticSeverity.{_effectiveSeverityOpt.Value.ToString()})");
            }

            if (_syntaxPredicate != null && _showPredicate)
            {
                sb.Append(".WhereSyntax(...)");
            }

            return sb.ToString();
        }

        public static string GetAssertText(DiagnosticDescription[] expected, IEnumerable<Diagnostic> actual)
        {
            const int CSharp = 1;
            const int VisualBasic = 2;
            var language = actual.Any() && actual.First().Id.StartsWith("CS", StringComparison.Ordinal) ? CSharp : VisualBasic;
            var includeDiagnosticMessagesAsComments = (language == CSharp);
            int indentDepth = (language == CSharp) ? 4 : 1;
            var includeDefaultSeverity = expected.Any() && expected.All(d => d.DefaultSeverity != null);
            var includeEffectiveSeverity = expected.Any() && expected.All(d => d.EffectiveSeverity != null);

            if (IsSortedOrEmpty(expected))
            {
                // If this is a new test (empty expectations) or a test that's already sorted,
                // we sort the actual diagnostics to minimize diff noise as diagnostics change.
                actual = Sort(actual);
            }

            var assertText = new StringBuilder();
            assertText.AppendLine();

            // write out the error baseline as method calls
            int i;
            assertText.AppendLine("Expected:");
            var expectedText = ArrayBuilder<string>.GetInstance();
            foreach (var d in expected)
            {
                expectedText.Add(GetDiagnosticDescription(d, indentDepth));
            }
            GetCommaSeparatedLines(assertText, expectedText);

            // write out the actual results as method calls (copy/paste this to update baseline)
            assertText.AppendLine("Actual:");
            var actualText = ArrayBuilder<string>.GetInstance();
            var e = actual.GetEnumerator();
            for (i = 0; e.MoveNext(); i++)
            {
                Diagnostic d = e.Current;
                string message = d.ToString();
                if (Regex.Match(message, @"{\d+}").Success)
                {
                    Assert.True(false, "Diagnostic messages should never contain unsubstituted placeholders.\n    " + message);
                }

                if (i > 0)
                {
                    assertText.AppendLine(",");
                }

                if (includeDiagnosticMessagesAsComments)
                {
                    Indent(assertText, indentDepth);
                    assertText.Append("// ");
                    assertText.AppendLine(d.ToString());
                    var l = d.Location;
                    if (l.IsInSource)
                    {
                        Indent(assertText, indentDepth);
                        assertText.Append("// ");
                        assertText.AppendLine(l.SourceTree.GetText().Lines.GetLineFromPosition(l.SourceSpan.Start).ToString());
                    }
                }

                var description = new DiagnosticDescription(d, errorCodeOnly: false, includeDefaultSeverity, includeEffectiveSeverity);
                var diffDescription = description;
                var idx = Array.IndexOf(expected, description);
                if (idx != -1)
                {
                    diffDescription = expected[idx];
                }
                assertText.Append(GetDiagnosticDescription(description, indentDepth));
                actualText.Add(GetDiagnosticDescription(diffDescription, indentDepth));
            }
            if (i > 0)
            {
                assertText.AppendLine();
            }

            assertText.AppendLine("Diff:");
            assertText.Append(DiffUtil.DiffReport(expectedText, actualText, separator: Environment.NewLine));

            actualText.Free();
            expectedText.Free();

            return assertText.ToString();
        }

        private static IEnumerable<Diagnostic> Sort(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.GetMappedLineSpan().StartLinePosition, LinePositionComparer.Instance);
        }

        private static bool IsSortedOrEmpty(DiagnosticDescription[] diagnostics)
        {
            var comparer = LinePositionComparer.Instance;
            DiagnosticDescription last = null;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic._startPosition == null)
                {
                    return false;
                }
                if (last != null && comparer.Compare(last._startPosition, diagnostic._startPosition) > 0)
                {
                    return false;
                }
                last = diagnostic;
            }
            return true;
        }

        private static string GetDiagnosticDescription(DiagnosticDescription d, int indentDepth)
        {
            return new string(' ', 4 * indentDepth) + d.ToString();
        }

        private static void Indent(StringBuilder sb, int count)
        {
            sb.Append(' ', 4 * count);
        }

        private static void GetCommaSeparatedLines(StringBuilder sb, ArrayBuilder<string> lines)
        {
            int n = lines.Count;
            for (int i = 0; i < n; i++)
            {
                sb.Append(lines[i]);
                if (i < n - 1)
                {
                    sb.Append(',');
                }
                sb.AppendLine();
            }
        }

        private class LinePositionComparer : IComparer<LinePosition?>
        {
            internal static LinePositionComparer Instance = new LinePositionComparer();

            public int Compare(LinePosition? x, LinePosition? y)
            {
                if (x == null)
                {
                    if (y == null)
                    {
                        return 0;
                    }
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                int lineDiff = x.Value.Line.CompareTo(y.Value.Line);
                if (lineDiff != 0)
                {
                    return lineDiff;
                }

                return x.Value.Character.CompareTo(y.Value.Character);
            }
        }
    }
}
