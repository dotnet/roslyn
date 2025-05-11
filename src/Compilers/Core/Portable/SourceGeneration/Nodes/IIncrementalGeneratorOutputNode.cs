// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Internal representation of an incremental output
    /// </summary>
    internal interface IIncrementalGeneratorOutputNode
    {
        IncrementalGeneratorOutputKind Kind { get; }

        void AppendOutputs(IncrementalExecutionContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents the various output kinds of an <see cref="IIncrementalGenerator"/>. 
    /// </summary>
    /// <remarks>
    /// Can be passed as a bit field when creating a <see cref="GeneratorDriver"/> to selectively disable outputs.
    /// </remarks>
    [Flags]
    public enum IncrementalGeneratorOutputKind
    {
        /// <summary>
        /// Represents no output kinds. Can be used when creating a driver to indicate that no outputs should be disabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// A regular source output, registered via <see cref="IncrementalGeneratorInitializationContext.RegisterSourceOutput{TSource}(IncrementalValueProvider{TSource}, Action{SourceProductionContext, TSource})"/> 
        /// or <see cref="IncrementalGeneratorInitializationContext.RegisterSourceOutput{TSource}(IncrementalValuesProvider{TSource}, Action{SourceProductionContext, TSource})"/>
        /// </summary>
        Source = 0b1,

        /// <summary>
        /// A post-initialization output, which will be visible to later phases, registered via <see cref="IncrementalGeneratorInitializationContext.RegisterPostInitializationOutput(Action{IncrementalGeneratorPostInitializationContext})"/>
        /// </summary>
        PostInit = 0b10,

        /// <summary>
        /// An Implementation only source output, registered via <see cref="IncrementalGeneratorInitializationContext.RegisterImplementationSourceOutput{TSource}(IncrementalValueProvider{TSource}, Action{SourceProductionContext, TSource})"/>
        /// or <see cref="IncrementalGeneratorInitializationContext.RegisterImplementationSourceOutput{TSource}(IncrementalValuesProvider{TSource}, Action{SourceProductionContext, TSource})"/>
        /// </summary>
        Implementation = 0b100,

        /// <summary>
        /// A host specific output, registered via <see cref="IncrementalGeneratorInitializationContext.RegisterHostOutput{TSource}(IncrementalValueProvider{TSource}, Action{HostOutputProductionContext, TSource})"/> 
        /// or <see cref="IncrementalGeneratorInitializationContext.RegisterHostOutput{TSource}(IncrementalValuesProvider{TSource}, Action{HostOutputProductionContext, TSource})"/>
        /// </summary>
        Host = 0b1000,
    }
}
