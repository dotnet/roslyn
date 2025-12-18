// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation;

[ExportWorkspaceService(typeof(INavigateToLinkService), layer: ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultNavigateToLinkService() : INavigateToLinkService
{
    public async ValueTask<bool> TryNavigateToLinkAsync(Uri uri, CancellationToken cancellationToken)
        => false;
}
