// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    [ExportWorkspaceService(typeof(IDocumentSupportsFeatureService), ServiceLayer.Editor), Shared]
    internal sealed class DefaultDocumentSupportsFeatureService : IDocumentSupportsFeatureService
    {
        public bool SupportsCodeFixes(Document document)
        {
            return true;
        }

        public bool SupportsRefactorings(Document document)
        {
            return true;
        }

        public bool SupportsRename(Document document)
        {
            return true;
        }

        public bool SupportsNavigationToAnyPosition(Document document)
        {
            return true;
        }
    }
}
