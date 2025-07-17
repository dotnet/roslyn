// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class XamlSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data XAML injection sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static XamlSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWindowsMarkupXamlReader,
                SinkKind.Xaml,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "Load", new[] { "stream", "reader", "xaml" }),
                    ( "LoadAsync", ["stream", "reader"]),
                    ( "LoadWithInitialTemplateValidation", ["xaml"]),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
