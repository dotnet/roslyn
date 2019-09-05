// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Shared
{
    internal interface IDocumentSupportsFeatureService : IWorkspaceService
    {
        bool SupportsCodeFixes(Document document);
        bool SupportsRefactorings(Document document);
        bool SupportsRename(Document document);
        bool SupportsNavigationToAnyPosition(Document document);
    }


    [ExportWorkspaceService(typeof(IDocumentSupportsFeatureService), ServiceLayer.Default), Shared]
    internal class DefaultDocumentSupportsFeatureService : IDocumentSupportsFeatureService
    {
        [ImportingConstructor]
        public DefaultDocumentSupportsFeatureService()
        {
        }

        public bool SupportsCodeFixes(Document document)
            => true;

        public bool SupportsNavigationToAnyPosition(Document document)
            => true;

        public bool SupportsRefactorings(Document document)
            => true;

        public bool SupportsRename(Document document)
            => true;
    }
}
