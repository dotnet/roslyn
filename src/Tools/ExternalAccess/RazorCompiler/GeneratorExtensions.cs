// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler
{
    internal static partial class GeneratorExtensions
    {
        public static void RegisterHostOutput<TSource>(ref this IncrementalGeneratorInitializationContext @this, IncrementalValuesProvider<TSource> source, Action<HostProductionContext, TSource, CancellationToken> action)
        {
            _ = @this;
            source.Node.RegisterOutput(new HostOutputNode<TSource>(source.Node, action));
        }

        public static ImmutableArray<(string Key, string Value)> GetHostOutputs(this GeneratorRunResult runResult) => runResult.HostOutputs;
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
