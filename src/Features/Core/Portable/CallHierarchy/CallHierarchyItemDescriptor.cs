// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CallHierarchy;

internal sealed record CallHierarchyItemDescriptor(
    CallHierarchyItemId ItemId,
    string MemberName,
    string ContainingTypeName,
    string ContainingNamespaceName,
    Glyph Glyph,
    ImmutableArray<CallHierarchySearchDescriptor> SupportedSearchDescriptors);
