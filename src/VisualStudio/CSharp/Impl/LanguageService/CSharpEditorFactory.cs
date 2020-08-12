// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.FileHeaders;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FileHeaders;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExcludeFromCodeCoverage]
    [Guid(Guids.CSharpEditorFactoryIdString)]
    internal class CSharpEditorFactory : AbstractEditorFactory
    {
        public CSharpEditorFactory(IComponentModel componentModel)
            : base(componentModel)
        {
        }

        protected override string ContentTypeName => ContentTypeNames.CSharpContentType;
        protected override string LanguageName => LanguageNames.CSharp;
        protected override SyntaxGenerator SyntaxGenerator => CSharpSyntaxGenerator.Instance;
        protected override AbstractFileHeaderHelper FileHeaderHelper => CSharpFileHeaderHelper.Instance;

        protected override async Task<Document> OrganizeUsingsCreatedFromTemplateAsync(Document document, CancellationToken cancellationToken)
        {
            var organizedDocument = await base.OrganizeUsingsCreatedFromTemplateAsync(document, cancellationToken).ConfigureAwait(false);
            return await MisplacedUsingDirectivesCodeFixProvider.TransformDocumentIfRequiredAsync(organizedDocument, cancellationToken).ConfigureAwait(false);
        }
    }
}
