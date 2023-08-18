// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options.EditorConfig;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    [Guid(Guids.CSharpOptionPageCodeStyleIdString)]
    internal class CodeStylePage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            var editorService = serviceProvider.GetMefService<EditorConfigOptionsGenerator>();

            return new GridOptionPreviewControl(
                serviceProvider,
                optionStore,
                (o, s) => new StyleViewModel(o, s),
                editorService.GetDefaultOptions(LanguageNames.CSharp),
                LanguageNames.CSharp);
        }
    }
}
