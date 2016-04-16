// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.GeneratedCodeRecognition
{
    [ExportWorkspaceServiceFactory(typeof(IGeneratedCodeRecognitionService), ServiceLayer.Default), Shared]
    internal class GeneratedCodeRecognitionServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly IGeneratedCodeRecognitionService s_singleton = new GeneratedCodeRecognitionService();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return s_singleton;
        }

        private class GeneratedCodeRecognitionService : IGeneratedCodeRecognitionService
        {
            public bool IsGeneratedCode(Document document)
            {
                return IsFileNameForGeneratedCode(document.Name);
            }

            private static bool IsFileNameForGeneratedCode(string fileName)
            {
                if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                string extension = Path.GetExtension(fileName);
                if (extension != string.Empty)
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName);

                    if (fileName.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".generated", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
