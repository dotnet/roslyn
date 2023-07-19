// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class SourceGeneratedFileItem : BaseItem
    {
        private readonly IThreadingContext _threadingContext;
        private readonly string _languageName;

        public SourceGeneratedFileItem(
            IThreadingContext threadingContext,
            DocumentId documentId,
            string hintName,
            string languageName,
            Workspace workspace)
            : base(name: hintName)
        {
            _threadingContext = threadingContext;
            DocumentId = documentId;
            HintName = hintName;
            _languageName = languageName;
            Workspace = workspace;
        }

        public DocumentId DocumentId { get; }
        public string HintName { get; }
        public Workspace Workspace { get; }

        public override ImageMoniker IconMoniker
            => _languageName switch
            {
                LanguageNames.CSharp => KnownMonikers.CSFileNode,
                LanguageNames.VisualBasic => KnownMonikers.VBFileNode,
                _ => KnownMonikers.Document
            };

        public override object GetBrowseObject()
        {
            return new BrowseObject(this);
        }

        public override IInvocationController InvocationController
            => new InvocationControllerImpl(_threadingContext);

        private sealed class InvocationControllerImpl : IInvocationController
        {
            private readonly IThreadingContext _threadingContext;

            public InvocationControllerImpl(IThreadingContext threadingContext)
            {
                _threadingContext = threadingContext;
            }

            public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
            {
                return _threadingContext.JoinableTaskFactory.Run(async () =>
                {
                    var didNavigate = false;
                    foreach (var item in items.OfType<SourceGeneratedFileItem>())
                    {
                        var documentNavigationService = item.Workspace.Services.GetService<IDocumentNavigationService>();
                        if (documentNavigationService != null)
                        {
                            // TODO: we're navigating back to the top of the file, do we have a way to just bring it to the focus and that's it?
                            // TODO: Use a threaded-wait-dialog here so we can cancel navigation.
                            didNavigate |= await documentNavigationService.TryNavigateToPositionAsync(
                                item._threadingContext, item.Workspace, item.DocumentId, position: 0, CancellationToken.None).ConfigureAwait(false);
                        }
                    }

                    return didNavigate;
                });
            }
        }
    }
}
