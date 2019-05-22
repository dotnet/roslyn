// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Apex
{
    internal interface IApexAsynchronousOperationListenerProviderAccessor
    {
        Task WaitAllAsync(string[] featureNames = null, Action eventProcessingAction = null);
    }
}
