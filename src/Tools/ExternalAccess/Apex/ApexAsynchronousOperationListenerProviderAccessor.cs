// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Apex
{
    [Export(typeof(IApexAsynchronousOperationListenerProviderAccessor))]
    [Shared]
    internal sealed class ApexAsynchronousOperationListenerProviderAccessor : IApexAsynchronousOperationListenerProviderAccessor
    {
        private readonly AsynchronousOperationListenerProvider _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ApexAsynchronousOperationListenerProviderAccessor(AsynchronousOperationListenerProvider implementation)
        {
            _implementation = implementation;
        }

        public Task WaitAllAsync(string[] featureNames = null, Action eventProcessingAction = null)
            => _implementation.WaitAllAsync(featureNames, eventProcessingAction);
    }
}
