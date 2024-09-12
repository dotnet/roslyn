// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represents a color.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#color">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class Color
    {
        /// <summary>
        /// Gets or sets the Red value.
        /// </summary>
        /// <remarks>
        /// Value should be clamped to [0,1].
        /// </remarks>
        [DataMember(Name = "red")]
        public decimal Red { get; set; }

        /// <summary>
        /// Gets or sets the Green value.
        /// </summary>
        /// <remarks>
        /// Value should be clamped to [0,1].
        /// </remarks>
        [DataMember(Name = "green")]
        public decimal Green { get; set; }

        /// <summary>
        /// Gets or sets the Blue value.
        /// </summary>
        /// <remarks>
        /// Value should be clamped to [0,1].
        /// </remarks>
        [DataMember(Name = "blue")]
        public decimal Blue { get; set; }

        /// <summary>
        /// Gets or sets the Alpha value.
        /// </summary>
        /// <remarks>
        /// Value should be clamped to [0,1].
        /// </remarks>
        [DataMember(Name = "alpha")]
        public decimal Alpha { get; set; }
    }
}
