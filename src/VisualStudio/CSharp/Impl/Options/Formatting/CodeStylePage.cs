// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
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
            return new GridOptionPreviewControl(
                serviceProvider,
                optionStore,
                (o, s) => new StyleViewModel(o, s),
                GetEditorConfigOptions(),
                LanguageNames.CSharp);
        }

        private static ImmutableArray<(string feature, ImmutableArray<IOption> options)> GetEditorConfigOptions()
        {
            var builder = ArrayBuilder<(string, ImmutableArray<IOption>)>.GetInstance();
            builder.AddRange(GridOptionPreviewControl.GetLanguageAgnosticEditorConfigOptions());
            builder.Add((CSharpVSResources.CSharp_Coding_Conventions, CSharpCodeStyleOptions.AllOptions.As<IOption>()));
            builder.Add((CSharpVSResources.CSharp_Formatting_Rules, CSharpFormattingOptions2.AllOptions.As<IOption>()));
            return builder.ToImmutableAndFree();
        }

        internal readonly struct TestAccessor
        {
            internal static ImmutableArray<(string feature, ImmutableArray<IOption> options)> GetEditorConfigOptions()
                => CodeStylePage.GetEditorConfigOptions();
        }
    }
}
