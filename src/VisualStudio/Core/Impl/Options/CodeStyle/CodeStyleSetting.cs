// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

/// <summary>
/// Projects a legacy option into USX without changing the legacy option's persistence contract.
/// Both halves of a code-style option and individual bits of formatting flags share one option key.
/// </summary>
internal sealed class CodeStyleSetting(string moniker, IOption2 option, string language, string description, int? flag = null)
{
    public string Moniker { get; } = moniker;
    public OptionKey2 Key { get; } = new(option, option.IsPerLanguage ? language : null);
    public string Description { get; } = description;
    public int? Flag { get; } = flag;
    internal IReadOnlyList<EnumChoice>? Choices { get; set; }

    internal static string ToUnifiedScalarMoniker(string configName)
        => string.Concat(configName.Split('_').Select((part, index) => index == 0 || part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part.Substring(1)));

    public object Read(IOptionsReader reader, bool severity = false)
    {
        var value = CodeStyleSettingsProvider.Read(reader, Key);
        if (value is ICodeStyleOption2 style)
            return severity ? SerializeSeverity(style.Notification) : SerializePreference(style);

        if (Flag is int flag)
            return (Convert.ToInt32(value, CultureInfo.InvariantCulture) & flag) == flag;

        return value.GetType().IsEnum ? value.ToString()! : value;
    }

    public object Update(object current, object value, bool severity = false)
    {
        if (value is null)
            throw new ArgumentException(nameof(value));
        if (current is ICodeStyleOption2 style)
        {
            if (value is not string text)
                throw new ArgumentException(nameof(value));

            if (severity)
                return style.WithNotification(ParseSeverity(text));

            if (!GetChoices().Any(choice => choice.Moniker == text))
                throw new ArgumentException(nameof(value));
            if (!Key.Option.Definition.Serializer.TryParse(text + ":silent", out var parsed) || parsed is not ICodeStyleOption2 preference)
                throw new ArgumentException(nameof(value));

            // Never recreate the compound option from its default: that would reset its other half.
            return style.WithValue(preference.Value!).WithNotification(style.Notification);
        }

        if (Flag is int flag && value is bool enabled)
        {
            var bits = Convert.ToInt32(current, CultureInfo.InvariantCulture);
            return Enum.ToObject(current.GetType(), enabled ? bits | flag : bits & ~flag);
        }

        if (current.GetType().IsEnum && value is string enumValue)
        {
            if (!GetChoices().Any(choice => choice.Moniker == enumValue))
                throw new ArgumentException(nameof(value));
            var parsed = Enum.Parse(current.GetType(), enumValue);
            if (!Enum.IsDefined(current.GetType(), parsed))
                throw new ArgumentException(nameof(value));
            return parsed;
        }

        if (value.GetType() != current.GetType())
            throw new ArgumentException(nameof(value));
        return value;
    }

    public IReadOnlyList<EnumChoice> GetChoices()
    {
        if (Choices is not null)
            return Choices;
        if (Key.Option.DefaultValue is not ICodeStyleOption2 style)
            return Enum.GetNames(Key.Option.Type).Select(name => new EnumChoice(name, name)).ToArray();

        var type = style.Value!.GetType();
        var values = type == typeof(bool) ? new object[] { true, false } : Enum.GetValues(type).Cast<object>();
        return values.Select(value => new EnumChoice(SerializePreference(style.WithValue(value)), value.ToString()!)).ToArray();
    }

    internal string SerializePreference(ICodeStyleOption2 style)
        => Key.Option.Definition.Serializer.Serialize(style).Split(':')[0];

    internal static string SerializeSeverity(NotificationOption2 notification)
        => notification.Severity switch
        {
            Microsoft.CodeAnalysis.ReportDiagnostic.Suppress => "none",
            Microsoft.CodeAnalysis.ReportDiagnostic.Hidden => "silent",
            Microsoft.CodeAnalysis.ReportDiagnostic.Info => "suggestion",
            Microsoft.CodeAnalysis.ReportDiagnostic.Warn => "warning",
            Microsoft.CodeAnalysis.ReportDiagnostic.Error => "error",
            _ => "silent",
        };

    internal static NotificationOption2 ParseSeverity(string severity)
        => severity switch
        {
            "none" => NotificationOption2.None,
            "silent" => NotificationOption2.Silent,
            "suggestion" => NotificationOption2.Suggestion,
            "warning" => NotificationOption2.Warning,
            "error" => NotificationOption2.Error,
            _ => throw new ArgumentException(nameof(severity)),
        };
}
