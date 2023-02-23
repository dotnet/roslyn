// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal sealed class SymbolSearchLogService : ISymbolSearchLogService
    {
        /// <summary>
        /// Logged messages kept in memory to help us diagnose what was going on previously if a crash occurs.
        /// </summary>
        private static readonly LinkedList<string> s_log = new();

        public static readonly ISymbolSearchLogService Instance = new SymbolSearchLogService();

        private SymbolSearchLogService()
        {
        }

        public ValueTask LogInfoAsync(string text, CancellationToken cancellationToken)
            => LogAsync(text);

        public ValueTask LogExceptionAsync(string exception, string text, CancellationToken cancellationToken)
            => LogAsync(text + ". " + exception);

        private static ValueTask LogAsync(string text)
        {
            Log(text);
            return default;
        }

        private static void Log(string text)
        {
            // Keep a running in memory log as well for debugging purposes.
            s_log.AddLast(text);
            while (s_log.Count > 100)
                s_log.RemoveFirst();
        }
    }
}
