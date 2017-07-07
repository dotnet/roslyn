// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
    {
        private class CodeActionAll : AbstractCodeAction
        {
            public CodeActionAll(
                TService service,
                Document document,
                State state,
                IList<IMethodSymbol> constructors)
                : base(service, document, state, constructors, FeaturesResources.Generate_all)
            {
            }
        }
    }
}
