// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract class ArgumentProvider
    {
        public string Name { get; }

        protected ArgumentProvider()
            => Name = GetType().FullName!;

        /// <summary>
        /// Supports providing argument values for an argument completion session.
        /// </summary>
        /// <remarks>
        /// See <see cref="ArgumentContext"/> for more information about argument values.
        /// </remarks>
        /// <param name="context">The argument context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task ProvideArgumentAsync(ArgumentContext context);
    }
}
