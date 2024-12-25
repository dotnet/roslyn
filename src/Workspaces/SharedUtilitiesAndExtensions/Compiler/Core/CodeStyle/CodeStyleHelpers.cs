// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class CodeStyleHelpers
{
    public static bool TryParseStringEditorConfigCodeStyleOption(string arg, CodeStyleOption2<string> defaultValue, [NotNullWhen(true)] out CodeStyleOption2<string>? option)
    {
        if (TryGetCodeStyleValueAndOptionalNotification(
                arg, defaultValue.Notification, out var value, out var notification))
        {
            option = new CodeStyleOption2<string>(value, notification);
            return true;
        }

        option = null;
        return false;
    }

    public static bool TryParseBoolEditorConfigCodeStyleOption(string arg, CodeStyleOption2<bool> defaultValue, out CodeStyleOption2<bool> option)
    {
        if (TryGetCodeStyleValueAndOptionalNotification(
                arg, defaultValue.Notification, out var value, out var notification))
        {
            // First value has to be true or false.  Anything else is unsupported.
            if (bool.TryParse(value, out var isEnabled))
            {
                option = new CodeStyleOption2<bool>(isEnabled, notification);
                return true;
            }
        }

        option = defaultValue;
        return false;
    }

    /// <summary>
    /// Given an editor-config code-style-option, gives back the core value part of the 
    /// option.  For example, if the option is "true:error" or "true" then "true" will be returned
    /// in <paramref name="value"/>.
    /// </summary>
    public static bool TryGetCodeStyleValue(
        string arg, [NotNullWhen(true)] out string? value)
        => TryGetCodeStyleValueAndOptionalNotification(arg, defaultNotification: NotificationOption2.None, out value, out _);

    /// <summary>
    /// Given an editor-config code-style-option, gives back the constituent parts of the 
    /// option.  For example, if the option is "true:error" then "true" will be returned
    /// in <paramref name="value"/> and <see cref="NotificationOption2.Error"/> will be returned
    /// in <paramref name="notification"/>.  Note that users are allowed to not provide
    /// a NotificationOption, so <paramref name="notification"/> will default to <paramref name="defaultNotification"/>.
    /// </summary>
    public static bool TryGetCodeStyleValueAndOptionalNotification(
        string arg, NotificationOption2 defaultNotification, [NotNullWhen(true)] out string? value, [NotNullWhen(true)] out NotificationOption2 notification)
    {
        var firstColonIndex = arg.IndexOf(':');

        // We allow a single value to be provided without an explicit notification.
        if (firstColonIndex == -1)
        {
            value = arg.Trim();
            notification = defaultNotification;
            return true;
        }

        var secondColonIndex = arg.IndexOf(':', firstColonIndex + 1);
        if (secondColonIndex == -1)
        {
            // If we have two args, then the second must be a notification option.  If 
            // it isn't, then this isn't a valid code style option at all.
            if (TryParseNotification(arg.AsSpan(firstColonIndex + 1), out var localNotification))
            {
                var firstValue = arg[..firstColonIndex];
                value = firstValue.Trim();
                notification = localNotification.WithIsExplicitlySpecified(true);
                return true;
            }
        }

        // We only support 0 or 1 args.  Anything else can't be parsed properly.
        value = null;
        notification = default;
        return false;
    }

    private static bool TryParseNotification(ReadOnlySpan<char> value, out NotificationOption2 notification)
    {
        switch (value.Trim())
        {
            case EditorConfigSeverityStrings.None:
                notification = NotificationOption2.None;
                return true;

            case EditorConfigSeverityStrings.Refactoring:
            case EditorConfigSeverityStrings.Silent:
                notification = NotificationOption2.Silent;
                return true;

            case EditorConfigSeverityStrings.Suggestion: notification = NotificationOption2.Suggestion; return true;
            case EditorConfigSeverityStrings.Warning: notification = NotificationOption2.Warning; return true;
            case EditorConfigSeverityStrings.Error: notification = NotificationOption2.Error; return true;
        }

        notification = NotificationOption2.Silent;
        return false;
    }

    public static Option2<CodeStyleOption2<T>> CreateEditorConfigOption<T>(
        this ImmutableArray<IOption2>.Builder optionsBuilder,
        string name,
        CodeStyleOption2<T> defaultValue,
        OptionGroup group,
        string? languageName = null,
        Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
    {
        var option = new Option2<CodeStyleOption2<T>>(name, defaultValue, group, languageName, isEditorConfigOption: true, serializer: (serializerFactory ?? EditorConfigValueSerializer.CodeStyle).Invoke(defaultValue));
        optionsBuilder.Add(option);
        return option;
    }

    public static Option2<T> CreateEditorConfigOption<T>(
        this ImmutableArray<IOption2>.Builder optionsBuilder,
        string name,
        T defaultValue,
        OptionGroup group,
        EditorConfigValueSerializer<T>? serializer = null)
    {
        var option = new Option2<T>(name, defaultValue, group, languageName: null, isEditorConfigOption: true, serializer: serializer);
        optionsBuilder.Add(option);
        return option;
    }

    public static PerLanguageOption2<CodeStyleOption2<T>> CreatePerLanguageEditorConfigOption<T>(
        this ImmutableArray<IOption2>.Builder optionsBuilder,
        string name,
        CodeStyleOption2<T> defaultValue,
        OptionGroup group,
        Func<CodeStyleOption2<T>, EditorConfigValueSerializer<CodeStyleOption2<T>>>? serializerFactory = null)
    {
        var option = new PerLanguageOption2<CodeStyleOption2<T>>(name, defaultValue, group, isEditorConfigOption: true, serializer: (serializerFactory ?? EditorConfigValueSerializer.CodeStyle).Invoke(defaultValue));
        optionsBuilder.Add(option);
        return option;
    }

    private static readonly CodeStyleOption2<UnusedValuePreference> s_preferNoneUnusedValuePreference =
        new(default, NotificationOption2.None);

    private static readonly BidirectionalMap<string, UnusedValuePreference> s_unusedExpressionAssignmentPreferenceMap =
        new(
        [
            KeyValuePairUtil.Create("discard_variable", UnusedValuePreference.DiscardVariable),
            KeyValuePairUtil.Create("unused_local_variable", UnusedValuePreference.UnusedLocalVariable),
        ]);

    internal static EditorConfigValueSerializer<CodeStyleOption2<UnusedValuePreference>> GetUnusedValuePreferenceSerializer(CodeStyleOption2<UnusedValuePreference> defaultValue)
        => new(parseValue: str => ParseUnusedExpressionAssignmentPreference(str, defaultValue),
               serializeValue: value => GetUnusedExpressionAssignmentPreferenceEditorConfigString(value, defaultValue));

    private static Optional<CodeStyleOption2<UnusedValuePreference>> ParseUnusedExpressionAssignmentPreference(
        string optionString,
        CodeStyleOption2<UnusedValuePreference> defaultCodeStyleOption)
    {
        if (TryGetCodeStyleValueAndOptionalNotification(optionString,
                defaultCodeStyleOption.Notification, out var value, out var notification))
        {
            return new CodeStyleOption2<UnusedValuePreference>(
                s_unusedExpressionAssignmentPreferenceMap.GetValueOrDefault(value), notification);
        }

        return s_preferNoneUnusedValuePreference;
    }

    private static string GetUnusedExpressionAssignmentPreferenceEditorConfigString(CodeStyleOption2<UnusedValuePreference> option, CodeStyleOption2<UnusedValuePreference> defaultValue)
    {
        Debug.Assert(s_unusedExpressionAssignmentPreferenceMap.ContainsValue(option.Value));
        var value = s_unusedExpressionAssignmentPreferenceMap.GetKeyOrDefault(option.Value) ?? s_unusedExpressionAssignmentPreferenceMap.GetKeyOrDefault(defaultValue.Value);
        return $"{value}{GetEditorConfigStringNotificationPart(option, defaultValue)}";
    }

    internal static string GetEditorConfigStringNotificationPart<T>(CodeStyleOption2<T> option, CodeStyleOption2<T> defaultValue)
        => option.Notification != defaultValue.Notification
            ? $":{option.Notification.ToEditorConfigString()}"
            : string.Empty;
}
