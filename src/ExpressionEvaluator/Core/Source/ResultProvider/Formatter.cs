// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
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

        // True if the language in question supports 3rd-party customizations via the IDkmClrFormatter interface.
        // By default, this is true, as there existing 3rd-party EE's out there that implement IDkmClrFormatter
        // to customize the formatting of the C# EE.  One might wonder - why make this overridable at all - why
        // not true always?  The reason is that the IDkmClrFormatter interfaces were cobbled together quickly
        // in order to meet the deadline for inclusion in the VS 2015 release, and the signature of some of the
        // interface methods is missing things.  In particular, the MC++ EE needs GetValueString() to know
        // the DkmClrCustomTypeInfo for the declared type in order for C++ reference values or boxed values to
        // get displayed correctly.  But, the existing interface requires GetValueString() to NOT know about
        // the custom type info.
        //
        // Hopefully, we will someday clean up the formatting interfaces to allow for more general 3rd-party
        // customization than what today's interface supports.  But, for now, decision has been made to hack
        // things together to allow the MC++ EE to be able to function with a minimal amount of work.  So,
        // what we do is this:
        //  - All languages except MC++ throw away the custom type info and call into the existing interface.
        //      This keeps C# and VB working (which don't need the custom type info), and also 3rd-party languages
        //      that plug into the existing interfaces (since their components will still get called).
        //  - 

        protected virtual bool UseIDkmClrFormatterInterface
        {
            get
            {
                return true;
            }
        }

        string IDkmClrFormatter.GetValueString(DkmClrValue value, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers)
        {
            // Note: This interface drops the custom type info object on the floor.  It sucks, but the signature
            // of IDkmClrFormatter has already shipped, and we can't change it without breaking things.  Fortunately, C# and VB
            // do not actually depend on the custom type info for purposes of value formatting, so it doesn't matter there.
            // MC++, which does need the custom type info, avoids the problem by not getting here (the caller checks the UseIDkmClrFormatterInterface property
            // before calling us).
            //
            // Even under MC++, it is still possible to end here in the context of evaluating DebuggerDisplay expressions, but DebuggerDisplay is always C#, so, again,
            // dropping the custom type info doesn't matter in that case.

            var options = ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.NoQuotes) == 0) ?
                ObjectDisplayOptions.UseQuotes :
                ObjectDisplayOptions.None;

            return GetValueString(value, inspectionContext, options, GetValueFlags.IncludeObjectId, customTypeInfo: null);
        }

        // Do not change the signature of this method.  The MC++ EE overrides this.
        internal string GetValueString(DkmClrValue value, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers, DkmClrCustomTypeInfo customTypeInfo)
        {
            if(UseIDkmClrFormatterInterface)
            {
                return value.GetValueString(inspectionContext, formatSpecifiers);
            }
            else
            {
                var options = ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.NoQuotes) == 0) ?
                    ObjectDisplayOptions.UseQuotes :
                    ObjectDisplayOptions.None;
                return GetValueString(value, inspectionContext, options, GetValueFlags.IncludeObjectId, customTypeInfo);
            }
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

        /// <returns>
        /// The qualified name (i.e. including containing types and namespaces) of a named, pointer,
        /// or array type followed by the qualified name of the actual runtime type, if provided.
        /// 
        /// The resultant string combines both the declared type and the runtime type of the value.
        /// This is overridden by the managed C++ EE to handle special language-specific cases, such as C++ reference types.
        /// </returns>
        internal virtual string GetTypeNameOfValue(DkmInspectionContext inspectionContext, DkmClrValue value, DkmClrType declaredType, DkmClrCustomTypeInfo declaredTypeInfo, ExpansionKind kind)
        {
            var declaredLmrType = declaredType.GetLmrType();
            var runtimeType = value.Type;
            var runtimeLmrType = runtimeType.GetLmrType();
            var declaredTypeName = inspectionContext.GetTypeName(declaredType, declaredTypeInfo, Formatter.NoFormatSpecifiers);
            var runtimeTypeName = inspectionContext.GetTypeName(runtimeType, CustomTypeInfo: null, FormatSpecifiers: Formatter.NoFormatSpecifiers);
            var includeRuntimeTypeName =
                !string.Equals(declaredTypeName, runtimeTypeName, StringComparison.OrdinalIgnoreCase) && // Names will reflect "dynamic", types will not.
                !declaredLmrType.IsPointer &&
                (kind != ExpansionKind.PointerDereference) &&
                (!declaredLmrType.IsNullable() || value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown));
            return includeRuntimeTypeName ?
                string.Format("{0} {{{1}}}", declaredTypeName, runtimeTypeName) :
                declaredTypeName;

        }

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
