// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace IdeBenchmarks.Lsp;

[MemoryDiagnoser]
public class LspDispatchBenchmarks
{
    [Benchmark]
    public object CreateQueueItem()
    {
        using var cancellationSource = new CancellationTokenSource();
        return QueueItem<object>.Create(
            "benchmark",
            serializedRequest: null,
            EmptyLspServices.Instance,
            NoOpLspLogger.Instance,
            cancellationSource.Token).Item1;
    }

    private sealed class EmptyLspServices : ILspServices
    {
        public static readonly EmptyLspServices Instance = new();

        public void Dispose()
        {
        }

        public T? GetService<T>() where T : notnull
            => default;

        public T GetRequiredService<T>() where T : notnull
            => throw new InvalidOperationException();

        public IEnumerable<T> GetRequiredServices<T>()
            => [];

        public bool TryGetService(Type type, [NotNullWhen(true)] out object? service)
        {
            service = null;
            return false;
        }
    }
}
