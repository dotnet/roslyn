﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
    {
        private sealed class CodeActionAll(
            Document document,
            State state,
            IList<IMethodSymbol> constructors,
            CodeAndImportGenerationOptionsProvider fallbackOptions) : AbstractCodeAction(document, state, constructors, FeaturesResources.Generate_all, fallbackOptions)
        {
        }
    }
}
