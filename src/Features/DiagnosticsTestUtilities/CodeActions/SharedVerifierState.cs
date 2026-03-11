// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

internal sealed class SharedVerifierState
{
    private readonly AnalyzerTest<DefaultVerifier> _test;
    private readonly string _defaultFileExt;

    /// <summary>
    /// The index in <see cref="Testing.ProjectState.AnalyzerConfigFiles"/> of the generated
    /// <strong>.editorconfig</strong> file for <see cref="Options"/>, or <see langword="null"/> if no such
    /// file has been generated yet.
    /// </summary>
    private int? _analyzerConfigIndex;

    /// <summary>
    /// The index in <see cref="Testing.ProjectState.AnalyzerConfigFiles"/> of the additional
    /// regular editorconfig added when <see cref="EditorConfig"/> is a global config, or
    /// <see langword="null"/> if not applicable.
    /// </summary>
    private int? _regularEditorConfigIndex;

    /// <summary>
    /// The index in <see cref="AnalyzerTest{TVerifier}.SolutionTransforms"/> of the options transformation for
    /// remaining <see cref="Options"/>, or <see langword="null"/> if no such transfor has been generated yet.
    /// </summary>
    private Func<Solution, ProjectId, Solution>? _remainingOptionsSolutionTransform;

    public SharedVerifierState(AnalyzerTest<DefaultVerifier> test, string defaultFileExt)
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

    internal void Apply()
    {
        var analyzerConfigSource = CodeFixVerifierHelper.ConvertOptionsToAnalyzerConfig(_defaultFileExt, EditorConfig, Options);
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

        // When EditorConfig is a global config (is_global=true), the end_of_line=crlf setting
        // appended under [*.ext] won't be effective because global configs don't use file globs.
        // Add a separate regular editorconfig to ensure end_of_line is properly applied.
        if (EditorConfig?.TrimStart().StartsWith("is_global", StringComparison.OrdinalIgnoreCase) == true)
        {
            var regularConfig = SourceText.From(
                $"root = true\r\n\r\n[*.{_defaultFileExt}]\r\nend_of_line = crlf\r\n", Encoding.UTF8);
            if (_regularEditorConfigIndex is null)
            {
                _regularEditorConfigIndex = _test.TestState.AnalyzerConfigFiles.Count;
                _test.TestState.AnalyzerConfigFiles.Add(("/src/.editorconfig", regularConfig));
            }
            else
            {
                _test.TestState.AnalyzerConfigFiles[_regularEditorConfigIndex.Value] = ("/src/.editorconfig", regularConfig);
            }
        }
        else if (_regularEditorConfigIndex is { } regIndex)
        {
            _regularEditorConfigIndex = null;
            _test.TestState.AnalyzerConfigFiles.RemoveAt(regIndex);
        }

        var solutionTransformIndex = _remainingOptionsSolutionTransform is not null ? _test.SolutionTransforms.IndexOf(_remainingOptionsSolutionTransform) : -1;
        if (_remainingOptionsSolutionTransform is not null)
        {
            _test.SolutionTransforms.Remove(_remainingOptionsSolutionTransform);
            _remainingOptionsSolutionTransform = null;
        }
    }
}
