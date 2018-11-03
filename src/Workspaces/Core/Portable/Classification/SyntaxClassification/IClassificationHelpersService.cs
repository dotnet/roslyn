// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IClassificationHelpersService : ILanguageService
    {
        string GetClassification(SyntaxToken token);
    }
}
