// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(RazorGlobalOptions)), Shared]
    internal sealed class RazorGlobalOptions
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorGlobalOptions(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        /// <summary>
        /// For testing purposes.
        /// </summary>
        public static RazorGlobalOptions GetGlobalOptions(Workspace workspace)
            => ((IMefHostExportProvider)workspace.Services.HostServices).GetExports<RazorGlobalOptions>().Single().Value;

        public RazorAutoFormattingOptions GetAutoFormattingOptions()
            => new(_globalOptions.GetAutoFormattingOptions(LanguageNames.CSharp));
    }
}
