// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Diagnostics;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    internal sealed class SharedVerifierState
    {
        private readonly AnalyzerTest<XUnitVerifier> _test;
        private readonly string _defaultFileExt;

        /// <summary>
        /// The index in <see cref="Testing.ProjectState.AnalyzerConfigFiles"/> of the generated
        /// <strong>.editorconfig</strong> file for <see cref="Options"/>, or <see langword="null"/> if no such
        /// file has been generated yet.
        /// </summary>
        private int? _analyzerConfigIndex;

        /// <summary>
        /// The index in <see cref="AnalyzerTest{TVerifier}.SolutionTransforms"/> of the options transformation for
        /// remaining <see cref="Options"/>, or <see langword="null"/> if no such transfor has been generated yet.
        /// </summary>
        private Func<Solution, ProjectId, Solution>? _remainingOptionsSolutionTransform;

        public SharedVerifierState(AnalyzerTest<XUnitVerifier> test, string defaultFileExt)
        {
            _test = test;
            _defaultFileExt = defaultFileExt;
            Options = new OptionsCollection(test.Language);
        }

        public string? EditorConfig { get; set; }

        /// <summary>
        /// Gets a collection of options to apply to <see cref="Solution.Options"/> for testing. Values may be added
        /// using a collection initializer.
        /// </summary>
        internal OptionsCollection Options { get; }

#if !CODE_STYLE
        internal CodeActionOptions CodeActionOptions { get; set; } = CodeActionOptions.Default;
        internal IdeAnalyzerOptions IdeAnalyzerOptions { get; set; } = IdeAnalyzerOptions.Default;
#endif
        internal void Apply()
        {
            var (analyzerConfigSource, remainingOptions) = CodeFixVerifierHelper.ConvertOptionsToAnalyzerConfig(_defaultFileExt, EditorConfig, Options);
            if (analyzerConfigSource is not null)
            {
                if (_analyzerConfigIndex is null)
                {
                    _analyzerConfigIndex = _test.TestState.AnalyzerConfigFiles.Count;
                    _test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", analyzerConfigSource));
                }
                else
                {
                    _test.TestState.AnalyzerConfigFiles[_analyzerConfigIndex.Value] = ("/.editorconfig", analyzerConfigSource);
                }
            }
            else if (_analyzerConfigIndex is { } index)
            {
                _analyzerConfigIndex = null;
                _test.TestState.AnalyzerConfigFiles.RemoveAt(index);
            }

            var solutionTransformIndex = _remainingOptionsSolutionTransform is not null ? _test.SolutionTransforms.IndexOf(_remainingOptionsSolutionTransform) : -1;
            if (remainingOptions is not null)
            {
                // Generate a new solution transform
                _remainingOptionsSolutionTransform = (solution, projectId) =>
                {
#if !CODE_STYLE
                    var options = solution.Options;
                    foreach (var (key, value) in remainingOptions)
                    {
                        options = options.WithChangedOption(key, value);
                    }

                    solution = solution.WithOptions(options);
#endif

                    return solution;
                };

                if (solutionTransformIndex < 0)
                {
                    _test.SolutionTransforms.Add(_remainingOptionsSolutionTransform);
                }
                else
                {
                    _test.SolutionTransforms[solutionTransformIndex] = _remainingOptionsSolutionTransform;
                }
            }
            else if (_remainingOptionsSolutionTransform is not null)
            {
                _test.SolutionTransforms.Remove(_remainingOptionsSolutionTransform);
                _remainingOptionsSolutionTransform = null;
            }
        }
    }
}
