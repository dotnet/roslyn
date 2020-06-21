// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// A little class which holds onto the next ID to be produced. Avoids static state in unit testing.
    /// </summary>
    internal sealed class IdFactory
    {
        /// <summary>
        /// The next numberic ID that will be used for an object. Accessed only with Interlocked.Increment.
        /// </summary>
        private int _globalId = 0;

        public Id<T> Create<T>() where T : Element
        {
            var id = Interlocked.Increment(ref _globalId);
            return new Id<T>(id);
        }
    }
}
