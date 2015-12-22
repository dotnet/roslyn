// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed partial class CSharpFormatter : Formatter
    {
        private static bool IsPotentialKeyword(string identifier)
        {
            return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None;
        }

        protected override void AppendIdentifierEscapingPotentialKeywords(StringBuilder builder, string identifier, out bool sawInvalidIdentifier)
        {
            sawInvalidIdentifier = !SyntaxFacts.IsValidIdentifier(identifier);
            if (IsPotentialKeyword(identifier))
            {
                builder.Append('@');
            }
            builder.Append(identifier);
        }

        protected override void AppendGenericTypeArgumentList(
            StringBuilder builder,
            Type[] typeArguments,
            int typeArgumentOffset,
            DynamicFlagsCustomTypeInfo dynamicFlags,
            ref int index,
            int arity,
            bool escapeKeywordIdentifiers,
            out bool sawInvalidIdentifier)
        {
            sawInvalidIdentifier = false;
            builder.Append('<');
            for (int i = 0; i < arity; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                Type typeArgument = typeArguments[typeArgumentOffset + i];
                bool sawSingleInvalidIdentifier;
                AppendQualifiedTypeName(builder, typeArgument, dynamicFlags, ref index, escapeKeywordIdentifiers, out sawSingleInvalidIdentifier);
                sawInvalidIdentifier |= sawSingleInvalidIdentifier;
            }
            builder.Append('>');
        }

        protected override void AppendRankSpecifier(StringBuilder builder, int rank)
        {
            Debug.Assert(rank > 0);

            builder.Append('[');
            builder.Append(',', rank - 1);
            builder.Append(']');
        }

        protected override bool AppendSpecialTypeName(StringBuilder builder, Type type, bool isDynamic)
        {
            if (isDynamic)
            {
                Debug.Assert(type.IsObject());
                builder.Append("dynamic"); // Not a keyword, does not require escaping.
                return true;
            }

            if (type.IsPredefinedType())
            {
                builder.Append(type.GetPredefinedTypeName()); // Not an identifier, does not require escaping.
                return true;
            }

            if (type.IsVoid())
            {
                builder.Append("void"); // Not an identifier, does not require escaping.
                return true;
            }

            return false;
        }
    }
}
