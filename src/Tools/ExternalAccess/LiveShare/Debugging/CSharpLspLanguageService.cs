// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Debugging
{
    [Guid(StringConstants.CSharpLspLanguageServiceGuidString)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2302:FlagServiceProviders")]
    internal class CSharpLspLanguageService : AbstractLanguageService<CSharpLspPackage, CSharpLspLanguageService>
    {
        public static Guid LanguageServiceGuid { get; } = new Guid(StringConstants.CSharpLspLanguageServiceGuidString);

        internal CSharpLspLanguageService(CSharpLspPackage package)
            : base(package)
        {
        }

        internal IComponentModel ComponentModel => (IComponentModel)SystemServiceProvider.GetService(typeof(SComponentModel));

        protected override Guid DebuggerLanguageId { get; } = new Guid(StringConstants.CSharpLspDebuggerLanguageGuidString);

        public override Guid LanguageServiceId { get; } = new Guid(StringConstants.CSharpLspLanguageServiceGuidString);

        protected override string ContentTypeName => StringConstants.CSharpLspContentTypeName;

        protected override string LanguageName => StringConstants.CSharpLspLanguageName;

        protected override string RoslynLanguageName => StringConstants.CSharpLspLanguageName;

        public static CSharpLspLanguageService FromServiceProvider(IServiceProvider serviceProvider) =>
            VisualStudio.LanguageServices.Implementation.Interop.ComAggregate.GetManagedObject<CSharpLspLanguageService>(serviceProvider.GetService(typeof(CSharpLspLanguageService)));

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
