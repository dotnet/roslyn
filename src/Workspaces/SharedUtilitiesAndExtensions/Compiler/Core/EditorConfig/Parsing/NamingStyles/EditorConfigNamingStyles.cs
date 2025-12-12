// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;

/// <summary>
/// Represents a completed parse of a single editorconfig document
/// </summary>
/// <param name="FileName">The full file path to the file on disk. Can be null if you never need to compare if a section is valid for pathing reasons</param>
/// <param name="Rules">The set of naming style options that were parsed in the file</param>
internal sealed record class EditorConfigNamingStyles(string? FileName, ImmutableArray<NamingStyleOption> Rules)
    : EditorConfigFile<NamingStyleOption>(FileName, Rules);
