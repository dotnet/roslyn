// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Guid(Guids.RoslynOptionPageInternalSolutionCrawlerIdString)]
    internal class InternalSolutionCrawlerPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            return new InternalOptionsControl(nameof(InternalSolutionCrawlerOptions), optionStore);
        }
    }
}
