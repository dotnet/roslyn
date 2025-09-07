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
    internal sealed class SimpleDiagnostic
    {
        public required Position Location { get; init; }
        public required string Message { get; init; }

        /// <summary>
        /// An adapter of <see cref="FileLinePositionSpan"/> that ensures we JSON-serialize only the necessary fields.
        /// </summary>
        public readonly struct Position
        {
            public string Path { get; init; }
            public LinePositionSpan Span { get; init; }

            public static implicit operator Position(FileLinePositionSpan fileLinePositionSpan) => new()
            {
                Path = fileLinePositionSpan.Path,
                Span = fileLinePositionSpan.Span,
            };
        }
    }

    [JsonSerializable(typeof(RunApiInput))]
    [JsonSerializable(typeof(RunApiOutput))]
    internal partial class RunFileApiJsonSerializerContext : JsonSerializerContext;
}
