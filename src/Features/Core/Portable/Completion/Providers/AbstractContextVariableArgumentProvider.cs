// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// This <see cref="ArgumentProvider"/> attempts to locate a matching value in the context of a method invocation.
    /// </summary>
    internal abstract class AbstractContextVariableArgumentProvider : ArgumentProvider
    {
        public override Task ProvideArgumentAsync(ArgumentContext context)
        {
            if (context.PreviousValue is not null)
            {
                // This argument provider does not attempt to replace arguments already in code.
                return Task.CompletedTask;
            }

            var symbols = context.SemanticModel.LookupSymbols(context.Position, name: context.Parameter.Name);
            foreach (var symbol in symbols)
            {
                // Currently we check for an exact type match before using a variable from context. As we hone the
                // default argument provider heuristics, we may alter the definition of "in scope" as well as the type
                // and name check(s) that occur.
                if (SymbolEqualityComparer.Default.Equals(context.Parameter.Type, symbol.GetSymbolType()))
                {
                    context.DefaultValue = context.Parameter.Name;
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
    }
}
