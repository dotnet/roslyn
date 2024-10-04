// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle;

/// <summary>
/// Internal representation of a code style option value. Should be used throughout Roslyn.
/// The internal values are translated to the public ones (ICodeStyleOption) at the public entry points.
/// </summary>
internal interface ICodeStyleOption2
{
    XElement ToXElement();
    object? Value { get; }
    NotificationOption2 Notification { get; }
    ICodeStyleOption2 WithValue(object value);
    ICodeStyleOption2 WithNotification(NotificationOption2 notification);

    /// <summary>
    /// Creates a new <see cref="ICodeStyleOption2"/> from a specified <paramref name="element"/>.
    /// </summary>
    /// <exception cref="Exception">
    /// The type of the serialized data does not match the type of <see cref="ICodeStyleOption2.Value"/> or the format of the serialized data is invalid.
    /// </exception>
    ICodeStyleOption2 FromXElement(XElement element);
}

internal static class CodeStyleOption2
{
    /// <remarks>
    /// When user preferences are not yet set for a style, we fall back to the default value.
    /// One such default(s), is that the feature is turned on, so that codegen consumes it,
    /// but with silent enforcement, so that the user is not prompted about their usage.
    /// </remarks>
    public static readonly CodeStyleOption2<bool> TrueWithSilentEnforcement = new(value: true, notification: NotificationOption2.Silent);
    public static readonly CodeStyleOption2<bool> FalseWithSilentEnforcement = new(value: false, notification: NotificationOption2.Silent);
    public static readonly CodeStyleOption2<bool> TrueWithSuggestionEnforcement = new(value: true, notification: NotificationOption2.Suggestion);
    public static readonly CodeStyleOption2<bool> FalseWithSuggestionEnforcement = new(value: false, notification: NotificationOption2.Suggestion);

    /// <summary>
    /// Use singletons for most common values.
    /// </summary>
    public static CodeStyleOption2<bool> GetCodeStyle(bool value, NotificationOption2 notification)
        => (value, notification.Severity) switch
        {
            (true, ReportDiagnostic.Hidden) => TrueWithSilentEnforcement,
            (true, ReportDiagnostic.Info) => TrueWithSuggestionEnforcement,
            (false, ReportDiagnostic.Hidden) => FalseWithSilentEnforcement,
            (false, ReportDiagnostic.Info) => FalseWithSuggestionEnforcement,
            _ => new(value, notification)
        };
}

/// <summary>
/// Represents a code style option and an associated notification option.  Supports
/// being instantiated with T as a <see cref="bool"/> or an <c>enum type</c>.
/// 
/// CodeStyleOption also has some basic support for migration a <see cref="bool"/> option
/// forward to an <c>enum type</c> option.  Specifically, if a previously serialized
/// bool-CodeStyleOption is then deserialized into an enum-CodeStyleOption then 'false' 
/// values will be migrated to have the 0-value of the enum, and 'true' values will be
/// migrated to have the 1-value of the enum.
/// 
/// Similarly, enum-type code options will serialize out in a way that is compatible with 
/// hosts that expect the value to be a boolean.  Specifically, if the enum value is 0 or 1
/// then those values will write back as false/true.
/// </summary>
[DataContract]
internal sealed partial class CodeStyleOption2<T>(T value, NotificationOption2 notification) : ICodeStyleOption2, IEquatable<CodeStyleOption2<T>?>
{
    public static readonly CodeStyleOption2<T> Default = new(default!, NotificationOption2.Silent);

    private const int SerializationVersion = 1;

    private const string XmlElement_CodeStyleOption = "CodeStyleOption";
    private const string XmlAttribute_SerializationVersion = "SerializationVersion";
    private const string XmlAttribute_Type = "Type";
    private const string XmlAttribute_Value = "Value";
    private const string XmlAttribute_DiagnosticSeverity = "DiagnosticSeverity";

    [DataMember(Order = 0)]
    public T Value { get; } = value;

    [DataMember(Order = 1)]
    public NotificationOption2 Notification { get; } = notification;

    object? ICodeStyleOption2.Value => this.Value;
    ICodeStyleOption2 ICodeStyleOption2.WithValue(object value) => WithValue((T)value);
    ICodeStyleOption2 ICodeStyleOption2.WithNotification(NotificationOption2 notification) => new CodeStyleOption2<T>(Value, notification);

    public CodeStyleOption2<T> WithValue(T value)
    {
        if (typeof(T) == typeof(bool))
        {
            var boolValue = (bool)(object)value!;
            if (boolValue == (bool)(object)Value!)
            {
                return this;
            }

            return (CodeStyleOption2<T>)(object)CodeStyleOption2.GetCodeStyle(boolValue, Notification);
        }

        return EqualityComparer<T>.Default.Equals(value, Value) ? this : new CodeStyleOption2<T>(value, Notification);
    }

    private int EnumValueAsInt32 => (int)(object)Value!;

    public XElement ToXElement()
        => new(XmlElement_CodeStyleOption, // Ensure that we use "CodeStyleOption" as the name for back compat.
            new XAttribute(XmlAttribute_SerializationVersion, SerializationVersion),
            new XAttribute(XmlAttribute_Type, GetTypeNameForSerialization()),
            new XAttribute(XmlAttribute_Value, GetValueForSerialization()),
            new XAttribute(XmlAttribute_DiagnosticSeverity, Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));

    private object GetValueForSerialization()
    {
        if (typeof(T) == typeof(string))
        {
            return Value!;
        }
        else if (typeof(T) == typeof(bool))
        {
            return Value!;
        }
        else if (IsZeroOrOneValueOfEnum())
        {
            return EnumValueAsInt32 == 1;
        }
        else
        {
            return EnumValueAsInt32;
        }
    }

    private string GetTypeNameForSerialization()
    {
        if (typeof(T) == typeof(string))
        {
            return nameof(String);
        }

        if (typeof(T) == typeof(bool) || IsZeroOrOneValueOfEnum())
        {
            return nameof(Boolean);
        }
        else
        {
            return nameof(Int32);
        }
    }

    private bool IsZeroOrOneValueOfEnum()
    {
        var intVal = EnumValueAsInt32;
        return intVal is 0 or 1;
    }

    ICodeStyleOption2 ICodeStyleOption2.FromXElement(XElement element)
        => FromXElement(element);

    public static CodeStyleOption2<T> FromXElement(XElement element)
    {
        var typeAttribute = element.Attribute(XmlAttribute_Type);
        var valueAttribute = element.Attribute(XmlAttribute_Value);
        var severityAttribute = element.Attribute(XmlAttribute_DiagnosticSeverity);
        var version = (int?)element.Attribute(XmlAttribute_SerializationVersion);

        if (typeAttribute == null || valueAttribute == null || severityAttribute == null)
        {
            // data from storage is corrupt, or nothing has been stored yet.
            return Default;
        }

        if (version != SerializationVersion)
        {
            return Default;
        }

        var parser = GetParser(typeAttribute.Value);
        var value = parser(valueAttribute.Value);
        var severity = (DiagnosticSeverity)Enum.Parse(typeof(DiagnosticSeverity), severityAttribute.Value);

        return new CodeStyleOption2<T>(value, severity switch
        {
            DiagnosticSeverity.Hidden => NotificationOption2.Silent,
            DiagnosticSeverity.Info => NotificationOption2.Suggestion,
            DiagnosticSeverity.Warning => NotificationOption2.Warning,
            DiagnosticSeverity.Error => NotificationOption2.Error,
            _ => throw new ArgumentException(nameof(element)),
        });
    }

    private static Func<string, T> GetParser(string type)
        => type switch
        {
            nameof(Boolean) =>
                // Try to map a boolean value.  Either map it to true/false if we're a 
                // CodeStyleOption<bool> or map it to the 0 or 1 value for an enum if we're
                // a CodeStyleOption<SomeEnumType>.
                v => Convert(bool.Parse(v)),
            nameof(Int32) => v => Convert(int.Parse(v)),
            nameof(String) => v => (T)(object)v,
            _ => throw new ArgumentException(nameof(type)),
        };

    private static T Convert(bool b)
    {
        // If we had a bool and we wanted a bool, then just return this value.
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)b;
        }

        // Map booleans to the 1/0 value of the enum.
        return b ? (T)(object)1 : (T)(object)0;
    }

    private static T Convert(int i)
    {
        // We got an int, but we wanted a bool.  Map 0 to false, 1 to true, and anything else to default.
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)(i == 1);
        }

        // If had an int and we wanted an enum, then just return this value.
        return (T)(object)(i);
    }

    public bool Equals(CodeStyleOption2<T>? other)
    {
        return other is not null
            && EqualityComparer<T>.Default.Equals(Value, other.Value)
            && Notification == other.Notification;
    }

    public override bool Equals(object? obj)
        => obj is CodeStyleOption2<T> option &&
           Equals(option);

    public override int GetHashCode()
        => unchecked((Notification.GetHashCode() * (int)0xA5555529) + EqualityComparer<T>.Default.GetHashCode(Value!));
}
