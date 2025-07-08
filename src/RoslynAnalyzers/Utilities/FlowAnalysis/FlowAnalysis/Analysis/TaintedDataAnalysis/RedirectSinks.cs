// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class RedirectSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data Redirect injection sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static RedirectSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWebHttpResponse,
                SinkKind.Redirect,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] { "RedirectLocation" },
                sinkMethodParameters: new[] {
                    ( "Redirect", new[] { "url" }),
                    ( "RedirectPermanent", ["url"]),
                    ( "RedirectToRoute", ["routeName"]),
                    ( "RedirectToRoutePermanent", ["routeName"]),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemWebHttpResponseBase,
                SinkKind.Redirect,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] { "RedirectLocation" },
                sinkMethodParameters: new[] {
                    ( "Redirect", new[] { "url" }),
                    ( "RedirectPermanent", ["url"]),
                    ( "RedirectToRoute", ["routeName"]),
                    ( "RedirectToRoutePermanent", ["routeName"]),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
