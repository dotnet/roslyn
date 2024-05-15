// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

/// <summary>
/// This class is used to refer to a Symbol definition which could be in source or metadata
/// it has a metadata name.
/// </summary>
internal class RenameDeclarationLocationReference
{
    // The DocumentId and the TextSpan of the First Symbol Location
    public readonly DocumentId DocumentId;
    public readonly TextSpan TextSpan;

    /// <summary>
    /// The metadata name for this symbol.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Count of symbol location (Partial Types, Constructors, etc).
    /// </summary>
    public readonly int SymbolLocationsCount;

    /// <summary>
    /// A flag indicating that the associated symbol is an override of a symbol from metadata
    /// </summary>
    public readonly bool IsOverriddenFromMetadata;

    public RenameDeclarationLocationReference(DocumentId documentId, TextSpan textSpan, bool overriddenFromMetadata, int declaringSyntaxReferencesCount)
    {
        this.DocumentId = documentId;
        this.TextSpan = textSpan;
        this.Name = null;
        this.SymbolLocationsCount = declaringSyntaxReferencesCount;
        this.IsOverriddenFromMetadata = overriddenFromMetadata;
    }

    public RenameDeclarationLocationReference(string name, int declaringSyntaxReferencesCount)
    {
        this.Name = name;
        this.IsOverriddenFromMetadata = false;
        this.SymbolLocationsCount = declaringSyntaxReferencesCount;
    }

    public bool IsSourceLocation
    {
        get
        {
            return Name == null;
        }
    }
}
