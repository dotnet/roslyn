// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    [ExportVersionSpecific(typeof(INavigateToHostVersionService), VisualStudioVersion.Dev14)]
    internal partial class Dev14NavigateToHostVersionService : INavigateToHostVersionService
    {
        private readonly IGlyphService _glyphService;

        [ImportingConstructor]
        public Dev14NavigateToHostVersionService(
            IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public virtual bool GetSearchCurrentDocument(INavigateToOptions options)
        {
            return false;
        }

        public virtual INavigateToItemDisplayFactory CreateDisplayFactory()
        {
            return new Dev14ItemDisplayFactory(new NavigateToIconFactory(_glyphService));
        }
    }
}