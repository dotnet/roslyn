// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#pragma warning disable CA1825 // Avoid zero-length array allocations.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
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
    internal abstract partial class Formatter : IDkmClrFormatter, IDkmClrFormatter2, IDkmClrFullNameProvider
    {
        private readonly string _defaultFormat;
        private readonly string _nullString;
        private readonly string _thisString;
        private string _hostValueNotFoundString => Resources.HostValueNotFound;

        internal Formatter(string defaultFormat, string nullString, string thisString)
        {
            _defaultFormat = defaultFormat;
            _nullString = nullString;
            _thisString = thisString;
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
            return GetTypeName(new TypeAndCustomInfo(type, typeInfo), escapeKeywordIdentifiers: false, sawInvalidIdentifier: out unused);
        }

        bool IDkmClrFormatter.HasUnderlyingString(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            return HasUnderlyingString(value, inspectionContext);
        }

        string IDkmClrFormatter.GetUnderlyingString(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            return GetUnderlyingString(value, inspectionContext);
        }

        string IDkmClrFormatter2.GetValueString(DkmClrValue value, DkmClrCustomTypeInfo customTypeInfo, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers)
        {
            return value.GetValueString(inspectionContext, formatSpecifiers);
        }

        string IDkmClrFormatter2.GetEditableValueString(DkmClrValue value, DkmInspectionContext inspectionContext, DkmClrCustomTypeInfo customTypeInfo)
        {
            return GetEditableValue(value, inspectionContext, customTypeInfo);
        }

        string IDkmClrFullNameProvider.GetClrTypeName(DkmInspectionContext inspectionContext, DkmClrType clrType, DkmClrCustomTypeInfo customTypeInfo)
        {
            Debug.Assert(inspectionContext != null);
            bool sawInvalidIdentifier;
            var name = GetTypeName(new TypeAndCustomInfo(clrType, customTypeInfo), escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
            return sawInvalidIdentifier ? null : name;
        }

        string IDkmClrFullNameProvider.GetClrArrayIndexExpression(DkmInspectionContext inspectionContext, string[] indices)
        {
            return GetArrayIndexExpression(indices);
        }

        string IDkmClrFullNameProvider.GetClrCastExpression(DkmInspectionContext inspectionContext, string argument, DkmClrType type, DkmClrCustomTypeInfo customTypeInfo, DkmClrCastExpressionOptions castExpressionOptions)
        {
            bool sawInvalidIdentifier;
            var name = GetTypeName(new TypeAndCustomInfo(type, customTypeInfo), escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
            if (sawInvalidIdentifier)
            {
                return null;
            }
            return GetCastExpression(argument, name, castExpressionOptions);
        }

        string IDkmClrFullNameProvider.GetClrObjectCreationExpression(DkmInspectionContext inspectionContext, DkmClrType type, DkmClrCustomTypeInfo customTypeInfo, string[] arguments)
        {
            bool sawInvalidIdentifier;
            var name = GetTypeName(new TypeAndCustomInfo(type, customTypeInfo), escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
            if (sawInvalidIdentifier)
            {
                return null;
            }
            return GetObjectCreationExpression(name, arguments);
        }

        string IDkmClrFullNameProvider.GetClrValidIdentifier(DkmInspectionContext inspectionContext, string identifier)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            bool sawInvalidIdentifier;
            AppendIdentifierEscapingPotentialKeywords(builder, identifier, out sawInvalidIdentifier);
            var result = sawInvalidIdentifier ? null : builder.ToString();
            pooledBuilder.Free();
            return result;
        }

        string IDkmClrFullNameProvider.GetClrExpressionAndFormatSpecifiers(DkmInspectionContext inspectionContext, string expression, out ReadOnlyCollection<string> formatSpecifiers)
        {
            return TrimAndGetFormatSpecifiers(expression, out formatSpecifiers);
        }

        bool IDkmClrFullNameProvider.ClrExpressionMayRequireParentheses(DkmInspectionContext inspectionContext, string expression)
        {
            return NeedsParentheses(expression);
        }

        string IDkmClrFullNameProvider.GetClrMemberName(
            DkmInspectionContext inspectionContext,
            string parentFullName,
            DkmClrType declaringType,
            DkmClrCustomTypeInfo declaringTypeInfo,
            string memberName,
            bool memberAccessRequiresExplicitCast,
            bool memberIsStatic)
        {
            string qualifier;
            if (memberIsStatic)
            {
                bool sawInvalidIdentifier;
                qualifier = GetTypeName(new TypeAndCustomInfo(declaringType, declaringTypeInfo), escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
                if (sawInvalidIdentifier)
                {
                    return null; // FullName wouldn't be parseable.
                }
            }
            else if (memberAccessRequiresExplicitCast)
            {
                bool sawInvalidIdentifier;
                var typeName = GetTypeName(new TypeAndCustomInfo(declaringType, declaringTypeInfo), escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
                if (sawInvalidIdentifier)
                {
                    return null; // FullName wouldn't be parseable.
                }
                qualifier = GetCastExpression(parentFullName, typeName, DkmClrCastExpressionOptions.ParenthesizeEntireExpression);
            }
            else
            {
                qualifier = parentFullName;
            }
            return $"{qualifier}.{memberName}";
        }

        string IDkmClrFullNameProvider.GetClrExpressionForNull(DkmInspectionContext inspectionContext)
        {
            return _nullString;
        }

        string IDkmClrFullNameProvider.GetClrExpressionForThis(DkmInspectionContext inspectionContext)
        {
            return _thisString;
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
        private bool NeedsParentheses(string expr)
        {
            int parens = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                var ch = expr[i];
                switch (ch)
                {
                    case '(':
                        // Cast, "(A)b", requires parentheses.
                        if ((parens == 0) && FollowsCloseParen(expr, i))
                        {
                            return true;
                        }
                        parens++;
                        break;
                    case '[':
                        parens++;
                        break;
                    case ')':
                    case ']':
                        parens--;
                        break;
                    case '.':
                        break;
                    default:
                        if (parens == 0)
                        {
                            if (this.IsIdentifierPartCharacter(ch))
                            {
                                // Cast, "(A)b", requires parentheses.
                                if (FollowsCloseParen(expr, i))
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                        break;
                }
            }
            return false;
        }

        private static bool FollowsCloseParen(string expr, int index)
        {
            return (index > 0) && (expr[index - 1] == ')');
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
