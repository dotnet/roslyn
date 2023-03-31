// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal abstract partial class CodeActionRequestPriorityProvider
    {
        /// <summary>
        /// Default provider with <see cref="CodeActionRequestPriority.None"/> <see cref="Priority"/>.
        /// </summary>
        public static readonly CodeActionRequestPriorityProvider Default = DefaultProvider.Instance;

        protected CodeActionRequestPriorityProvider(CodeActionRequestPriority priority)
        {
            Priority = priority;
        }

        public CodeActionRequestPriority Priority { get; }

        protected abstract bool IsDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer);

        /// <summary>
        /// Tracks the given <paramref name="analyzer"/> as a de-prioritized analyzer that should be moved to
        /// <see cref="CodeActionRequestPriority.Low"/> bucket.
        /// </summary>
        public abstract void AddDeprioritizedAnalyzerWithLowPriority(DiagnosticAnalyzer analyzer);

        public abstract CodeActionRequestPriorityProvider With(CodeActionRequestPriority priority);

        public static CodeActionRequestPriorityProvider Create(CodeActionRequestPriority priority)
        {
            if (priority == CodeActionRequestPriority.None)
                return Default;

            return MutableProvider.Create(priority);
        }

        /// <summary>
        /// Returns true if the given <paramref name="analyzer"/> can report diagnostics that can have
        /// fixes from a code fix provider with <see cref="CodeFixProvider.RequestPriority"/>
        /// matching <see cref="Priority"/>.
        /// This method is useful for performing a performance optimization for lightbulb diagnostic computation,
        /// wherein we can reduce the set of analyzers to be executed when computing fixes for a specific
        /// <see cref="Priority"/>.
        /// </summary>
        public bool MatchesPriority(DiagnosticAnalyzer analyzer)
        {
            // If caller isn't asking for prioritized result, then run all analyzers.
            if (Priority == CodeActionRequestPriority.None)
                return true;

            // 'CodeActionRequestPriority.Lowest' is used for suppression/configuration fixes,
            // which requires all analyzer diagnostics.
            if (Priority == CodeActionRequestPriority.Lowest)
                return true;

            // The compiler analyzer always counts for any priority.  It's diagnostics may be fixed
            // by high pri or normal pri fixers.
            if (analyzer.IsCompilerAnalyzer())
                return true;

            // Check if we are computing diagnostics for 'CodeActionRequestPriority.Low' and
            // this analyzer was de-prioritized to low priority bucket.
            if (Priority == CodeActionRequestPriority.Low &&
                IsDeprioritizedAnalyzerWithLowPriority(analyzer))
            {
                return true;
            }

            // Now compute this analyzer's priority and compare it with the provider's request 'Priority'.
            // Our internal 'IBuiltInAnalyzer' can specify custom request priority, while all
            // the third-party analyzers are assigned 'Normal' priority.
            var analyzerPriority = analyzer is IBuiltInAnalyzer { RequestPriority: var requestPriority }
                ? requestPriority
                : CodeActionRequestPriority.Normal;

            return Priority == analyzerPriority;
        }

        /// <summary>
        /// Returns true if the given <paramref name="codeFixProvider"/> should be considered
        /// a candidate when computing fixes for the given <see cref="Priority"/>.
        /// </summary>
        public bool MatchesPriority(
            CodeFixProvider codeFixProvider)
            => Priority switch
            {
                // We are computing fixes for all priorities
                CodeActionRequestPriority.None => true,

                // 'Low' priority can be used for two types of code fixers:
                //  1. Those which explicitly set their 'RequestPriority' to 'Low' and
                //  2. Those which can fix diagnostics for expensive analyzers which were de-prioritized
                //     to 'Low' priority bucket to improve lightbulb population performance.
                // Hence, when processing the 'Low' Priority bucket, we accept fixers with any RequestPriority,
                // as long as they can fix a diagnostic from an analyzer that was executed in the 'Low' bucket.
                CodeActionRequestPriority.Low => true,

                _ => Priority == codeFixProvider.RequestPriority,
            };
    }
}
