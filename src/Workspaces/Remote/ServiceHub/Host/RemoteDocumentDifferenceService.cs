// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide document difference service specific to remote workspace's behavior.
    /// 
    /// Default <see cref="AbstractDocumentDifferenceService"/> is optimized for typing case in editor where we have events
    /// for each typing. But in remote workspace, we aggregate changes and update solution in bulk and we don't have concept
    /// of active file making default implementation unsuitable. Functionally, default one is still correct, but it often
    /// time makes us to do more than we need. Basically, it always says this project has semantic change which can cause
    /// a lot of re-analysis.
    /// </summary>
    internal class RemoteDocumentDifferenceService : IDocumentDifferenceService
    {
        [ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.CSharp, layer: WorkspaceKind.Host), Shared]
        internal sealed class CSharpDocumentDifferenceService : RemoteDocumentDifferenceService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public CSharpDocumentDifferenceService()
            {
            }
        }

        [ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.VisualBasic, layer: WorkspaceKind.Host), Shared]
        internal sealed class VisualBasicDocumentDifferenceService : AbstractDocumentDifferenceService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public VisualBasicDocumentDifferenceService()
            {
            }
        }

        public async Task<DocumentDifferenceResult?> GetDifferenceAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
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

            // the service is only registered for C# and VB documents, which must have syntax trees:
            Contract.ThrowIfNull(oldRoot);
            Contract.ThrowIfNull(newRoot);

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
