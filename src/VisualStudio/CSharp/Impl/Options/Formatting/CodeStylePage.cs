// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;

[Guid(Guids.CSharpOptionPageCodeStyleIdString)]
internal sealed class CodeStylePage : AbstractOptionPage
{
    protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
    {
        var enumerator = serviceProvider.GetMefService<EditorConfigOptionsEnumerator>();

        return new GridOptionPreviewControl(
            serviceProvider,
            optionStore,
            (o, s) => new StyleViewModel(o, s),
            enumerator.GetOptions(LanguageNames.CSharp),
            LanguageNames.CSharp);
    }
}
