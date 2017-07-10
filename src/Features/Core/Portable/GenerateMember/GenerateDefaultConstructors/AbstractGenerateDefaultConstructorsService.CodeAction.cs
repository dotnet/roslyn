// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
    {
        private class GenerateDefaultConstructorCodeAction : AbstractCodeAction
        {
            public GenerateDefaultConstructorCodeAction(
                TService service,
                Document document,
                State state,
                IMethodSymbol constructor)
                : base(service, document, state, new[] { constructor }, GetDisplayText(state, constructor))
            {
            }

            private static string GetDisplayText(State state, IMethodSymbol constructor)
            {
                var parameters = constructor.Parameters.Select(p => p.Name);
                var parameterString = string.Join(", ", parameters);

                return string.Format(FeaturesResources.Generate_constructor_0_1,
                    state.ClassType.Name, parameterString);
            }
        }
    }
}
