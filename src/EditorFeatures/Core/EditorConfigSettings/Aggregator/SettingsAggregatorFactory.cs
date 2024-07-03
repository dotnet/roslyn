// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings;

[ExportWorkspaceServiceFactory(typeof(ISettingsAggregator), ServiceLayer.Default), Shared]
internal sealed class SettingsAggregatorFactory : IWorkspaceServiceFactory
{
    private readonly IThreadingContext _threadingContext;
    private readonly IAsynchronousOperationListener _listener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SettingsAggregatorFactory(
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        _listener = listenerProvider.GetListener(FeatureAttribute.RuleSetEditor);
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new SettingsAggregator(workspaceServices.Workspace, _threadingContext, _listener);
}
