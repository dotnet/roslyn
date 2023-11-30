// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler;

[assembly: TypeForwardedTo(typeof(HostProductionContext))]

namespace Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler
{
    internal static partial class GeneratorExtensions
    {
        public static void RegisterHostOutput<TSource>(ref this IncrementalGeneratorInitializationContext @this, IncrementalValuesProvider<TSource> source, Action<HostProductionContext, TSource, CancellationToken> action)
        {
            Experimental.GeneratorExtensions.RegisterHostOutput(ref @this, source, action);
        }

        public static ImmutableArray<(string Key, string Value)> GetHostOutputs(this GeneratorRunResult runResult)
        {
            return Experimental.GeneratorExtensions.GetHostOutputs(runResult);
        }
    }
}
