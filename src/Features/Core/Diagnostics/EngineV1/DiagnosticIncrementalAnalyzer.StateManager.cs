// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler.State;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        internal class StateManager
        {
            private static readonly int s_stateTypeCount = Enum.GetNames(typeof(StateType)).Count();
            private static readonly ImmutableArray<StateType> s_documentScopeStateTypes = ImmutableArray.Create<StateType>(StateType.Syntax, StateType.Document);

            private readonly ConditionalWeakTable<DiagnosticAnalyzer, DiagnosticState[]> _stateMap;

            public StateManager()
            {
                _stateMap = new ConditionalWeakTable<DiagnosticAnalyzer, DiagnosticState[]>();
            }

            public DiagnosticState GetState(DiagnosticAnalyzer analyzer, StateType statetype)
            {
                return _stateMap.GetValue(analyzer, CreateAnalyzerStates)[(int)statetype];
            }

            private DiagnosticState[] CreateAnalyzerStates(DiagnosticAnalyzer unused)
            {
                return new DiagnosticState[s_stateTypeCount];
            }
        }
    }
}
