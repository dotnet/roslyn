// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public class FormattedClassification
    {
        private static readonly ImmutableDictionary<string, string> s_classificationPrefixToTestHelperMap = ImmutableDictionary<string, string>.Empty
            .Add("regex - ", "Regex.")
            .Add("json - ", "Json.")
            .Add("xml doc comment - ", "XmlDoc.")
            .Add("xml literal - ", "VBXml");

        public string ClassificationName { get; }
        public string Text { get; }

        private FormattedClassification() { }

        public FormattedClassification(string text, string classificationName)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            ClassificationName = classificationName ?? throw new ArgumentNullException(nameof(classificationName));
        }

        public override bool Equals(object obj)
        {
            if (obj is FormattedClassification other)
            {
                return this.ClassificationName == other.ClassificationName
                    && this.Text == other.Text;
            }

            return false;
        }

        public override int GetHashCode()
            => ClassificationName.GetHashCode() ^ Text.GetHashCode();

        public override string ToString()
        {
            foreach (var kvp in s_classificationPrefixToTestHelperMap)
            {
                if (ClassificationName.StartsWith(kvp.Key))
                {
                    var remainder = ClassificationName[kvp.Key.Length..];
                    var parts = remainder.Split(' ');
                    var type = string.Join("", parts.Select(Capitalize));
                    return kvp.Value + $"{type}(\"{Text}\")";
                }
            }

            switch (ClassificationName)
            {
                case "punctuation":
                    switch (Text)
                    {
                        case "(":
                            return "Punctuation.OpenParen";
                        case ")":
                            return "Punctuation.CloseParen";
                        case "[":
                            return "Punctuation.OpenBracket";
                        case "]":
                            return "Punctuation.CloseBracket";
                        case "{":
                            return "Punctuation.OpenCurly";
                        case "}":
                            return "Punctuation.CloseCurly";
                        case ";":
                            return "Punctuation.Semicolon";
                        case ":":
                            return "Punctuation.Colon";
                        case ",":
                            return "Punctuation.Comma";
                        case "..":
                            return "Punctuation.DotDot";
                        case "<":
                            return "Punctuation.OpenAngle";
                        case ">":
                            return "Punctuation.CloseAngle";
                    }

                    goto default;

                case "operator":
                case "operator - overloaded":
                    var operatorTypeName = ClassificationName switch
                    {
                        "operator" => "Operators",
                        "operator - overloaded" => "OverloadedOperators",
                        _ => throw ExceptionUtilities.Unreachable(),
                    };

                    switch (Text)
                    {
                        case "&&":
                            return $"{operatorTypeName}.AmpersandAmpersand";
                        case "&=":
                            return $"{operatorTypeName}.AmpersandEquals";
                        case "*":
                            return $"{operatorTypeName}.Asterisk";
                        case "*=":
                            return $"{operatorTypeName}.AsteriskEquals";
                        case "|":
                            return $"{operatorTypeName}.Bar";
                        case "||":
                            return $"{operatorTypeName}.BarBar";
                        case "|=":
                            return $"{operatorTypeName}.BarEquals";
                        case "^":
                            return $"{operatorTypeName}.Caret";
                        case "^=":
                            return $"{operatorTypeName}.CaretEquals";
                        case ":":
                            return $"{operatorTypeName}.Colon";
                        case "::":
                            return $"{operatorTypeName}.ColonColon";
                        case ":=":
                            return $"{operatorTypeName}.ColonEquals";
                        case ".":
                            return $"{operatorTypeName}.Dot";
                        case "=":
                            return $"{operatorTypeName}.Equals";
                        case "==":
                            return $"{operatorTypeName}.EqualsEquals";
                        case "=>":
                            return $"{operatorTypeName}.EqualsGreaterThan";
                        case "!":
                            return $"{operatorTypeName}.Exclamation";
                        case "!=":
                            return $"{operatorTypeName}.ExclamationEquals";
                        case ">":
                            return $"{operatorTypeName}.GreaterThan";
                        case ">=":
                            return $"{operatorTypeName}.GreaterThanEquals";
                        case ">>":
                            return $"{operatorTypeName}.GreaterThanGreaterThan";
                        case ">>>":
                            return $"{operatorTypeName}.GreaterThanGreaterThanGreaterThan";
                        case ">>=":
                            return $"{operatorTypeName}.GreaterThanGreaterThanEquals";
                        case ">>>=":
                            return $"{operatorTypeName}.GreaterThanGreaterThanGreaterThanEquals";
                        case "<":
                            return $"{operatorTypeName}.LessThan";
                        case "<=":
                            return $"{operatorTypeName}.LessThanEquals";
                        case "<>":
                            return $"{operatorTypeName}.LessThanGreaterThan";
                        case "<<":
                            return $"{operatorTypeName}.LessThanLessThan";
                        case "<<=":
                            return $"{operatorTypeName}.LessThanLessThanEquals";
                        case "-":
                            return $"{operatorTypeName}.Minus";
                        case "-=":
                            return $"{operatorTypeName}.MinusEquals";
                        case "->":
                            return $"{operatorTypeName}.MinusGreaterThan";
                        case "--":
                            return $"{operatorTypeName}.MinusMinus";
                        case "%":
                            return $"{operatorTypeName}.Percent";
                        case "%=":
                            return $"{operatorTypeName}.PercentEquals";
                        case "+":
                            return $"{operatorTypeName}.Plus";
                        case "+=":
                            return $"{operatorTypeName}.PlusEquals";
                        case "++":
                            return $"{operatorTypeName}.PlusPlus";
                        case "?":
                            return $"{operatorTypeName}.QuestionMark";
                        case "??=":
                            return $"{operatorTypeName}.QuestionQuestionEquals";
                        case "/":
                            return $"{operatorTypeName}.Slash";
                        case "/=":
                            return $"{operatorTypeName}.SlashEquals";
                        case "~":
                            return $"{operatorTypeName}.Tilde";
                    }

                    goto default;

                case "keyword - control":
                    return $"ControlKeyword(\"{Text}\")";

                case "static symbol":
                    return $"Static(\"{Text}\")";

                case "string - verbatim":
                    return $"Verbatim(\"{Text}\")";

                case "string - escape character":
                    return $"Escape(\"{Text}\")";

                default:
                    var trimmedClassification = ClassificationName;
                    if (trimmedClassification.EndsWith(" name"))
                    {
                        trimmedClassification = trimmedClassification[..^" name".Length];
                    }

                    return $"{string.Join("", trimmedClassification.Split(' ').Select(Capitalize))}(\"{Text}\")";
            }
        }

        private static string Capitalize(string val)
            => char.ToUpperInvariant(val[0]) + val[1..];
    }
}
