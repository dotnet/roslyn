// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentationProviderService), ServiceLayer.Default), Shared]
    internal sealed class DocumentationProviderServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new DocumentationProviderService();
        }

        internal sealed class DocumentationProviderService : IDocumentationProviderService
        {
            private readonly ConcurrentDictionary<string, DocumentationProvider> _assemblyPathToDocumentationProviderMap =
                new ConcurrentDictionary<string, DocumentationProvider>();

            public DocumentationProvider GetDocumentationProvider(string assemblyPath)
            {
                if (assemblyPath == null)
                {
                    throw new ArgumentNullException(nameof(assemblyPath));
                }

                string xmlDocumentationFilePath;
                if (!TryFindDocumentationFile(assemblyPath, out xmlDocumentationFilePath))
                {
                    return _assemblyPathToDocumentationProviderMap.GetOrAdd(nameof(DocumentationProvider.Default), DocumentationProvider.Default);
                }

                return _assemblyPathToDocumentationProviderMap.GetOrAdd(xmlDocumentationFilePath, _path => new FileBasedXmlDocumentationProvider(_path));
            }

            private bool TryFindDocumentationFile(string assemblyPath, out string xmlDocumentationFilePath)
            {
                var xmlFileName = Path.ChangeExtension(Path.GetFileName(assemblyPath), "xml");
                var originalDirectory = Path.GetDirectoryName(assemblyPath);

                // 1. Look in subdirectories based on current culture
                var culture = CultureInfo.CurrentUICulture;
                var xmlFilePath = string.Empty;

                while (culture != CultureInfo.InvariantCulture)
                {
                    xmlFilePath = Path.Combine(originalDirectory, culture.Name, xmlFileName);
                    if (File.Exists(xmlFilePath))
                    {
                        xmlDocumentationFilePath = xmlFilePath;
                        return true;
                    }

                    culture = culture.Parent;
                }

                // 2. Look in the same directory as the assembly itself
                xmlFilePath = Path.Combine(originalDirectory, xmlFileName);
                if (File.Exists(xmlFilePath))
                {
                    xmlDocumentationFilePath = xmlFilePath;
                    return true;
                }

                xmlDocumentationFilePath = null;
                return false;
            }
        }
    }
}
