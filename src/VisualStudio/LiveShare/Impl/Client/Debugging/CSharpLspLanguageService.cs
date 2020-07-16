// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Debugging
{
    [Guid(StringConstants.CSharpLspLanguageServiceGuidString)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2302:FlagServiceProviders")]
    internal class CSharpLspLanguageService : AbstractLanguageService<CSharpLspPackage, CSharpLspLanguageService>
    {
        public static readonly Guid LanguageServiceGuid = new Guid(StringConstants.CSharpLspLanguageServiceGuidString);

        internal CSharpLspLanguageService(CSharpLspPackage package)
            : base(package)
        {
        }

        internal IComponentModel ComponentModel => (IComponentModel)SystemServiceProvider.GetService(typeof(SComponentModel));

        protected override Guid DebuggerLanguageId { get; } = new Guid(StringConstants.CSharpLspDebuggerLanguageGuidString);

        public override Guid LanguageServiceId { get; } = LanguageServiceGuid;

        protected override string ContentTypeName => ContentTypeNames.CSharpLspContentTypeName;

        protected override string LanguageName => StringConstants.CSharpLspLanguageName;

        protected override string RoslynLanguageName => StringConstants.CSharpLspLanguageName;

        protected override AbstractDebuggerIntelliSenseContext CreateContext(
            IWpfTextView view,
            IVsTextView vsTextView,
            IVsTextLines debuggerBuffer,
            ITextBuffer subjectBuffer,
            TextSpan[] currentStatementSpan)
        {
            return new CSharpLspDebuggerIntelliSenseContext(view,
                vsTextView,
                debuggerBuffer,
                subjectBuffer,
                currentStatementSpan,
                Package.ComponentModel,
                SystemServiceProvider);
        }
    }
}
