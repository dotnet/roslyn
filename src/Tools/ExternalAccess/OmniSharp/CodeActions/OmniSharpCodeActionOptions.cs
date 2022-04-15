// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeActions
{
    internal readonly record struct OmniSharpCodeActionOptions(
        OmniSharpImplementTypeOptions ImplementTypeOptions)
    {
        internal CodeActionOptions GetCodeActionOptions()
            => new(ImplementTypeOptions: new(
                InsertionBehavior: (ImplementTypeInsertionBehavior)ImplementTypeOptions.InsertionBehavior,
                PropertyGenerationBehavior: (ImplementTypePropertyGenerationBehavior)ImplementTypeOptions.PropertyGenerationBehavior));
    }
}
