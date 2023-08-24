// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// Contains msbuild information used for telemetry reporting on the project.
/// This information intentionally matches what O# produces to ensure consistent telemetry.
/// </summary>
internal record ProjectTelemetryMetadata(ImmutableArray<string> ProjectCapabilities, ImmutableArray<string> ContentFilePaths, bool IsSdkStyle);
