// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionOptions
    {
        public static readonly PerLanguageOption<int> ConditionalExpressionWrappingLength = new PerLanguageOption<int>(
            nameof(UseConditionalExpressionOptions),
            nameof(ConditionalExpressionWrappingLength), defaultValue: 120,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ConditionalExpressionWrappingLength)}"));
    }
}
