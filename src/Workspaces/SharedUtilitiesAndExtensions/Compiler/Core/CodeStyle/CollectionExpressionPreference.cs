// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Shared.CodeStyle;

internal enum CollectionExpressionPreference
{
    Never,
    WhenTypesExactlyMatch,
    WhenTypesLooselyMatch,
}

internal static class CollectionExpressionPreferenceUtilities
{
    private const string never = "never";
    private const string when_types_exactly_match = "when_types_exactly_match";
    private const string when_types_loosely_match = "when_types_loosely_match";

    // Default to beginning_of_line if we don't know the value.
    public static string GetEditorConfigString(
        CodeStyleOption2<CollectionExpressionPreference> value,
        CodeStyleOption2<CollectionExpressionPreference> defaultValue)
    {
        var prefix = value.Value switch
        {
            CollectionExpressionPreference.Never => never,
            CollectionExpressionPreference.WhenTypesExactlyMatch => when_types_exactly_match,
            _ => when_types_loosely_match,
        };

        return $"{prefix}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue)}";
    }

    public static Optional<CodeStyleOption2<CollectionExpressionPreference>> Parse(
        string optionString, CodeStyleOption2<CollectionExpressionPreference> defaultValue)
    {
        if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                optionString, defaultValue.Notification, out var value, out var notification))
        {
            switch (value)
            {
                case "false" or never: return new CodeStyleOption2<CollectionExpressionPreference>(CollectionExpressionPreference.Never, notification);
                case "true" or when_types_exactly_match: return new CodeStyleOption2<CollectionExpressionPreference>(CollectionExpressionPreference.WhenTypesExactlyMatch, notification);
                case when_types_loosely_match: return new CodeStyleOption2<CollectionExpressionPreference>(CollectionExpressionPreference.WhenTypesLooselyMatch, notification);
            }
        }

        return defaultValue;
    }
}
