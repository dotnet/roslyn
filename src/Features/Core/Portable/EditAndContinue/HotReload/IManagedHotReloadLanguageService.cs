// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

// These types are available in newer version of Debugger.Contracts package in main-vs-deps.

namespace Microsoft.VisualStudio.Debugger.Contracts.HotReload
{
    internal interface IManagedHotReloadLanguageService
    {
        ValueTask StartSessionAsync(CancellationToken cancellationToken);

        ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken);

        ValueTask CommitUpdatesAsync(CancellationToken cancellationToken);

        ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken);

        ValueTask EndSessionAsync(CancellationToken cancellationToken);
    }

    internal interface IManagedHotReloadService
    {
        ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken);
    }

    internal readonly struct ManagedHotReloadUpdates
    {
        public ImmutableArray<ManagedHotReloadUpdate> Updates { get; }
        public ImmutableArray<ManagedHotReloadDiagnostic> Diagnostics { get; }

        public ManagedHotReloadUpdates(ImmutableArray<ManagedHotReloadUpdate> updates, ImmutableArray<ManagedHotReloadDiagnostic> diagnostics)
        {
            Updates = updates;
            Diagnostics = diagnostics;
        }
    }

    internal readonly struct ManagedHotReloadUpdate
    {
        public Guid Module { get; }
        public ImmutableArray<byte> ILDelta { get; }
        public ImmutableArray<byte> MetadataDelta { get; }

        public ManagedHotReloadUpdate(Guid module, ImmutableArray<byte> ilDelta, ImmutableArray<byte> metadataDelta)
        {
            Module = module;
            ILDelta = ilDelta;
            MetadataDelta = metadataDelta;
        }
    }

    internal readonly struct ManagedHotReloadDiagnostic
    {
        public string Id { get; }
        public string Message { get; }
        public ManagedHotReloadDiagnosticSeverity Severity { get; }
        public string FilePath { get; }
        public SourceSpan Span { get; }

        public ManagedHotReloadDiagnostic(string id, string message, ManagedHotReloadDiagnosticSeverity severity, string filePath, SourceSpan span)
        {
            Id = id;
            Message = message;
            Severity = severity;
            FilePath = filePath;
            Span = span;
        }
    }

    internal enum ManagedHotReloadDiagnosticSeverity
    {
        Warning = 1,
        Error
    }
}
