// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SerializerServiceFactory()
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SerializerService(workspaceServices);
    }
}
