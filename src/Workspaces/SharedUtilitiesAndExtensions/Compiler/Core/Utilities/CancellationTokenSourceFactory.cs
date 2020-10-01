// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class CancellationTokenSourceFactory
    {
        private static readonly ConditionalWeakTable<CancellationTokenSource, Tuple<string, int>> s_tokenCreators = new();

        private static CancellationTokenSource RecordLocation(CancellationTokenSource source, string? sourcePath, int sourceLine)
        {
            Contract.ThrowIfNull(sourcePath);
            s_tokenCreators.Add(source, Tuple.Create(sourcePath, sourceLine));
            return source;
        }

        public static CancellationTokenSource Create([CallerFilePath] string? sourcePath = null, [CallerLineNumber] int sourceLine = 0)
            => RecordLocation(new CancellationTokenSource(), sourcePath, sourceLine);

#pragma warning disable CA1068 // CancellationToken parameters must come last
        internal static CancellationTokenSource CreateLinkedTokenSource(CancellationToken cancellationToken, [CallerFilePath] string? sourcePath = null, [CallerLineNumber] int sourceLine = 0)
            => RecordLocation(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken), sourcePath, sourceLine);
#pragma warning restore

#pragma warning disable CA1068 // CancellationToken parameters must come last
        internal static CancellationTokenSource CreateLinkedTokenSource(CancellationToken cancellationToken1, CancellationToken cancellationToken2, [CallerFilePath] string? sourcePath = null, [CallerLineNumber] int sourceLine = 0)
            => RecordLocation(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken1, cancellationToken2), sourcePath, sourceLine);
#pragma warning restore

        internal static bool TryGetCreationSourceLocation(CancellationToken cancellationToken, [NotNullWhen(true)] out string? sourcePath, out int sourceLine)
        {
            var source = (CancellationTokenSource?)typeof(CancellationToken)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(field => field.FieldType == typeof(CancellationTokenSource))?
                .GetValue(cancellationToken);

            if (source != null && s_tokenCreators.TryGetValue(source, out var location))
            {
                (sourcePath, sourceLine) = location;
                return true;
            }

            sourcePath = null;
            sourceLine = 0;
            return false;
        }

        internal static string GetUnexpectedCancellationMessage(OperationCanceledException exception, CancellationToken cancellationToken)
        {
            var sourceLocation = TryGetCreationSourceLocation(exception.CancellationToken, out var sourcePath, out var sourceLine) ?
                $"{sourcePath}({sourceLine})" : "unknown";

            return $"Unexpected cancellation triggered by cancellation source at {sourceLocation}: " +
                (cancellationToken == CancellationToken.None ? " caller does not expect cancellation" : "caller expects different token signaled");
        }
    }
}
