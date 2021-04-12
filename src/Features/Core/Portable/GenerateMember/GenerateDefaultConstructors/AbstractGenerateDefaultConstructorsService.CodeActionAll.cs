﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
    {
        private class CodeActionAll : AbstractCodeAction
        {
            public CodeActionAll(
                Document document,
                State state,
                IList<IMethodSymbol> constructors)
                : base(document, state, constructors, FeaturesResources.Generate_all)
            {
            }
        }
    }
}
