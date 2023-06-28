// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfig;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    [Guid(Guids.CSharpOptionPageCodeStyleIdString)]
    internal class CodeStylePage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            var editorService = (EditorConfigGeneratorService)serviceProvider.GetService(typeof(EditorConfigGeneratorService));
            return new GridOptionPreviewControl(
                serviceProvider,
                optionStore,
                (o, s) => new StyleViewModel(o, s),
                editorService.GetOptions(LanguageNames.CSharp),
                LanguageNames.CSharp);
        }

        internal readonly struct TestAccessor
        {
            internal static ImmutableArray<(string feature, ImmutableArray<IOption2> options)> GetEditorConfigOptions()
                => default;//new CSharpEditorConfigFileGenerator().GetEditorConfigOptions();
        }
    }
}
