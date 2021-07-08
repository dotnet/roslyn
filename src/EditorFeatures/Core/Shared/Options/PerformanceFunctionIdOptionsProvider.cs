// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    [ExportOptionProvider, Shared]
    internal class PerformanceFunctionIdOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PerformanceFunctionIdOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options
        {
            get
            {
                using var resultDisposer = ArrayBuilder<IOption>.GetInstance(out var result);
                foreach (var id in (FunctionId[])Enum.GetValues(typeof(FunctionId)))
                {
                    result.Add(FunctionIdOptions.GetOption(id));
                }

                return result.ToImmutable();
            }
        }
    }
}
