// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Manages instrumentation state.
/// </summary>
internal sealed class InstrumentationState
{
    /// <summary>
    /// Used to temporary suspend instrumentation, for example when lowering expression tree.
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// Current instrumenter.
    /// </summary>
    public Instrumenter Instrumenter { get; set; } = Instrumenter.NoOp;

    public void RemoveDynamicAnalysisInstrumentation()
        => Instrumenter = RemoveDynamicAnalysisInjectors(Instrumenter);

    public static Instrumenter RemoveDynamicAnalysisInjectors(Instrumenter instrumenter)
    {
        switch (instrumenter)
        {
            case DynamicAnalysisInjector { Previous: var previous }:
                return RemoveDynamicAnalysisInjectors(previous);

            case DebugInfoInjector { Previous: var previous } injector:
                var newPrevious = RemoveDynamicAnalysisInjectors(previous);
                if ((object)newPrevious == previous)
                {
                    return injector;
                }
                else if ((object)newPrevious == Instrumenter.NoOp)
                {
                    return DebugInfoInjector.Singleton;
                }
                else
                {
                    return new DebugInfoInjector(previous);
                }

            case CompoundInstrumenter compound:
                // If we hit this it means a new kind of compound instrumenter is in use.
                // Either add a new case or add an abstraction that lets us
                // filter out the unwanted injectors in a more generalized way.
                throw ExceptionUtilities.UnexpectedValue(compound);

            default:
                Debug.Assert((object)instrumenter == Instrumenter.NoOp);
                return instrumenter;
        }
    }
}
