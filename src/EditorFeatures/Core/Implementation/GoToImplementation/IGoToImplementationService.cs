// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    interface IGoToImplementationService : ILanguageService
    {
        /// <summary>
        /// Finds the implementations for the symbol at the specific position in the document and then 
        /// navigates to them.
        /// </summary>
        /// <returns>True if navigating to the implementation of the symbol at the provided position succeeds.  False, otherwise.</returns>
        bool TryGoToImplementation(Document document, int position, CancellationToken cancellationToken, out string message);
    }
}
