// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public static class GeneratorExtensions
    {
        /// <summary>
        /// Returns the underlying type of a given generator
        /// </summary>
        /// <remarks>
        /// For <see cref="IIncrementalGenerator"/>s a wrapper is created that also implements
        /// <see cref="ISourceGenerator"/>. This method will unwrap and return the underlying type
        /// in those cases.
        /// </remarks>
        /// <param name="generator">The generator to get the type of</param>
        /// <returns>The underlying generator type</returns>
        public static Type GetGeneratorType(this ISourceGenerator generator)
        {
            if (generator is IncrementalGeneratorWrapper igw)
            {
                return igw.Generator.GetType();
            }
            return generator.GetType();
        }

        /// <summary>
        /// Returns the underlying type of a given generator
        /// </summary>
        /// <param name="generator">The generator to get the type of</param>
        /// <returns>The underlying generator type</returns>
        public static Type GetGeneratorType(this IIncrementalGenerator generator)
        {
            if (generator is SourceGeneratorAdaptor adaptor)
            {
                return adaptor.SourceGenerator.GetType();
            }
            return generator.GetType();
        }

        /// <summary>
        /// Converts an <see cref="IIncrementalGenerator"/> into an <see cref="ISourceGenerator"/> object that can be used when constructing a <see cref="GeneratorDriver"/>
        /// </summary>
        /// <param name="incrementalGenerator">The incremental generator to wrap</param>
        /// <returns>A wrapped generator that can be passed to a generator driver</returns>
        public static ISourceGenerator AsSourceGenerator(this IIncrementalGenerator incrementalGenerator) => incrementalGenerator switch
        {
            SourceGeneratorAdaptor adaptor => adaptor.SourceGenerator,
            _ => new IncrementalGeneratorWrapper(incrementalGenerator)
        };

        /// <summary>
        /// Converts an <see cref="ISourceGenerator"/> into an <see cref="IIncrementalGenerator"/>
        /// </summary>
        /// <param name="sourceGenerator">The source generator to convert</param>
        /// <returns>An incremental generator</returns>
        public static IIncrementalGenerator AsIncrementalGenerator(this ISourceGenerator sourceGenerator) => sourceGenerator switch
        {
            IncrementalGeneratorWrapper wrapper => wrapper.Generator,
            _ => new SourceGeneratorAdaptor(sourceGenerator, SourceGeneratorAdaptor.DummySourceExtension)
        };
    }
}
