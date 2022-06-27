// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Provides context information for argument completion.
    /// </summary>
    internal sealed class ArgumentContext
    {
        public ArgumentContext(
            ArgumentProvider provider,
            OptionSet options,
            SemanticModel semanticModel,
            int position,
            IParameterSymbol parameter,
            string? previousValue,
            CancellationToken cancellationToken)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            SemanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
            Position = position;
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            PreviousValue = previousValue;
            CancellationToken = cancellationToken;
        }

        internal ArgumentProvider Provider { get; }

        /// <summary>
        /// Gets the effective options where argument completion is requested.
        /// </summary>
        public OptionSet Options { get; }

        /// <summary>
        /// Gets the semantic model where argument completion is requested.
        /// </summary>
        public SemanticModel SemanticModel { get; }

        /// <summary>
        /// Gets the position within <see cref="SemanticModel"/> where argument completion is requested.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets the symbol for the parameter for which an argument value is requested.
        /// </summary>
        public IParameterSymbol Parameter { get; }

        /// <summary>
        /// Gets the previously-provided argument value for this parameter.
        /// </summary>
        /// <value>
        /// The existing text of the argument value, if the argument is already in code; otherwise,
        /// <see langword="null"/> when requesting a new argument value.
        /// </value>
        public string? PreviousValue { get; }

        /// <summary>
        /// Gets a cancellation token that argument providers may observe.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets or sets the default argument value.
        /// </summary>
        /// <remarks>
        /// If this value is not set, the argument completion session will insert a language-specific default value for
        /// the argument.
        /// </remarks>
        public string? DefaultValue { get; set; }
    }
}
