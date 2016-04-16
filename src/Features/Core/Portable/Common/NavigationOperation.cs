// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal class NavigationOperation : CodeActionOperation
    {
        private readonly DocumentId _documentId;
        private readonly int _position;

        public NavigationOperation(DocumentId documentId, int position)
        {
            _documentId = documentId;
            _position = position;
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            if (workspace.CanOpenDocuments)
            {
                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
                navigationService.TryNavigateToPosition(workspace, _documentId, _position);
            }
        }
    }
}
