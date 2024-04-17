// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SolutionCrawler;

[ExportWorkspaceService(typeof(IDocumentTrackingService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultDocumentTrackingService() : IDocumentTrackingService
{
    public event EventHandler<DocumentId?> ActiveDocumentChanged { add { } remove { } }

    public ImmutableArray<DocumentId> GetVisibleDocuments()
        => [];

    public DocumentId? TryGetActiveDocument()
        => null;
}
