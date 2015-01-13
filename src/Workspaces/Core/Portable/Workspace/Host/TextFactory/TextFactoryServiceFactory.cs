// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ITextFactoryService), ServiceLayer.Default), Shared]
    internal partial class TextFactoryServiceFactory : IWorkspaceServiceFactory
    {
        private readonly TextFactoryService singleton = new TextFactoryService();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return singleton;
        }
    }
}