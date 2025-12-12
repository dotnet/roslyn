// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages;

[Guid(Guids.RoslynOptionPagePerformanceFunctionIdIdString)]
internal sealed class PerformanceFunctionIdPage : AbstractOptionPage
{
    protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
    {
        return new InternalOptionsControl(FunctionIdOptions.GetOptions(), optionStore);
    }
}
