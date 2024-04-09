// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Extension class for <see cref="InitializeParams"/> with fields specific to Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSInternalInitializeParams : InitializeParams
    {
        /// <summary>
        /// Gets or sets the capabilities supported by the Visual Studio client.
        /// </summary>
        [DataMember(Name = "capabilities")]
        public VSInternalClientCapabilities VSInternalCapabilities
        {
            get => Capabilities as VSInternalClientCapabilities;
            set => Capabilities = value;
        }
    }
}
