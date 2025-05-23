// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTestGenerator.Api;

internal sealed class WrappedAddImportFixData
{
    internal readonly AddImportFixData Underlying;

    public WrappedAddImportFixKind Kind => Underlying.Kind switch
    {
        AddImportFixKind.ProjectSymbol => WrappedAddImportFixKind.ProjectSymbol,
        AddImportFixKind.PackageSymbol => WrappedAddImportFixKind.PackageSymbol,
        AddImportFixKind.MetadataSymbol => WrappedAddImportFixKind.MetadataSymbol,
        AddImportFixKind.ReferenceAssemblySymbol => WrappedAddImportFixKind.ReferenceAssemblySymbol,
        _ => throw ExceptionUtilities.UnexpectedValue(Underlying.Kind),
    };

    public ImmutableArray<TextChange> TextChanges => Underlying.TextChanges;

    public string? Title => Underlying.Title;

    public ImmutableArray<string> Tags => Underlying.Tags;

    #region When adding P2P references.

    public ProjectId? ProjectReferenceToAdd => Underlying.ProjectReferenceToAdd;

    #endregion

    #region When adding a metadata reference

    public ProjectId? PortableExecutableReferenceProjectId => Underlying.PortableExecutableReferenceProjectId;

    public string? PortableExecutableReferenceFilePathToAdd => Underlying.PortableExecutableReferenceFilePathToAdd;

    #endregion

    #region When adding an assembly reference

    public string? AssemblyReferenceAssemblyName => Underlying.AssemblyReferenceAssemblyName;

    public string? AssemblyReferenceFullyQualifiedTypeName => Underlying.AssemblyReferenceFullyQualifiedTypeName;

    #endregion

    #region When adding a package reference

    public string? PackageSource => Underlying.PackageSource;

    public string? PackageName => Underlying.PackageName;

    public string? PackageVersionOpt => Underlying.PackageVersionOpt;

    #endregion

    internal WrappedAddImportFixData(AddImportFixData underlying)
    {
        Underlying = underlying;
    }
}
