// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Xaml
{
    /// <summary>
    /// TODO: move this to Microsoft.VisualStudio.LanguageServices.Xaml.
    /// https://github.com/dotnet/roslyn/issues/56324
    /// 
    /// Currently GlobalOptionService.CreateLazySerializableOptionsByLanguage loads all IOptionProvider types eagerly to determine whether or not they contribute to solution options.
    /// This is causing RPS regression.
    /// </summary>
    internal sealed class XamlOptions
    {
        public static readonly Option2<bool> EnableLspIntelliSenseFeatureFlag = new("xaml_enable_lsp_intellisense", defaultValue: false);
    }
}
