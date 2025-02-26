// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class ParameterContextWrapper(ParameterContext context)
    {
        public string Name => context.Name;
        public string Type => context.Type;
        public ImmutableArray<string> Modifiers => context.Modifiers;
    }
}
