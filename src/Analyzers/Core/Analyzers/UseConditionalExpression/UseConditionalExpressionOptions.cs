// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionOptions
    {
        public static readonly PerLanguageOption2<int> ConditionalExpressionWrappingLength = new(
            nameof(UseConditionalExpressionOptions),
            nameof(ConditionalExpressionWrappingLength), defaultValue: 120,
            storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ConditionalExpressionWrappingLength)}"));
    }
}
