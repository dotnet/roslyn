// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    [Export(typeof(VSTypeScriptGlobalOptions)), Shared]
    internal sealed class VSTypeScriptGlobalOptions
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptGlobalOptions(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public bool BlockForCompletionItems
        {
            get => _globalOptions.GetOption(CompletionViewOptions.BlockForCompletionItems, InternalLanguageNames.TypeScript);
            set => _globalOptions.SetGlobalOption(new OptionKey(CompletionViewOptions.BlockForCompletionItems, InternalLanguageNames.TypeScript), value);
        }
    }
}
