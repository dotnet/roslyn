// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;

    /// <summary>
    /// Attribute that defines the expected value of the <see cref="KindPropertyName"/> JSON property when a type is
    /// used in an <see cref="ISumType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal class KindAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KindAttribute"/> class.
        /// </summary>
        /// <param name="kind">The expected value of the <paramref name="kindPropertyName"/> JSON property.</param>
        /// <param name="kindPropertyName">The name of the property that is used to identify the contained type of the <see cref="ISumType"/>.</param>
        /// <remarks>Specifying this attribute doesn't automatically include the <paramref name="kindPropertyName"/> JSON property upon serialization.
        ///
        /// In the current implementation the <paramref name="kindPropertyName"/> JSON property is always considered required.</remarks>
        public KindAttribute(string kind, string kindPropertyName = "kind")
        {
            this.Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            this.KindPropertyName = kindPropertyName ?? throw new ArgumentNullException(nameof(kindPropertyName));
        }

        /// <summary>
        /// Gets the expected value of the <see cref="KindPropertyName"/> JSON property.
        /// </summary>
        public string Kind { get; private set; }

        /// <summary>
        /// Gets the name of the property that is used to identify the contained type of the <see cref="ISumType"/>.
        /// </summary>
        public string KindPropertyName { get; private set; }
    }
}
