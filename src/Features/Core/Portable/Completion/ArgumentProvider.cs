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

        public abstract Task ProvideArgumentAsync(ArgumentContext context);
    }
}
