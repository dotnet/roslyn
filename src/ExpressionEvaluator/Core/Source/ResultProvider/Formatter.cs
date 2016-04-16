// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Computes string representations of <see cref="DkmClrValue"/> instances.
    /// </summary>
    internal abstract partial class Formatter : IDkmClrFormatter
    {
        private readonly string _defaultFormat;
        private readonly string _nullString;
        internal readonly string StaticMembersString;

        internal Formatter(string defaultFormat, string nullString, string staticMembersString)
        {
            _defaultFormat = defaultFormat;
            _nullString = nullString;
            this.StaticMembersString = staticMembersString;
        }

        string IDkmClrFormatter.GetValueString(DkmClrValue value, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers)
        {
            var useQuotes = (inspectionContext.EvaluationFlags & DkmEvaluationFlags.NoQuotes) == 0;
            var options = useQuotes
                ? ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters
                : ObjectDisplayOptions.None;
            return GetValueString(value, inspectionContext, options, GetValueFlags.IncludeObjectId);
        }

        string IDkmClrFormatter.GetTypeName(DkmInspectionContext inspectionContext, DkmClrType type, DkmClrCustomTypeInfo typeInfo, ReadOnlyCollection<string> formatSpecifiers)
        {
            bool unused;
            return GetTypeName(new TypeAndCustomInfo(type.GetLmrType(), typeInfo), escapeKeywordIdentifiers: false, sawInvalidIdentifier: out unused);
        }

        bool IDkmClrFormatter.HasUnderlyingString(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            return HasUnderlyingString(value, inspectionContext);
        }

        string IDkmClrFormatter.GetUnderlyingString(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            return GetUnderlyingString(value, inspectionContext);
        }

        // CONSIDER: If the number or complexity of the "language-specific syntax helpers" grows (or if
        // we make this a public API, it would be good to consider abstracting them into a separate object
        // that can be passed to the ResultProvider on construction (a "LanguageSyntax" service of sorts).
        // It seems more natural to ask these questions of the ResultProvider, but adding such a component
        // for these few methods seemed a bit overly elaborate given the current internal usage.
        #region Language-specific syntax helpers

        internal abstract bool IsValidIdentifier(string name);

        internal abstract bool IsIdentifierPartCharacter(char c);

        internal abstract bool IsPredefinedType(Type type);

        internal abstract bool IsWhitespace(char c);

        // Note: We could be less conservative (e.g. "new C()").
        internal bool NeedsParentheses(string expr)
        {
            foreach (var ch in expr)
            {
                if (!this.IsIdentifierPartCharacter(ch) && !this.IsWhitespace(ch) && ch != '.') return true;
            }

            return false;
        }

        internal abstract string TrimAndGetFormatSpecifiers(string expression, out ReadOnlyCollection<string> formatSpecifiers);

        internal static readonly ReadOnlyCollection<string> NoFormatSpecifiers = new ReadOnlyCollection<string>(new string[0]);

        internal static ReadOnlyCollection<string> AddFormatSpecifier(ReadOnlyCollection<string> formatSpecifiers, string formatSpecifier)
        {
            if (formatSpecifiers.Contains(formatSpecifier))
            {
                return formatSpecifiers;
            }
            var builder = ArrayBuilder<string>.GetInstance();
            builder.AddRange(formatSpecifiers);
            builder.Add(formatSpecifier);
            return builder.ToImmutableAndFree();
        }

        protected string RemoveLeadingAndTrailingContent(string expression, int start, int length, Predicate<char> leading, Predicate<char> trailing)
        {
            int oldLength = expression.Length;
            for (; start < oldLength && leading(expression[start]); start++)
            {
            }
            for (; length > start && trailing(expression[length - 1]); length--)
            {
            }
            if ((start > 0) || (length < oldLength))
            {
                return expression.Substring(start, length - start);
            }
            return expression;
        }

        protected string RemoveLeadingAndTrailingWhitespace(string expression)
        {
            return RemoveLeadingAndTrailingContent(expression, 0, expression.Length, IsWhitespace, IsWhitespace);
        }

        protected string RemoveFormatSpecifiers(string expression, out ReadOnlyCollection<string> formatSpecifiers)
        {
            var builder = ArrayBuilder<string>.GetInstance();
            int oldLength = expression.Length;
            int newLength = oldLength;
            for (var i = oldLength - 1; i >= 0; i--)
            {
                var ch = expression[i];
                if (ch == ',')
                {
                    builder.Add(RemoveLeadingAndTrailingContent(expression, i + 1, newLength, IsWhitespace, IsWhitespace));
                    newLength = i;
                }
                else if (!IsIdentifierPartCharacter(ch) && !IsWhitespace(ch))
                {
                    break;
                }
            }

            if (builder.Count == 0)
            {
                formatSpecifiers = NoFormatSpecifiers;
            }
            else
            {
                var specifiers = builder.ToArray();
                Array.Reverse(specifiers);
                formatSpecifiers = new ReadOnlyCollection<string>(specifiers);
            }
            builder.Free();

            Debug.Assert((formatSpecifiers.Count == 0) == (newLength == oldLength));
            if (newLength < oldLength)
            {
                return expression.Substring(0, newLength);
            }
            return expression;
        }

        #endregion
    }
}
