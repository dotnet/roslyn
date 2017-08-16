// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.NavigateTo
{
    [ExportVersionSpecific(typeof(INavigateToHostVersionService), VisualStudioVersion.Dev15)]
    internal partial class Dev15NavigateToHostVersionService : INavigateToHostVersionService
    {
        public bool GetSearchCurrentDocument(INavigateToOptions options)
        {
            var options2 = options as INavigateToOptions2;
            return options2?.SearchCurrentDocument ?? false;
        }

        public INavigateToItemDisplayFactory CreateDisplayFactory()
            => new Dev15ItemDisplayFactory();
    }
}
