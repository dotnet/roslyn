// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [Flags]
    internal enum AttributeLocation : short
    {
        None = 0,

        // the order of these determine the order in which they are displayed in error messages when multiple locations are possible:
        Assembly = 1 << 0,
        Module = 1 << 1,
        Type = 1 << 2,
        Method = 1 << 3,
        Field = 1 << 4,
        Property = 1 << 5,
        Event = 1 << 6,
        Parameter = 1 << 7,
        Return = 1 << 8,
        TypeParameter = 1 << 9,

        // must be the last:
        Unknown = 1 << 10,
    }

    internal static class AttributeLocationExtensions
    {
        internal static string ToDisplayString(this AttributeLocation locations)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 1; i < (int)AttributeLocation.Unknown; i <<= 1)
            {
                if ((locations & (AttributeLocation)i) != 0)
                {
                    if (result.Length > 0)
                    {
                        result.Append(", ");
                    }

                    switch ((AttributeLocation)i)
                    {
                        case AttributeLocation.Assembly:
                            result.Append("assembly");
                            break;

                        case AttributeLocation.Module:
                            result.Append("module");
                            break;

                        case AttributeLocation.Type:
                            result.Append("type");
                            break;

                        case AttributeLocation.Method:
                            result.Append("method");
                            break;

                        case AttributeLocation.Field:
                            result.Append("field");
                            break;

                        case AttributeLocation.Property:
                            result.Append("property");
                            break;

                        case AttributeLocation.Event:
                            result.Append("event");
                            break;

                        case AttributeLocation.Return:
                            result.Append("return");
                            break;

                        case AttributeLocation.Parameter:
                            result.Append("param");
                            break;

                        case AttributeLocation.TypeParameter:
                            result.Append("typevar");
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(i);
                    }
                }
            }

            return result.ToString();
        }

        internal static AttributeLocation ToAttributeLocation(this SyntaxToken token)
        {
            // NOTE: to match dev10, we're using the value text, rather
            // than the actual text.  For example, "@return" is equivalent
            // to "return".
            var result = ToAttributeLocation(token.ValueText);

#if DEBUG
            var kind = SyntaxFacts.GetKeywordKind(token.ValueText);
            if (kind == SyntaxKind.None)
            {
                kind = SyntaxFacts.GetContextualKeywordKind(token.ValueText);
            }

            Debug.Assert(result == AttributeLocation.None ^ SyntaxFacts.IsAttributeTargetSpecifier(kind));
#endif

            return result;
        }

        internal static AttributeLocation ToAttributeLocation(this Syntax.InternalSyntax.SyntaxToken token)
        {
            // NOTE: to match dev10, we're using the value text, rather
            // than the actual text.  For example, "@return" is equivalent
            // to "return".
            return ToAttributeLocation(token.ValueText);
        }

        private static AttributeLocation ToAttributeLocation(string text)
        {
            switch (text)
            {
                case "assembly":
                    return AttributeLocation.Assembly;
                case "module":
                    return AttributeLocation.Module;
                case "type":
                    return AttributeLocation.Type;
                case "return":
                    return AttributeLocation.Return;
                case "method":
                    return AttributeLocation.Method;
                case "field":
                    return AttributeLocation.Field;
                case "event":
                    return AttributeLocation.Event;
                case "param":
                    return AttributeLocation.Parameter;
                case "property":
                    return AttributeLocation.Property;
                case "typevar":
                    return AttributeLocation.TypeParameter;
                default:
                    return AttributeLocation.None;
            }
        }
    }
}
