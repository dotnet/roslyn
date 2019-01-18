// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public class FormattedClassification
    {
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
        {
            return ClassificationName.GetHashCode() ^ Text.GetHashCode();
        }

        public override string ToString()
        {
            if (ClassificationName.StartsWith("regex"))
            {
                var remainder = ClassificationName.Substring("regex - ".Length);
                var parts = remainder.Split(' ');
                var type = string.Join("", parts.Select(Capitalize));
                return "Regex." + $"{type}(\"{Text}\")";
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
                    }
                    goto default;

                case "operator":
                    switch (Text)
                    {
                        case "=":
                            return "Operators.Equals";
                        case "++":
                            return "Operators.PlusPlus";
                    }
                    goto default;

                default:
                    return $"{Capitalize(ClassificationName)}(\"{Text}\")";
            }
        }

        private static string Capitalize(string val)
            => char.ToUpperInvariant(val[0]) + val.Substring(1);
    }
}
