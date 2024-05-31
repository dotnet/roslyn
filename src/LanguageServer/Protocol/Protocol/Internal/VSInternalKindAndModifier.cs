// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class that contains the base kind and modifiers used to describe a response item.
    /// </summary>
    internal class VSInternalKindAndModifier : IEquatable<VSInternalKindAndModifier>
    {
        /// <summary>
        /// Gets or sets the ImageIds for a certain kind.
        /// </summary>
        [JsonPropertyName("_vs_kind")]
        public string Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the modifier of the kind.
        /// </summary>
        [JsonPropertyName("_vs_modifier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Modifier
        {
            get;
            set;
        }

        public static bool operator ==(VSInternalKindAndModifier? value1, VSInternalKindAndModifier? value2)
        {
            if (ReferenceEquals(value1, value2))
            {
                return true;
            }

            // Is null?
            if (ReferenceEquals(null, value2))
            {
                return false;
            }

            return value1?.Equals(value2) ?? false;
        }

        public static bool operator !=(VSInternalKindAndModifier? value1, VSInternalKindAndModifier? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as VSInternalKindAndModifier);
        }

        /// <inheritdoc/>
        public bool Equals(VSInternalKindAndModifier? other)
        {
            return other != null &&
                string.Equals(this.Kind, other.Kind, StringComparison.Ordinal) &&
                this.CheckModifierEquality(other.Modifier);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1850642763;
            hashCode = (hashCode * -1521134295) + (this.Kind == null ? 0 : StringComparer.Ordinal.GetHashCode(this.Kind));
            if (this.Modifier != null)
            {
                for (var i = 0; i < this.Modifier.Length; i++)
                {
                    if (this.Modifier[i] != null)
                    {
                        hashCode = (hashCode * -1521134295) + StringComparer.Ordinal.GetHashCode(this.Modifier[i]);
                    }
                }
            }

            return hashCode;
        }

        private bool CheckModifierEquality(string[]? modifiers)
        {
            if (modifiers == null ^ this.Modifier == null)
            {
                return false;
            }

            if (modifiers != null &&
                this.Modifier != null &&
                modifiers.Length == this.Modifier.Length)
            {
                for (var i = 0; i < modifiers.Length; i++)
                {
                    if (!string.Equals(modifiers[i], this.Modifier[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
