// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Defines a <see cref="ISourceGenerator"/> which does not add any sources.
    /// </summary>
    public sealed class EmptySourceGeneratorProvider : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
