// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Defines a dependency between source generators
    /// </summary>
    /// <remarks>
    /// Can be defined more than once for a generator.
    /// No effect when generator is not found.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class GenerateAfterAttribute : Attribute
    {
        /// <summary>
        /// Fully Qualified Name (FQN: namespace + class name) of the generator to execute before.
        /// </summary>
        /// <remarks>
        /// No effect if the generator is not found.
        /// </remarks>
        public string GeneratorToExecuteBefore { get; }

        /// <summary>
        /// Defines a dependency between source generators
        /// </summary>
        /// <param name="generatorToExecuteBefore">
        /// Fully Qualified Name (FQN: namespace + class name) of the generator to execute before
        /// </param>
        public GenerateAfterAttribute(string generatorToExecuteBefore)
        {
            if (string.IsNullOrWhiteSpace(generatorToExecuteBefore))
            {
                throw new ArgumentNullException(nameof(generatorToExecuteBefore));
            }
            GeneratorToExecuteBefore = generatorToExecuteBefore;
        }
    }
}
