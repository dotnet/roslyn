﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

/// <summary>
/// Wrapper for <see cref="SourceGeneratedDocumentIdentity" /> and <see cref="SourceGeneratorIdentity" />
/// </summary>
internal record struct RazorGeneratedDocumentIdentity(DocumentId DocumentId, string HintName, string FilePath, string GeneratorAssemblyName, string? GeneratorAssemblyPath, Version GeneratorAssemblyVersion, string GeneratorTypeName);
