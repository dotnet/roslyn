// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

/// <summary>
/// Represent the information to replace a sub-location inside a comment or string.
/// </summary>
internal record StringAndCommentRenameContext(RenameLocation RenameLocation, string ReplacementText);
