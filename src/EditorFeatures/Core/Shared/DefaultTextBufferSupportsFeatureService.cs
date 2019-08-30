// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    [ExportWorkspaceService(typeof(ITextBufferSupportsFeatureService), ServiceLayer.Editor), Shared]
    internal sealed class DefaultTextBufferSupportsFeatureService : ITextBufferSupportsFeatureService
    {
        [ImportingConstructor]
        public DefaultTextBufferSupportsFeatureService()
        {
        }

        public bool SupportsCodeFixes(ITextBuffer textBuffer)
        {
            return true;
        }

        public bool SupportsRefactorings(ITextBuffer textBuffer)
        {
            return true;
        }

        public bool SupportsRename(ITextBuffer textBuffer)
        {
            return true;
        }

        public bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer)
        {
            return true;
        }
    }
}
