// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

/// <summary>
/// A result of a synchronization operation for a Html document
/// </summary>
/// <remarks>
/// If <see cref="Synchronized" /> is <see langword="false" />, <see cref="Checksum" /> will be <see langword="default" />.
/// </remarks>
internal readonly record struct SynchronizationResult(bool Synchronized, ChecksumWrapper Checksum);
