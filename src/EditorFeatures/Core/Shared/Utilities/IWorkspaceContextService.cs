// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

internal interface IWorkspaceContextService : IWorkspaceService
{
    /// <summary>
    /// Determines if LSP is being used as the editor.
    /// Used to disable non-LSP editor feature integration.
    /// </summary>
    bool IsInLspEditorContext();

    /// <summary>
    /// Determines if the VS instance is being as a cloud environment client.
    /// </summary>
    bool IsCloudEnvironmentClient();
}

[ExportWorkspaceService(typeof(IWorkspaceContextService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultWorkspaceContextService(IGlobalOptionService globalOptionsService) : IWorkspaceContextService
{
    /// <summary>
    /// Roslyn LSP feature flag name, as defined in the PackageRegistraion.pkgdef
    /// by everything following '$RootKey$\FeatureFlags\' and '\' replaced by '.'
    /// </summary>
    public const string LspEditorFeatureFlagName = "Roslyn.LSP.Editor";

    private readonly IGlobalOptionService _globalOptionsService = globalOptionsService;

    public bool IsInLspEditorContext() => _globalOptionsService.GetOption(LspOptionsStorage.LspEditorFeatureFlag);

    public bool IsCloudEnvironmentClient() => false;
}
