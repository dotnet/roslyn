// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Roslyn.Utilities;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed partial class CSharpFormatter : Formatter
    {
        private void AppendEnumTypeAndName(StringBuilder builder, Type typeToDisplayOpt, string name)
        {
            if (typeToDisplayOpt != null)
            {
                // We're showing the type of a value, so "dynamic" does not apply.
                bool unused;
                int index = 0;
                AppendQualifiedTypeName(builder, typeToDisplayOpt, default(DynamicFlagsCustomTypeInfo), ref index, escapeKeywordIdentifiers: true, sawInvalidIdentifier: out unused);
                builder.Append('.');
                AppendIdentifierEscapingPotentialKeywords(builder, name, sawInvalidIdentifier: out unused);
            }
            else
            {
                builder.Append(name);
            }
        }

        internal override string GetArrayDisplayString(DkmClrAppDomain appDomain, Type lmrType, ReadOnlyCollection<int> sizes, ReadOnlyCollection<int> lowerBounds, ObjectDisplayOptions options)
        {
            Debug.Assert(lmrType.IsArray);

            Type originalLmrType = lmrType;

            // Strip off all array types.  We'll process them at the end.
            while (lmrType.IsArray)
            {
                lmrType = lmrType.GetElementType();
            }

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            builder.Append('{');

            // We're showing the type of a value, so "dynamic" does not apply.
            bool unused;
            builder.Append(GetTypeName(new TypeAndCustomInfo(DkmClrType.Create(appDomain, lmrType)), escapeKeywordIdentifiers: false, sawInvalidIdentifier: out unused)); // NOTE: call our impl directly, since we're coupled anyway.

            var numSizes = sizes.Count;

            builder.Append('[');
            for (int i = 0; i < numSizes; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                var lowerBound = lowerBounds[i];
                var size = sizes[i];
                if (lowerBound == 0)
                {
                    builder.Append(FormatLiteral(size, options));
                }
                else
                {
                    builder.Append(FormatLiteral(lowerBound, options));
                    builder.Append("..");
                    builder.Append(FormatLiteral(size + lowerBound - 1, options));
                }
            }
            builder.Append(']');

            lmrType = originalLmrType.GetElementType(); // Strip off one layer (already handled).
            while (lmrType.IsArray)
            {
                builder.Append('[');
                builder.Append(',', lmrType.GetArrayRank() - 1);
                builder.Append(']');

                lmrType = lmrType.GetElementType();
            }

            builder.Append('}');
            return pooled.ToStringAndFree();
        }

        internal override string GetArrayIndexExpression(int[] indices)
        {
            Debug.Assert(indices != null);

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            builder.Append('[');
            bool any = false;
            foreach (var index in indices)
            {
                if (any)
                {
                    builder.Append(", ");
                }
                builder.Append(index);
                any = true;
            }
            builder.Append(']');

            return pooled.ToStringAndFree();
        }

        internal override string GetCastExpression(string argument, string type, bool parenthesizeArgument, bool parenthesizeEntireExpression)
        {
            Debug.Assert(!string.IsNullOrEmpty(argument));
            Debug.Assert(!string.IsNullOrEmpty(type));

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            if (parenthesizeEntireExpression)
            {
                builder.Append('(');
            }

            var castExpressionFormat = parenthesizeArgument ? "({0})({1})" : "({0}){1}";
            builder.AppendFormat(castExpressionFormat, type, argument);

            if (parenthesizeEntireExpression)
            {
                builder.Append(')');
            }

            return pooled.ToStringAndFree();
        }

        internal override string GetNamesForFlagsEnumValue(ArrayBuilder<EnumField> fields, object value, ulong underlyingValue, ObjectDisplayOptions options, Type typeToDisplayOpt)
        {
            var usedFields = ArrayBuilder<EnumField>.GetInstance();
            FillUsedEnumFields(usedFields, fields, underlyingValue);

            if (usedFields.Count == 0)
            {
                return null;
            }

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            for (int i = usedFields.Count - 1; i >= 0; i--) // Backwards to list smallest first.
            {
                AppendEnumTypeAndName(builder, typeToDisplayOpt, usedFields[i].Name);

                if (i > 0)
                {
                    builder.Append(" | ");
                }
            }

            usedFields.Free();

            return pooled.ToStringAndFree();
        }

        internal override string GetNameForEnumValue(ArrayBuilder<EnumField> fields, object value, ulong underlyingValue, ObjectDisplayOptions options, Type typeToDisplayOpt)
        {
            foreach (var field in fields)
            {
                // First match wins (deterministic since sorted).
                if (underlyingValue == field.Value)
                {
                    var pooled = PooledStringBuilder.GetInstance();
                    var builder = pooled.Builder;

                    AppendEnumTypeAndName(builder, typeToDisplayOpt, field.Name);

                    return pooled.ToStringAndFree();
                }
            }

            return null;
        }

        internal override string GetObjectCreationExpression(string type, string arguments)
        {
            Debug.Assert(!string.IsNullOrEmpty(type));

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            builder.Append("new ");
            builder.Append(type);
            builder.Append('(');
            builder.Append(arguments);
            builder.Append(')');

            return pooled.ToStringAndFree();
        }

        internal override string FormatLiteral(char c, ObjectDisplayOptions options)
        {
            return ObjectDisplay.FormatLiteral(c, options);
        }

        internal override string FormatLiteral(int value, ObjectDisplayOptions options)
        {
            return ObjectDisplay.FormatLiteral(value, options & ~(ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));
        }

        internal override string FormatPrimitiveObject(object value, ObjectDisplayOptions options)
        {
            return ObjectDisplay.FormatPrimitive(value, options);
        }

        internal override string FormatString(string str, ObjectDisplayOptions options)
        {
            return ObjectDisplay.FormatLiteral(str, options);
        }
    }
}
