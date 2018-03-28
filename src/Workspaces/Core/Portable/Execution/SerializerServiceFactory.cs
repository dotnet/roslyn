﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(ISerializerService), layer: ServiceLayer.Default), Shared]
    internal class SerializerServiceFactory : IWorkspaceServiceFactory
    {
        [Obsolete("This is the factory method for " + nameof(SerializerService) + ".", error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SerializerService(workspaceServices);
        }
    }
}
