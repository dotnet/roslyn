// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Debugging
{
    internal interface IProximityExpressionsService : ILanguageService
    {
        Task<IList<string>> GetProximityExpressionsAsync(Document document, int position, CancellationToken cancellationToken);
        Task<bool> IsValidAsync(Document document, int position, string expressionValue, CancellationToken cancellationToken);
    }
}
