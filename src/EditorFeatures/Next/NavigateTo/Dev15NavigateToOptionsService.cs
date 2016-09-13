// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.NavigateTo
{
    [ExportVersionSpecific(typeof(INavigateToOptionsService), VisualStudioVersion.Dev15)]
    internal class Dev15NavigateToOptionsService : INavigateToOptionsService
    {
        public bool GetSearchCurrentDocument(INavigateToOptions options)
        {
            var options2 = options as INavigateToOptions2;
            return options2?.SearchCurrentDocument ?? false;
        }
    }
}