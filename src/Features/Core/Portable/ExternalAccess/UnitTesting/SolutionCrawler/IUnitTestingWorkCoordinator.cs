﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal interface IUnitTestingWorkCoordinator
    {
        void OnWorkspaceChanged(WorkspaceChangeEventArgs args);
#if false // Not used in unit testing crawling
        void OnTextDocumentOpened(TextDocumentEventArgs args);
        void OnTextDocumentClosed(TextDocumentEventArgs args);
#endif
    }
}
