// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExcludeFromCodeCoverage]
    [Guid(Guids.CSharpLanguageServiceIdString)]
    internal partial class CSharpLanguageService : AbstractLanguageService<CSharpPackage, CSharpLanguageService, CSharpProjectShim>
    {
        internal CSharpLanguageService(CSharpPackage package)
            : base(package)
        {
        }

        protected override Guid DebuggerLanguageId
        {
            get
            {
                return Guids.CSharpDebuggerLanguageId;
            }
        }

        protected override string ContentTypeName
        {
            get
            {
                return ContentTypeNames.CSharpContentType;
            }
        }

        public override Guid LanguageServiceId
        {
            get
            {
                return Guids.CSharpLanguageServiceId;
            }
        }

        protected override string LanguageName
        {
            get
            {
                return CSharpVSResources.C;
            }
        }

        protected override string RoslynLanguageName
        {
            get
            {
                return LanguageNames.CSharp;
            }
        }

        protected override AbstractDebuggerIntelliSenseContext CreateContext(
            IWpfTextView view,
            IVsTextView vsTextView,
            IVsTextLines debuggerBuffer,
            ITextBuffer subjectBuffer,
            Microsoft.VisualStudio.TextManager.Interop.TextSpan[] currentStatementSpan)
        {
            return new CSharpDebuggerIntelliSenseContext(view,
                vsTextView,
                debuggerBuffer,
                subjectBuffer,
                currentStatementSpan,
                this.Package.ComponentModel,
                this.SystemServiceProvider);
        }
    }
}
