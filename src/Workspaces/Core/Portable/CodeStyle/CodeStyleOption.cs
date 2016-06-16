// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Represents a code style option and an associated notification option.
    /// </summary>
    public class CodeStyleOption<T>
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
                new XAttribute("Type", typeof(T).Name),
                new XAttribute(nameof(Value), Value),
                new XAttribute(nameof(DiagnosticSeverity), Notification.Value));

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
            var value = (T)parser(valueAttribute.Value);
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

        private static Func<string, object> GetParser(string type)
        {
            switch (type)
            {
                case nameof(Boolean):
                    return v => bool.Parse(v);
                default:
                    throw new ArgumentException(nameof(type));
            }
        }
    }
}
