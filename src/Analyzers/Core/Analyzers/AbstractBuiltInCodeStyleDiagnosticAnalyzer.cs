// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract partial class AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// unique <see cref="IOption2"/> code style option.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="option">
        /// Code style option that can be used to configure the given <paramref name="diagnosticId"/>.
        /// <see langword="null"/>, if there is no such unique option.
        /// </param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// <see langword="null"/> if the message is identical to the title.
        /// </param>
        /// <param name="isUnnecessary"><see langword="true"/> if the diagnostic is reported on unnecessary code; otherwise, <see langword="false"/>.</param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            IOption2? option,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, enforceOnBuild, title, messageFormat, isUnnecessary, configurable)
        {
            AddDiagnosticIdToOptionMapping(diagnosticId, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor and
        /// two or more <see cref="IOption2 "/> code style options.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
        /// <param name="options">
        /// Set of two or more code style options that can be used to configure the diagnostic severity of the given diagnosticId.
        /// </param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// Null if the message is identical to the title.
        /// </param>
        /// <param name="isUnnecessary"><see langword="true"/> if the diagnostic is reported on unnecessary code; otherwise, <see langword="false"/>.</param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            EnforceOnBuild enforceOnBuild,
            ImmutableHashSet<IOption2> options,
            LocalizableString title,
            LocalizableString? messageFormat = null,
            bool isUnnecessary = false,
            bool configurable = true)
            : this(diagnosticId, enforceOnBuild, title, messageFormat, isUnnecessary, configurable)
        {
            RoslynDebug.Assert(options != null);
            Debug.Assert(options.Count > 1);
            AddDiagnosticIdToOptionMapping(diagnosticId, options);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with a code style option that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableDictionary<DiagnosticDescriptor, IOption2> supportedDiagnosticsWithOptions)
            : this(supportedDiagnosticsWithOptions.Keys.ToImmutableArray())
        {
            foreach (var (descriptor, option) in supportedDiagnosticsWithOptions)
                AddDiagnosticIdToOptionMapping(descriptor.Id, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with zero or more code style options that can be used to configure each descriptor.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableDictionary<DiagnosticDescriptor, ImmutableHashSet<IOption2>> supportedDiagnosticsWithOptions)
            : this(supportedDiagnosticsWithOptions.Keys.ToImmutableArray())
        {
            foreach (var (descriptor, options) in supportedDiagnosticsWithOptions)
            {
                if (!options.IsEmpty)
                {
                    AddDiagnosticIdToOptionMapping(descriptor.Id, options);
                }
            }
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, IOption2? option)
        {
            if (option != null)
            {
                AddDiagnosticIdToOptionMapping(diagnosticId, ImmutableHashSet.Create(option));
            }
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, ImmutableHashSet<IOption2> options)
            => IDEDiagnosticIdToOptionMappingHelper.AddOptionMapping(diagnosticId, options);

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        public virtual bool OpenFileOnly(SimplifierOptions? options)
            => false;
    }
}
