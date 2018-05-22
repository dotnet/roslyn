// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Experiment
{
    internal interface IDocumentServiceFactory
    {
        /// <summary>
        /// For now, there is no constraint for the service. it is basically a backdoor to
        /// get anything from this factory provider
        /// </summary>
        TService GetService<TService>();
    }
}
