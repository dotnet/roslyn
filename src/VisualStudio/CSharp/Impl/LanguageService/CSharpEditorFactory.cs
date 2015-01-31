// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExcludeFromCodeCoverage]
    [Guid(Guids.CSharpEditorFactoryIdString)]
    internal class CSharpEditorFactory : AbstractEditorFactory
    {
        public CSharpEditorFactory(CSharpPackage package)
            : base(package)
        {
        }

        protected override string ContentTypeName
        {
            get { return "CSharp"; }
        }

        protected override IList<TextChange> GetFormattedTextChanges(VisualStudioWorkspace workspace, string filePath, SourceText text, CancellationToken cancellationToken)
        {
            var root = SyntaxFactory.ParseSyntaxTree(text, path: filePath, cancellationToken: cancellationToken).GetRoot(cancellationToken);
            return Formatter.GetFormattedTextChanges(root, workspace, cancellationToken: cancellationToken);
        }
    }
}
