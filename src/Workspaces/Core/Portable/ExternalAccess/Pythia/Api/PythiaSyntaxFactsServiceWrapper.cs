// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal readonly struct PythiaSyntaxFactsServiceWrapper
    {
        internal readonly ISyntaxFactsService UnderlyingObject;

        internal PythiaSyntaxFactsServiceWrapper(ISyntaxFactsService underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        public static PythiaSyntaxFactsServiceWrapper Create(Document document)
            => new PythiaSyntaxFactsServiceWrapper(document.GetRequiredLanguageService<ISyntaxFactsService>());
    }
}
