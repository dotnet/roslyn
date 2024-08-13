// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler
{
    /// <summary>
    /// Nothing should be using these extensions, but they are kept here for now for back compat
    /// in case an older razor generator is using a newer Roslyn.
    /// </summary>
    internal static partial class GeneratorExtensions
    {
        public static void RegisterHostOutput<TSource>(ref this IncrementalGeneratorInitializationContext @this, IncrementalValuesProvider<TSource> source, Action<HostProductionContext, TSource, CancellationToken> action)
        {
            @this.RegisterHostOutput(source, (ctx, source) =>
            {
                var outputs = ArrayBuilder<(string, string)>.GetInstance();
                var hpc = new HostProductionContext(outputs);
                action(hpc, source, CancellationToken.None);
                foreach (var output in outputs)
                {
                    ctx.AddOutput(output.Item1, output.Item2);
                }
                outputs.Free();
            });
        }

        public static ImmutableArray<(string Key, string Value)> GetHostOutputs(this GeneratorRunResult runResult) => runResult.HostOutputs.SelectAsArray(a => (a.Key, a.Value.ToString() ?? ""));
    }

    internal readonly struct HostProductionContext
    {
        internal readonly ArrayBuilder<(string, string)> Outputs;

        internal HostProductionContext(ArrayBuilder<(string, string)> outputs)
        {
            Outputs = outputs;
        }

        public void AddOutput(string name, string value) => Outputs.Add((name, value));
    }
}
