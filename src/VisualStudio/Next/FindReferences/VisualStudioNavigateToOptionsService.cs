// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    [ExportWorkspaceService(typeof(INavigateToOptionsService)), Shared]
    internal class VisualStudioNavigateToOptionsService : INavigateToOptionsService
    {
        public bool GetSearchCurrentDocument(INavigateToOptions options)
        {
            var options2 = options as INavigateToOptions2;
            return options2?.SearchCurrentDocument ?? false;
        }
    }
}