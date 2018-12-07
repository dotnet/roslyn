// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias WORKSPACES;

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Notification;
using WORKSPACES::Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    [Export(typeof(IMoveToNamespaceOptionsService))]
    [PartNotDiscoverable]
    class TestMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestMoveToNamespaceOptionsService()
        {
        }

        public Task<MoveToNamespaceOptionsResult> GetChangeNamespaceOptionsAsync(ISyntaxFactsService syntaxFactsService, INotificationService notificationService, string defaultNamespace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
