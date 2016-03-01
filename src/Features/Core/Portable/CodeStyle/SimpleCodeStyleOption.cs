// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Represents a code style option that typically 
    /// offers two choices as preferences and a set of 
    /// notification styles. the preference is then 
    /// simply recorded as a boolean.
    /// </summary>
    internal class SimpleCodeStyleOption
    {
        private const int SerializationVersion = 1;
        public static readonly SimpleCodeStyleOption Default = new SimpleCodeStyleOption(false, NotificationOption.None);

        public SimpleCodeStyleOption(bool isChecked, NotificationOption notification)
        {
            IsChecked = isChecked;
            Notification = notification;
        }

        public bool IsChecked { get; set; }

        public NotificationOption Notification { get; set; }

        public XElement ToXElement() => 
            new XElement(nameof(SimpleCodeStyleOption),
                new XAttribute(nameof(SerializationVersion), SerializationVersion),
                new XAttribute(nameof(IsChecked), IsChecked),
                new XAttribute(nameof(DiagnosticSeverity), Notification.Value));

        public static SimpleCodeStyleOption FromXElement(XElement element)
        {
            var isChecked = bool.Parse(element.Attribute(nameof(IsChecked)).Value);
            var severity = (DiagnosticSeverity)Enum.Parse(typeof(DiagnosticSeverity), element.Attribute(nameof(DiagnosticSeverity)).Value);
            NotificationOption notificationOption;

            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    notificationOption = NotificationOption.None;
                    break;
                case DiagnosticSeverity.Info:
                    notificationOption = NotificationOption.Info;
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

            return new SimpleCodeStyleOption(isChecked, notificationOption);
        }
    }
}
