// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal interface ICodeStyleOption
    {
        XElement ToXElement();
    }

    /// <summary>
    /// Represents a code style option and an associated notification option.  Supports
    /// being instantiated with T as a <see cref="bool"/> or an <code>enum type</code>.
    /// 
    /// CodeStyleOption also has some basic support for migration a <see cref="bool"/> option
    /// forward to an <code>enum type</code> option.  Specifically, if a previously serialized
    /// bool-CodeStyleOption is then deserialized into an enum-CodeStyleOption then 'false' 
    /// values will be migrated to have the 0-value of the enum, and 'true' values will be
    /// migrated to have the 1-value of the enum.
    /// </summary>
    public class CodeStyleOption<T> : ICodeStyleOption, IEquatable<CodeStyleOption<T>>
    {
        public static CodeStyleOption<T> Default => new CodeStyleOption<T>(default(T), NotificationOption.None);

        private const int SerializationVersion = 1;

        public CodeStyleOption(T value, NotificationOption notification)
        {
            Value = value;
            Notification = notification;
        }

        public T Value { get; set; }

        public NotificationOption Notification { get; set; }

        public XElement ToXElement() =>
            new XElement(nameof(CodeStyleOption<T>), // `nameof()` returns just "CodeStyleOption"
                new XAttribute(nameof(SerializationVersion), SerializationVersion),
                new XAttribute("Type", GetTypeNameForSerialization()),
                new XAttribute(nameof(Value), GetValueForSerialization()),
                new XAttribute(nameof(DiagnosticSeverity), Notification.Value));

        private object GetValueForSerialization()
        {
            if (typeof(T) == typeof(bool))
            {
                return Value;
            }
            else
            {
                return (int)(object)Value;
            }
        }

        private string GetTypeNameForSerialization()
        {
            if (typeof(T) == typeof(bool))
            {
                return nameof(Boolean);
            }
            else
            {
                return nameof(Int32);
            }
        }

        public static CodeStyleOption<T> FromXElement(XElement element)
        {
            var typeAttribute = element.Attribute("Type");
            var valueAttribute = element.Attribute(nameof(Value));
            var severityAttribute = element.Attribute(nameof(DiagnosticSeverity));
            var version = (int)element.Attribute(nameof(SerializationVersion));

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

            NotificationOption notificationOption;
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    notificationOption = NotificationOption.None;
                    break;
                case DiagnosticSeverity.Info:
                    notificationOption = NotificationOption.Suggestion;
                    break;
                case DiagnosticSeverity.Warning:
                    notificationOption = NotificationOption.Warning;
                    break;
                case DiagnosticSeverity.Error:
                    notificationOption = NotificationOption.Error;
                    break;
                default:
                    throw new ArgumentException(nameof(element));
            }

            return new CodeStyleOption<T>(value, notificationOption);
        }

        private static Func<string, T> GetParser(string type)
        {
            switch (type)
            {
                case nameof(Boolean):
                    // Try to map a boolean value.  Either map it to true/false if we're a 
                    // CodeStyleOption<bool> or map it to the 0 or 1 value for an enum if we're
                    // a CodeStyleOption<SomeEnumType>.
                    return v => Convert(bool.Parse(v));
                case nameof(Int32):
                    return v => (T)(object)int.Parse(v);
                default:
                    throw new ArgumentException(nameof(type));
            }
        }

        private static T Convert(bool b)
        {
            // If we had a bool and we wanted a bool, then just return this
            // value.
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)b;
            }


            var enumValues = (T[])Enum.GetValues(typeof(T));
            return b 
                ? enumValues.First(v => (int)(object)v == 0) 
                : enumValues.First(v => (int)(object)v == 1);
        }

        public bool Equals(CodeStyleOption<T> other)
            => EqualityComparer<T>.Default.Equals(Value, other.Value) &&
               Notification == other.Notification;

        public override bool Equals(object obj)
            => obj is CodeStyleOption<T> option &&
               Equals(option);

        public override int GetHashCode()
            => Hash.Combine(Value.GetHashCode(), Notification.GetHashCode());
    }
}