// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Strongly typed object used to specify a LSP notification's parameter type.
    /// </summary>
    /// <typeparam name="TIn">The parameter type.</typeparam>
    internal class LspNotification<TIn>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LspNotification{TIn}"/> class.
        /// </summary>
        /// <param name="name">The name of the JSON-RPC notification.</param>
        public LspNotification(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the JSON-RPC notification.
        /// </summary>
        public string Name { get; }
    }
}
