// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class ManagedEditAndContinueDebuggerServiceExtensions_REMOVE
    {
        /// <summary>
        /// This will be removed when IManagedEditAndContinueDebuggerService gets the method for real
        /// </summary>
        public static ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(this IManagedEditAndContinueDebuggerService _1, CancellationToken _2)
        {
            return new(ImmutableArray.Create("Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"));
        }
    }
}
