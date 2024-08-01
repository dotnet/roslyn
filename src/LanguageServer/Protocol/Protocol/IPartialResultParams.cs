﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;

    /// <summary>
    /// Interface to describe parameters for requests that support streaming results.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#partialResultParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    /// <typeparam name="T">The type to be reported by <see cref="PartialResultToken"/>.</typeparam>
    internal interface IPartialResultParams<T>
    {
        /// <summary>
        /// Gets or sets the value of the PartialResultToken instance.
        /// </summary>
        public IProgress<T>? PartialResultToken
        {
            get;
            set;
        }
    }
}
