// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal static class RoslynGraphProperties
{
    public static readonly GraphSchema Schema;

    /// <summary>
    /// A graph property that holds the SymbolId of the symbol.
    /// </summary>
    public static readonly GraphProperty SymbolId;

    /// <summary>
    /// A graph property that holds the ProjectId where you can find the symbol. Note this is
    /// not strictly the project that defines the symbol in the case the symbol is from metadata.
    /// It's simply a project that has a compilation which you can use to get to the symbol.
    /// </summary>
    public static readonly GraphProperty ContextProjectId;

    /// <summary>
    /// A graph property that holds the DocumentId where you can find the symbol. This is used
    /// to distinguish between multiple locations for partial types. This will only exist
    /// for symbols in source that have partial implementations.
    /// </summary>
    public static readonly GraphProperty ContextDocumentId;

    /// <summary>
    /// A graph property to hold the label we have generated for the node.
    /// </summary>
    public static readonly GraphProperty Label;

    /// <summary>
    /// A graph property to hold the formatted label we have generated for the node.
    /// </summary>
    public static readonly GraphProperty FormattedLabelWithoutContainingSymbol;

    /// <summary>
    /// A graph property to hold the formatted label that has the containing symbol name.
    /// </summary>
    public static readonly GraphProperty FormattedLabelWithContainingSymbol;

    /// <summary>
    /// A graph property to hold the description we have generated for the node.
    /// </summary>
    public static readonly GraphProperty Description;

    /// <summary>
    /// A graph property to hold the description that has the containing symbol name.
    /// </summary>
    public static readonly GraphProperty DescriptionWithContainingSymbol;

    public static readonly GraphProperty SymbolKind;
    public static readonly GraphProperty TypeKind;
    public static readonly GraphProperty MethodKind;
    public static readonly GraphProperty DeclaredAccessibility;
    public static readonly GraphProperty SymbolModifiers;
    public static readonly GraphProperty ExplicitInterfaceImplementations;

    static RoslynGraphProperties()
    {
        Schema = new GraphSchema("Roslyn");

        SymbolKind = Schema.Properties.AddNewProperty(
            id: "SymbolKind",
            dataType: typeof(SymbolKind),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        TypeKind = Schema.Properties.AddNewProperty(
            id: "TypeKind",
            dataType: typeof(TypeKind),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        MethodKind = Schema.Properties.AddNewProperty(
            id: "MethodKind",
            dataType: typeof(MethodKind),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        DeclaredAccessibility = Schema.Properties.AddNewProperty(
            id: "DeclaredAccessibility",
            dataType: typeof(Accessibility),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        SymbolModifiers = Schema.Properties.AddNewProperty(
            id: "SymbolModifiers",
            dataType: typeof(DeclarationModifiers),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        ExplicitInterfaceImplementations = Schema.Properties.AddNewProperty(
            id: "ExplicitInterfaceImplementations",
            dataType: typeof(IList<SymbolKey>),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        SymbolId = Schema.Properties.AddNewProperty(
            id: "SymbolId",
            dataType: typeof(SymbolKey?),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        ContextProjectId = Schema.Properties.AddNewProperty(
            id: "ContextProjectId",
            dataType: typeof(ProjectId),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        ContextDocumentId = Schema.Properties.AddNewProperty(
            id: "ContextDocumentId",
            dataType: typeof(DocumentId),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        Label = Schema.Properties.AddNewProperty(
            id: "Label",
            dataType: typeof(string),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        FormattedLabelWithoutContainingSymbol = Schema.Properties.AddNewProperty(
            id: "FormattedLabelWithoutContainingSymbol",
            dataType: typeof(string),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        FormattedLabelWithContainingSymbol = Schema.Properties.AddNewProperty(
            id: "FormattedLabelWithContainingSymbol",
            dataType: typeof(string),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        Description = Schema.Properties.AddNewProperty(
            id: "Description",
            dataType: typeof(string),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));

        DescriptionWithContainingSymbol = Schema.Properties.AddNewProperty(
            id: "DescriptionWithContainingSymbol",
            dataType: typeof(string),
            callback: () => new GraphMetadata(options: GraphMetadataOptions.Sharable | GraphMetadataOptions.Removable));
    }
}
