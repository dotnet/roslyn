// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
            switch (ClassificationName)
            {
                case "punctuation":
                    switch (Text)
                    {
                        case "(":
                            return "Punctation.OpenParen";
                        case ")":
                            return "Punctation.CloseParen";
                        case ";":
                            return "Punctation.Semicolon";
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
                    }
                    goto default;

                default:
                    return $"{char.ToUpperInvariant(ClassificationName[0])}{ClassificationName.Substring(1)}(\"{Text}\")";
            }
        }
    }
}
