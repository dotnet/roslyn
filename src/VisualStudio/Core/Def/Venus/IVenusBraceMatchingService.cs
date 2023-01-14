// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal interface IVenusBraceMatchingService : ILanguageService
    {
        bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace);
    }
}
