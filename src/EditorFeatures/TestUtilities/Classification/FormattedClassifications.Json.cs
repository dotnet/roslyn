// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification;

public static partial class FormattedClassifications
{
    public static class Json
    {
        [DebuggerStepThrough]
        public static FormattedClassification Array(string value) => New(value, ClassificationTypeNames.JsonArray);

        [DebuggerStepThrough]
        public static FormattedClassification Object(string value) => New(value, ClassificationTypeNames.JsonObject);

        [DebuggerStepThrough]
        public static FormattedClassification PropertyName(string value) => New(value, ClassificationTypeNames.JsonPropertyName);

        [DebuggerStepThrough]
        public static FormattedClassification Punctuation(string value) => New(value, ClassificationTypeNames.JsonPunctuation);

        [DebuggerStepThrough]
        public static FormattedClassification Number(string value) => New(value, ClassificationTypeNames.JsonNumber);

        [DebuggerStepThrough]
        public static FormattedClassification Operator(string value) => New(value, ClassificationTypeNames.JsonOperator);

        [DebuggerStepThrough]
        public static FormattedClassification Keyword(string value) => New(value, ClassificationTypeNames.JsonKeyword);

        [DebuggerStepThrough]
        public static FormattedClassification ConstructorName(string value) => New(value, ClassificationTypeNames.JsonConstructorName);

        [DebuggerStepThrough]
        public static FormattedClassification Comment(string value) => New(value, ClassificationTypeNames.JsonComment);

        [DebuggerStepThrough]
        public static FormattedClassification Text(string value) => New(value, ClassificationTypeNames.JsonText);

        [DebuggerStepThrough]
        public static FormattedClassification String(string value) => New(value, ClassificationTypeNames.JsonString);
    }
}
