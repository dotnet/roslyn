// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// this contains all states regarding a <see cref="DiagnosticAnalyzer"/>
        /// 
        /// use <see cref="StateSet.GetState(StateType)"/> to retrieve specific <see cref="StateType"/> of <see cref="DiagnosticState"/>
        /// </summary>
        private class StateSet
        {
            private const string UserDiagnosticsPrefixTableName = "<UserDiagnostics>";

            private readonly string _language;
            private readonly DiagnosticAnalyzer _analyzer;
            private readonly string _errorSourceName;

            private readonly DiagnosticState[] _state;

            public StateSet(string language, DiagnosticAnalyzer analyzer, string errorSourceName)
            {
                _language = language;
                _analyzer = analyzer;
                _errorSourceName = errorSourceName;

                _state = CreateDiagnosticStates(language, analyzer);
            }

            public string ErrorSourceName => _errorSourceName;
            public string Language => _language;
            public DiagnosticAnalyzer Analyzer => _analyzer;

            public DiagnosticState GetState(StateType stateType)
            {
                return _state[(int)stateType];
            }

            public void Remove(object documentOrProjectId, bool onlyDocumentStates = false)
            {
                // this is to clear up states. some caller such as re-analyzing a file wants to
                // reset only document related states, some like removing a file wants to clear up
                // states all together.
                for (var stateType = 0; stateType < s_stateTypeCount; stateType++)
                {
                    if (onlyDocumentStates && stateType == (int)StateType.Project)
                    {
                        continue;
                    }

                    _state[stateType].Remove(documentOrProjectId);
                }
            }

            private static DiagnosticState[] CreateDiagnosticStates(string language, DiagnosticAnalyzer analyzer)
            {
                var states = new DiagnosticState[s_stateTypeCount];

                for (int stateType = 0; stateType < s_stateTypeCount; stateType++)
                {
                    var nameAndVersion = GetNameAndVersion(analyzer, (StateType)stateType);

                    var name = nameAndVersion.Item1;
                    var version = nameAndVersion.Item2;

                    states[stateType] = new DiagnosticState(name, version, language);
                }

                return states;
            }

            /// <summary>
            /// Get the unique state name for the given {type, analyzer} tuple.
            /// Note that this name is used by the underlying persistence stream of the corresponding <see cref="DiagnosticState"/> to Read/Write diagnostic data into the stream.
            /// If any two distinct {type, analyzer} tuples have the same diagnostic state name, we will end up sharing the persistence stream between them, leading to duplicate/missing/incorrect diagnostic data.
            /// </summary>
            private static ValueTuple<string, VersionStamp> GetNameAndVersion(DiagnosticAnalyzer analyzer, StateType type)
            {
                Contract.ThrowIfNull(analyzer);

                // Get the unique ID for given diagnostic analyzer.
                // note that we also put version stamp so that we can detect changed analyzer.
                var tuple = analyzer.GetAnalyzerIdAndVersion();
                return ValueTuple.Create(UserDiagnosticsPrefixTableName + "_" + type.ToString() + "_" + tuple.Item1, tuple.Item2);
            }
        }
    }
}
