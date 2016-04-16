// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IAnalyzerService), ServiceLayer.Default), Shared]
    internal sealed class SimpleAnalyzerAssemblyLoaderService : IAnalyzerService
    {
        private readonly SimpleAnalyzerAssemblyLoader _loader = new SimpleAnalyzerAssemblyLoader();

        public IAnalyzerAssemblyLoader GetLoader()
        {
            return _loader;
        }
    }
}
