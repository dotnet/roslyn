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
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExcludeFromCodeCoverage]
    [Guid(Guids.CSharpLanguageServiceIdString)]
    internal partial class CSharpLanguageService : AbstractLanguageService<CSharpPackage, CSharpLanguageService>
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
                return CSharpVSResources.CSharp;
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

        protected override IVsContainedLanguage CreateContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator, AbstractProject project, IVsHierarchy hierarchy, uint itemid)
        {
            // this is not important. just plumbing code for prototype
            return new ContainedLanguage<CSharpPackage, CSharpLanguageService>(
                bufferCoordinator, this.Package.ComponentModel, project, hierarchy, itemid,
                this, SourceCodeKind.Regular, new StatementOrMemberBaseIndentationFormattingRule());
        }

        /// <summary>
        ///  this will probably mess things up. this is just a prototype to show the intention.
        ///  probably only work for smart indenter 
        /// </summary>
        internal class StatementOrMemberBaseIndentationFormattingRule : AbstractFormattingRule
        {
            public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
            {
                switch (node)
                {
                    case StatementSyntax _:
                    case MemberDeclarationSyntax _:
                        if (node.Span.Length > 0)
                        {
                            var firstToken = node.GetFirstToken(includeZeroWidth: true);
                            var lastToken = node.GetLastToken(includeZeroWidth: true);
                            var delta = 0;
                            list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(firstToken, firstToken.GetNextToken(includeZeroWidth: true), lastToken, delta, IndentBlockOption.RelativePosition));
                        }
                        break;
                }

                // pass along to the next chain
                base.AddIndentBlockOperations(list, node, optionSet, nextOperation);
            }
        }
    }
}
