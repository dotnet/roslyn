// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Options to signal work done progress support in server capabilities.
    /// </summary>
    internal interface IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        ///
        /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workDoneProgressOptions">Language Server Protocol specification</see> for additional information.
        /// </summary>
        bool WorkDoneProgress
        {
            get;
            set;
        }
    }
}
