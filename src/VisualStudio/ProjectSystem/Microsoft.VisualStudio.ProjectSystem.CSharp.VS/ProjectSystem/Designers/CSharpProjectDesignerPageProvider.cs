// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides project designer property pages.
    /// </summary>
    [Export(typeof(IVsProjectDesignerPageProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpProjectDesignerPageProvider : IVsProjectDesignerPageProvider
    {
        [ImportingConstructor]
        internal CSharpProjectDesignerPageProvider()
        {
        }

        public Task<IReadOnlyCollection<IPageMetadata>> GetPagesAsync()
        {
            return Task.FromResult<IReadOnlyCollection<IPageMetadata>>(
                new IPageMetadata[] {
                    CSharpProjectDesignerPage.Application,
            });
        }
    }
}
