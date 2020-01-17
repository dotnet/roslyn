// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal interface IChangeSignatureOptionsService : IWorkspaceService
    {
        /// <summary>
        /// Changes signature of the symbol (currently a method symbol or an event symbol)
        /// </summary>
        /// <param name="document">the context document</param>
        /// <param name="insertPosition">the position in the document with the signature of the method</param>
        /// <param name="symbol">the symbol for changing the signature</param>
        /// <param name="parameters">existing parameters of the symbol</param>
        /// <returns></returns>
        ChangeSignatureOptionsResult GetChangeSignatureOptions(
            Document document,
            int insertPosition,
            ISymbol symbol,
            ParameterConfiguration parameters);

        /// <summary>
        /// Gets an added parameter (e.g. from the user input) and adds it to the change signature parameters.
        /// </summary>
        /// <param name="document">the context document</param>
        /// <param name="insertPosition">the position in the document with the signature of the method</param>
        /// <returns></returns>
        AddedParameter? GetAddedParameter(Document document, int insertPosition);
    }
}
