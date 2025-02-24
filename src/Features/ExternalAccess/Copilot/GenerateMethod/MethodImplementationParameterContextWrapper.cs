// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class MethodImplementationParameterContextWrapper
    {
        private readonly MethodImplementationParameterContext _methodImplementationParameterContext;

        public MethodImplementationParameterContextWrapper(MethodImplementationParameterContext methodImplementationParameterContext)
        {
            _methodImplementationParameterContext = methodImplementationParameterContext;
        }

        public string Name => _methodImplementationParameterContext.Name;

        public string Type => _methodImplementationParameterContext.Type;

        public ImmutableArray<string> Modifiers => _methodImplementationParameterContext.Modifiers;
    }
}
