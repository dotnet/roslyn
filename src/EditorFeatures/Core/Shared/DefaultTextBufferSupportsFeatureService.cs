// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
