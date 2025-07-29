// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms
{
    [JsonDerivedType(typeof(GetProject), nameof(GetProject))]
    internal abstract class RunApiInput
    {
        private RunApiInput() { }

        public sealed class GetProject : RunApiInput
        {
            public string? ArtifactsPath { get; init; }
            public required string EntryPointFileFullPath { get; init; }
        }
    }

    [JsonDerivedType(typeof(Error), nameof(Error))]
    [JsonDerivedType(typeof(Project), nameof(Project))]
    internal abstract class RunApiOutput
    {
        private RunApiOutput() { }

        public const int LatestKnownVersion = 1;

        [JsonPropertyOrder(-1)]
        public int Version { get; }

        public sealed class Error : RunApiOutput
        {
            public required string Message { get; init; }
            public required string Details { get; init; }
        }

        public sealed class Project : RunApiOutput
        {
            public required string Content { get; init; }
            public required ImmutableArray<SimpleDiagnostic> Diagnostics { get; init; }
        }
    }

    internal sealed record SimpleDiagnostic
    {
        public required Position Location { get; init; }
        public required string Message { get; init; }

        /// <summary>
        /// An adapter of <see cref="FileLinePositionSpan"/> that ensures we JSON-serialize only the necessary fields.
        /// </summary>
        public readonly record struct Position
        {
            public string Path { get; init; }
            public LinePositionSpanInternal Span { get; init; }
        }
    }

    internal record struct LinePositionInternal
    {
        public int Line { get; init; }
        public int Character { get; init; }
    }

    /// <summary>
    /// Workaround for inability to deserialize directly to <see cref="LinePositionSpan"/>.
    /// </summary>
    internal record struct LinePositionSpanInternal
    {
        public LinePositionInternal Start { get; init; }
        public LinePositionInternal End { get; init; }

        public LinePositionSpan ToLinePositionSpan()
        {
            return new LinePositionSpan(
                start: new LinePosition(Start.Line, Start.Character),
                end: new LinePosition(End.Line, End.Character));
        }
    }

    [JsonSerializable(typeof(RunApiInput))]
    [JsonSerializable(typeof(RunApiOutput))]
    [JsonSerializable(typeof(LinePositionSpanInternal))]
    internal partial class RunFileApiJsonSerializerContext : JsonSerializerContext;
}
