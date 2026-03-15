// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents an item in the call hierarchy (e.g., a method, property, event, or field).
/// </summary>
internal sealed class CallHierarchyItem : IEquatable<CallHierarchyItem>
{
    /// <summary>
    /// The symbol key for the symbol this item represents. Used to resolve the symbol across solution updates.
    /// </summary>
    public SymbolKey SymbolKey { get; }

    /// <summary>
    /// The display name of the item (e.g., "MyMethod(int, string)").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The symbol kind (Method, Property, Event, Field).
    /// </summary>
    public SymbolKind Kind { get; }

    /// <summary>
    /// Detail text providing additional context (e.g., containing type name).
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// The containing namespace name.
    /// </summary>
    public string ContainingNamespace { get; }

    /// <summary>
    /// The project containing this item.
    /// </summary>
    public ProjectId ProjectId { get; }

    /// <summary>
    /// The document containing the definition of this item.
    /// </summary>
    public DocumentId DocumentId { get; }

    /// <summary>
    /// The text span of the symbol's definition in the document.
    /// </summary>
    public TextSpan Span { get; }

    public CallHierarchyItem(
        SymbolKey symbolKey,
        string name,
        SymbolKind kind,
        string detail,
        string containingNamespace,
        ProjectId projectId,
        DocumentId documentId,
        TextSpan span)
    {
        SymbolKey = symbolKey;
        Name = name;
        Kind = kind;
        Detail = detail;
        ContainingNamespace = containingNamespace;
        ProjectId = projectId;
        DocumentId = documentId;
        Span = span;
    }

    public bool Equals(CallHierarchyItem? other)
    {
        if (other is null)
            return false;

        return SymbolKey.Equals(other.SymbolKey) &&
               Name == other.Name &&
               Kind == other.Kind &&
               ProjectId == other.ProjectId &&
               DocumentId == other.DocumentId &&
               Span == other.Span;
    }

    public override bool Equals(object? obj)
        => Equals(obj as CallHierarchyItem);

    public override int GetHashCode()
        => Hash.Combine(SymbolKey.GetHashCode(),
           Hash.Combine(Name.GetHashCode(),
           Hash.Combine((int)Kind,
           Hash.Combine(ProjectId.GetHashCode(),
           Hash.Combine(DocumentId.GetHashCode(), Span.GetHashCode())))));
}
