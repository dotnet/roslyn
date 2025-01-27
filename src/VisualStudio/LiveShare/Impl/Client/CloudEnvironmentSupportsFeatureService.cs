// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [ExportWorkspaceService(typeof(ITextBufferSupportsFeatureService), [WorkspaceKind.CloudEnvironmentClientWorkspace]), Shared]
    internal class CloudEnvironmentSupportsFeatureService : ITextBufferSupportsFeatureService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CloudEnvironmentSupportsFeatureService()
        {
        }

        public bool SupportsCodeFixes(ITextBuffer textBuffer) => false;

        public bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer) => false;

        public bool SupportsRefactorings(ITextBuffer textBuffer) => false;

        public bool SupportsRename(ITextBuffer textBuffer) => false;
    }
}
