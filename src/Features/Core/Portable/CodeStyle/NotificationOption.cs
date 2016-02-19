// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal class NotificationOption
    {
        public string Name { get; }
        public DiagnosticSeverity Value { get; }

        public static readonly NotificationOption None = new NotificationOption(nameof(None), DiagnosticSeverity.Hidden);
        public static readonly NotificationOption Info = new NotificationOption(nameof(Info), DiagnosticSeverity.Info);
        public static readonly NotificationOption Warning = new NotificationOption(nameof(Warning), DiagnosticSeverity.Warning);
        public static readonly NotificationOption Error = new NotificationOption(nameof(Error), DiagnosticSeverity.Error);

        private NotificationOption(string name, DiagnosticSeverity severity)
        {
            Name = name;
            Value = severity;
        }

        public override string ToString() => Name;
    }
}
