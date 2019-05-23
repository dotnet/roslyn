// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractBuiltInCodeStyleDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        /// <summary>
        /// Constructor for a code style analyzer with a single diagnostic descriptor.
        /// </summary>
        /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
        /// <param name="option">
        /// Option that can be used to configure the diagnostic severity of the given diagnosticId.
        /// Null, if there is no such unique option.
        /// </param>
        /// <param name="title">Title for the diagnostic descriptor</param>
        /// <param name="messageFormat">
        /// Message for the diagnostic descriptor.
        /// Null if the message is identical to the title.
        /// </param>
        /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string diagnosticId,
            IOption option,
            LocalizableString title,
            LocalizableString messageFormat = null,
            bool configurable = true)
            : base(diagnosticId, title, messageFormat, configurable)
        {
            AddDiagnosticIdToOptionMapping(diagnosticId, option);
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors with options that can be used to configure the diagnostic severity of each descriptor Id.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableDictionary<DiagnosticDescriptor, IOption> supportedDiagnosticsWithOptions)
            : base(supportedDiagnosticsWithOptions.Keys.ToImmutableArray())
        {
            foreach (var kvp in supportedDiagnosticsWithOptions)
            {
                AddDiagnosticIdToOptionMapping(kvp.Key.Id, kvp.Value);
            }
        }

        /// <summary>
        /// Constructor for a code style analyzer with a multiple diagnostic descriptors such that all the descriptors have no unique code style option to configure the descriptor Id.
        /// </summary>
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> supportedDiagnosticsWithoutOptions)
            : base(supportedDiagnosticsWithoutOptions)
        {
        }

        private static void AddDiagnosticIdToOptionMapping(string diagnosticId, IOption option)
        {
            if (option != null)
            {
                IDEDiagnosticIdToOptionMappingHelper.AddOptionMapping(diagnosticId, option);
            }
        }

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        public virtual bool OpenFileOnly(Workspace workspace)
            => false;
    }
}
