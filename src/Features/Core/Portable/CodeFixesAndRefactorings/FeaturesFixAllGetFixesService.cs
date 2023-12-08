// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

[ExportWorkspaceService(typeof(IFixAllGetFixesService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FeaturesFixAllGetFixesService() : AbstractFixAllGetFixesService
{
    protected override Solution? GetChangedSolution(Workspace workspace, Solution currentSolution, Solution newSolution, string fixAllPreviewChangesTitle, string fixAllTopLevelHeader, Glyph glyph)
    {
        return newSolution;
    }
}
