// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
