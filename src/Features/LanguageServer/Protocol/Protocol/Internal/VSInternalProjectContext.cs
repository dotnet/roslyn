// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class for a project context.
    /// </summary>
    [DataContract]
    internal class VSInternalProjectContext : VSProjectContext, IEquatable<VSInternalProjectContext>
    {
        /// <summary>
        /// Gets or sets the string context kind of the project context.
        /// </summary>
        [DataMember(Name = "_vs_vsKind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalKindAndModifier? VSKind
        {
            get;
            set;
        }

        public static bool operator ==(VSInternalProjectContext? value1, VSInternalProjectContext? value2)
        {
            if (ReferenceEquals(value1, value2))
            {
                return true;
            }

            if (value2 is null)
            {
                return false;
            }

            return value1?.Equals(value2) ?? false;
        }

        public static bool operator !=(VSInternalProjectContext? value1, VSInternalProjectContext? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public bool Equals(VSInternalProjectContext other)
        {
            return base.Equals(other)
                && this.VSKind == other.VSKind;
        }

        /// <inheritdoc/>
        public override bool Equals(VSProjectContext other)
        {
            return this.Equals((object)other);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is VSInternalProjectContext other)
            {
                return this.Equals(other);
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode()
                ^ (this.VSKind == null ? 13 : this.VSKind.GetHashCode() * 79);
        }
    }
}
