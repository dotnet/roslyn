// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide document different service specific to remote workspace's behavior.
    /// 
    /// default <see cref="AbstractDocumentDifferenceService"/> is optimized for typing case in editor where we have events
    /// for each typing. but in remote workspace, we aggregate changes and update solution in bulk and we don't have concept
    /// of active file making default implementation not suitable. functionally, default one is still correct. but it often
    /// time makes us to do more than we need. basically, it always says this project has semantic change which can cause
    /// a lot of re-analysis.
    /// </summary>
    internal class RemoteDocumentDifferenceService : IDocumentDifferenceService
    {
        [ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.CSharp, layer: WorkspaceKind.Host), Shared]
        internal class CSharpDocumentDifferenceService : RemoteDocumentDifferenceService
        {
            [ImportingConstructor]
            public CSharpDocumentDifferenceService()
            {
            }
        }

        [ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.VisualBasic, layer: WorkspaceKind.Host), Shared]
        internal class VisualBasicDocumentDifferenceService : AbstractDocumentDifferenceService
        {
            [ImportingConstructor]
            public VisualBasicDocumentDifferenceService()
            {
            }
        }

        public async Task<DocumentDifferenceResult> GetDifferenceAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        {
            // in remote workspace, we don't trust any version based on VersionStamp. we only trust content based information such as
            // checksum or tree comparison and etc.

            // first check checksum
            var oldTextChecksum = (await oldDocument.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false)).Text;
            var newTextChecksum = (await newDocument.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false)).Text;
            if (oldTextChecksum == newTextChecksum)
            {
                // null means nothing has changed.
                return null;
            }

            var oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot.IsEquivalentTo(newRoot, topLevel: true))
            {
                // only method body changed
                return new DocumentDifferenceResult(InvocationReasons.SyntaxChanged);
            }

            // semantic has changed as well.
            return new DocumentDifferenceResult(InvocationReasons.DocumentChanged);
        }
    }
}
