// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal interface IChangeSignatureOptionsService : IWorkspaceService
    {
        ChangeSignatureOptionsResult GetChangeSignatureOptions(
            ISymbol symbol,
            int insertPosition,
            ParameterConfiguration parameters,
            Document document);

        AddedParameterResult GetAddedParameter(Document document, int insertPosition);
    }
}
