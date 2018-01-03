// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public sealed class RegexClassificationTypes
        {
            [DebuggerStepThrough]
            public Tuple<string, string> Anchor(string value) => Tuple.Create(value, ClassificationTypeNames.RegexAnchor);

            [DebuggerStepThrough]
            public Tuple<string, string> Grouping(string value) => Tuple.Create(value, ClassificationTypeNames.RegexGrouping);

            [DebuggerStepThrough]
            public Tuple<string, string> Escape(string value) => Tuple.Create(value, ClassificationTypeNames.RegexEscape);

            [DebuggerStepThrough]
            public Tuple<string, string> Alternation(string value) => Tuple.Create(value, ClassificationTypeNames.RegexAlternation);

            [DebuggerStepThrough]
            public Tuple<string, string> CharacterClass(string value) => Tuple.Create(value, ClassificationTypeNames.RegexCharacterClass);

            [DebuggerStepThrough]
            public Tuple<string, string> Text(string value) => Tuple.Create(value, ClassificationTypeNames.RegexText);

            [DebuggerStepThrough]
            public Tuple<string, string> Quantifier(string value) => Tuple.Create(value, ClassificationTypeNames.RegexQuantifier);

            [DebuggerStepThrough]
            public Tuple<string, string> Comment(string value) => Tuple.Create(value, ClassificationTypeNames.RegexComment);
        }
    }
}
