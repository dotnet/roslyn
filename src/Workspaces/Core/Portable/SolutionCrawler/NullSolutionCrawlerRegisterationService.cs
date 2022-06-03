// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    /// <summary>
    /// null implementation of the service. it doesn't do anything since there is no way to observe
    /// its impact in this layer.
    /// </summary>
    [ExportWorkspaceService(typeof(ISolutionCrawlerRegistrationService), ServiceLayer.Default), Shared]
    internal partial class NullSolutionCrawlerRegistrationService : ISolutionCrawlerRegistrationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NullSolutionCrawlerRegistrationService()
        {
        }

        public void Register(Workspace workspace)
        {
            // base implementation do nothing.
        }

        public void Unregister(Workspace workspace, bool blockingShutdown = false)
        {
            // base implementation do nothing.
        }

        public void AddAnalyzerProvider(IIncrementalAnalyzerProvider provider, IncrementalAnalyzerProviderMetadata metadata)
        {
            // base implementation do nothing.
        }
    }
}
