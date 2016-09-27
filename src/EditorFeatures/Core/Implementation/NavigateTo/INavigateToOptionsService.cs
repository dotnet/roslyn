// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal interface INavigateToOptionsService
    {
        bool GetSearchCurrentDocument(INavigateToOptions options);
    }

    [ExportVersionSpecific(typeof(INavigateToOptionsService), VisualStudioVersion.Dev14)]
    internal class Dev14NavigateToOptionsService : INavigateToOptionsService
    {
        public bool GetSearchCurrentDocument(INavigateToOptions options)
        {
            return false;
        }
    }
}