// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Shared;

internal interface IDocumentSupportsFeatureService : IWorkspaceService
{
    bool SupportsCodeFixes(Document document);
    bool SupportsRefactorings(Document document);
    bool SupportsRename(Document document);
    bool SupportsNavigationToAnyPosition(Document document);
    bool SupportsSemanticSnippets(Document document);
}

[ExportWorkspaceService(typeof(IDocumentSupportsFeatureService), ServiceLayer.Default), Shared]
internal class DefaultDocumentSupportsFeatureService : IDocumentSupportsFeatureService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultDocumentSupportsFeatureService()
    {
    }

    public bool SupportsCodeFixes(Document document)
        => true;

    public bool SupportsNavigationToAnyPosition(Document document)
        => true;

    public bool SupportsRefactorings(Document document)
        => true;

    public bool SupportsRename(Document document)
        => true;

    public bool SupportsSemanticSnippets(Document document)
        => true;
}
