// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal interface ICodeStyleOption : IObjectWritable
    {
        XElement ToXElement();
        object? Value { get; }
        NotificationOption2 Notification { get; }
        ICodeStyleOption WithValue(object value);
        ICodeStyleOption WithNotification(NotificationOption2 notification);
        ICodeStyleOption AsCodeStyleOption<TCodeStyleOption>();
#if !CODE_STYLE
        ICodeStyleOption AsPublicCodeStyleOption();
#endif
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
    internal sealed partial class CodeStyleOption2<T> : ICodeStyleOption, IEquatable<CodeStyleOption2<T>?>
    {
        static CodeStyleOption2()
        {
            ObjectBinder.RegisterTypeReader(typeof(CodeStyleOption2<T>), ReadFrom);
        }

        public static readonly CodeStyleOption2<T> Default = new(default!, NotificationOption2.Silent);

        private const int SerializationVersion = 1;

        [DataMember(Order = 0)]
        public T Value { get; }

        [DataMember(Order = 1)]
        public NotificationOption2 Notification { get; }

        public CodeStyleOption2(T value, NotificationOption2 notification)
        {
            Value = value;
            Notification = notification;
        }

        object? ICodeStyleOption.Value => this.Value;
        ICodeStyleOption ICodeStyleOption.WithValue(object value) => new CodeStyleOption2<T>((T)value, Notification);
        ICodeStyleOption ICodeStyleOption.WithNotification(NotificationOption2 notification) => new CodeStyleOption2<T>(Value, notification);

#if CODE_STYLE
        ICodeStyleOption ICodeStyleOption.AsCodeStyleOption<TCodeStyleOption>() => this;
#else
        ICodeStyleOption ICodeStyleOption.AsCodeStyleOption<TCodeStyleOption>()
            => this is TCodeStyleOption ? this : new CodeStyleOption<T>(this);
        ICodeStyleOption ICodeStyleOption.AsPublicCodeStyleOption() => new CodeStyleOption<T>(this);
#endif

        private int EnumValueAsInt32 => (int)(object)Value!;

        public XElement ToXElement() =>
            new("CodeStyleOption", // Ensure that we use "CodeStyleOption" as the name for back compat.
                new XAttribute(nameof(SerializationVersion), SerializationVersion),
                new XAttribute("Type", GetTypeNameForSerialization()),
                new XAttribute(nameof(Value), GetValueForSerialization()),
                new XAttribute(nameof(DiagnosticSeverity), Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));

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

        public static CodeStyleOption2<T> FromXElement(XElement element)
        {
            var typeAttribute = element.Attribute("Type");
            var valueAttribute = element.Attribute(nameof(Value));
            var severityAttribute = element.Attribute(nameof(DiagnosticSeverity));
            var version = (int?)element.Attribute(nameof(SerializationVersion));

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

        public bool ShouldReuseInSerialization => false;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteValue(GetValueForSerialization());
            writer.WriteInt32((int)(Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));
        }

        public static CodeStyleOption2<object> ReadFrom(ObjectReader reader)
        {
            return new CodeStyleOption2<object>(
                reader.ReadValue(),
                (DiagnosticSeverity)reader.ReadInt32() switch
                {
                    DiagnosticSeverity.Hidden => NotificationOption2.Silent,
                    DiagnosticSeverity.Info => NotificationOption2.Suggestion,
                    DiagnosticSeverity.Warning => NotificationOption2.Warning,
                    DiagnosticSeverity.Error => NotificationOption2.Error,
                    var v => throw ExceptionUtilities.UnexpectedValue(v),
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
}
