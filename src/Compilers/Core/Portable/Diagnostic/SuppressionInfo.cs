// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains information about the source of diagnostic suppression.
    /// </summary>
    public sealed class SuppressionInfo
    {
        /// <summary>
        /// <see cref="Diagnostic.Id"/> of the suppressed diagnostic.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// If the diagnostic was suppressed by an attribute, then returns that attribute.
        /// Otherwise, returns null.
        /// </summary>
        public AttributeData? Attribute { get; }

        internal SuppressionInfo(string id, AttributeData? attribute)
        {
            Id = id;
            Attribute = attribute;
        }
    }
}
