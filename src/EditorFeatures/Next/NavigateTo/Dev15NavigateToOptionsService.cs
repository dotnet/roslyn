// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.NavigateTo
{
    [ExportVersionSpecific(typeof(INavigateToHostVersionService), VisualStudioVersion.Dev15)]
    internal class Dev15NavigateToHostVersionService : Dev14NavigateToHostVersionService, INavigateToHostVersionService
    {
        [ImportingConstructor]
        public Dev15NavigateToHostVersionService(IGlyphService glyphService)
            : base(glyphService)
        {
        }

        public override bool GetSearchCurrentDocument(INavigateToOptions options)
        {
            var options2 = options as INavigateToOptions2;
            return options2?.SearchCurrentDocument ?? false;
        }
    }
}