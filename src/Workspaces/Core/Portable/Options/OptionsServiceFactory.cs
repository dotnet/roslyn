// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Options
{
    [ExportWorkspaceServiceFactory(typeof(IOptionService), ServiceLayer.Default), Shared]
    internal class OptionsServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IOptionService _optionService;

        [ImportingConstructor]
        public OptionsServiceFactory(IOptionService optionService)
        {
            _optionService = optionService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _optionService;
        }
    }
}
