// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Offers different notification styles for enforcing
    /// a code style. Under the hood, it simply maps to <see cref="DiagnosticSeverity"/>
    /// </summary>
    /// <remarks>
    /// This also supports various properties for databinding.
    /// </remarks>
    [DataContract]
    internal class NotificationOption
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DiagnosticSeverity Value { get; set; }

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
