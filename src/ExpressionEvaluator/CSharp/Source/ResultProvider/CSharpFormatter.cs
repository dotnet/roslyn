// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed partial class CSharpFormatter : Formatter
    {
        public CSharpFormatter()
            : base(defaultFormat: "{{{0}}}", nullString: "null", thisString: "this", hostValueNotFoundString: Resources.HostValueNotFound)
        {
        }

        internal override bool IsValidIdentifier(string name)
        {
            return SyntaxFacts.IsValidIdentifier(name);
        }

        internal override bool IsIdentifierPartCharacter(char c)
        {
            return SyntaxFacts.IsIdentifierPartCharacter(c);
        }

        internal override bool IsPredefinedType(Type type)
        {
            return type.IsPredefinedType();
        }

        internal override bool IsWhitespace(char c)
        {
            return SyntaxFacts.IsWhitespace(c);
        }

        // TODO: https://github.com/dotnet/roslyn/issues/37536 
        // This parsing is imprecise and may result in bad expressions.
        internal override string TrimAndGetFormatSpecifiers(string expression, out ReadOnlyCollection<string> formatSpecifiers)
        {
            expression = RemoveComments(expression);
            expression = RemoveFormatSpecifiers(expression, out formatSpecifiers);
            return RemoveLeadingAndTrailingContent(expression, 0, expression.Length, IsWhitespace, ch => ch == ';' || IsWhitespace(ch));
        }

        private static string RemoveComments(string expression)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            var inMultilineComment = false;
            int length = expression.Length;

            // Workaround for https://dev.azure.com/devdiv/DevDiv/_workitems/edit/847849
            // Do not remove any comments that might be in a string. 
            // This won't work when there are quotes in the comment, but that's not that common.
            int lastQuote = expression.LastIndexOf('"') + 1;
            builder.Append(expression, 0, lastQuote);

            for (int i = lastQuote; i < length; i++)
            {
                var ch = expression[i];
                if (inMultilineComment)
                {
                    if (ch == '*' && i + 1 < length && expression[i + 1] == '/')
                    {
                        i++;
                        inMultilineComment = false;
                    }
                }
                else
                {
                    if (ch == '/' && i + 1 < length)
                    {
                        var next = expression[i + 1];
                        if (next == '*')
                        {
                            i++;
                            inMultilineComment = true;
                            continue;
                        }
                        else if (next == '/')
                        {
                            // Ignore remainder of string.
                            break;
                        }
                    }
                    builder.Append(ch);
                }
            }
            if (builder.Length < length)
            {
                expression = builder.ToString();
            }
            pooledBuilder.Free();
            return expression;
        }
    }
}
