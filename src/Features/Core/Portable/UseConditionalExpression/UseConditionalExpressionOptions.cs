// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionOptions
    {
        public static readonly PerLanguageOption2<int> ConditionalExpressionWrappingLength = new PerLanguageOption2<int>(
            nameof(UseConditionalExpressionOptions),
            nameof(ConditionalExpressionWrappingLength), defaultValue: 120,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ConditionalExpressionWrappingLength)}"));
    }
}
