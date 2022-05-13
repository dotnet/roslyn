// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Defines a dependent source generators, which should be executed after
    /// </summary>
    /// <remarks>
    /// Can be defined more than once for a generator.
    /// No effect when generator is not found.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class GenerateBeforeAttribute : Attribute
    {
        /// <summary>
        /// Fully Qualified Name (FQN: namespace + class name) of the generator to execute after.
        /// </summary>
        /// <remarks>
        /// No effect if the generator is not found.
        /// </remarks>
        public string GeneratorToExecuteAfter { get; }

        /// <summary>
        /// Defines a dependent source generators, which should be executed after
        /// </summary>
        /// <param name="generatorToExecuteAfter">
        /// Fully Qualified Name (FQN: namespace + class name) of the generator to execute after.
        /// </param>
        public GenerateBeforeAttribute(string generatorToExecuteAfter)
        {
            if (string.IsNullOrWhiteSpace(generatorToExecuteAfter))
            {
                throw new ArgumentNullException(nameof(generatorToExecuteAfter));
            }
            GeneratorToExecuteAfter = generatorToExecuteAfter;
        }
    }
}
