// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal interface INavigateToOptionsService : IWorkspaceService
    {
        bool GetSearchCurrentDocument(INavigateToOptions options);
    }

    [ExportWorkspaceService(typeof(INavigateToOptionsService)), Shared]
    internal class DefaultNavigateToOptionsService : INavigateToOptionsService
    {
        public bool GetSearchCurrentDocument(INavigateToOptions options)
        {
            return false;
        }
    }
}