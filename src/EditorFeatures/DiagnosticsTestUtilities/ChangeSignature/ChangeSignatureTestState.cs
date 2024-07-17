// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
{
    internal sealed class ChangeSignatureTestState : IDisposable
    {
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestChangeSignatureOptionsService));

        private readonly TestHostDocument _testDocument;
        public EditorTestWorkspace Workspace { get; }
        public Document InvocationDocument { get; }
        public AbstractChangeSignatureService ChangeSignatureService { get; }

        public static ChangeSignatureTestState Create(string markup, string languageName, ParseOptions parseOptions = null, OptionsCollection options = null)
        {
            var workspace = languageName switch
            {
                "XML" => EditorTestWorkspace.Create(markup, composition: s_composition),
                LanguageNames.CSharp => EditorTestWorkspace.CreateCSharp(markup, composition: s_composition, parseOptions: (CSharpParseOptions)parseOptions),
                LanguageNames.VisualBasic => EditorTestWorkspace.CreateVisualBasic(markup, composition: s_composition, parseOptions: parseOptions, compilationOptions: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)),
                _ => throw new ArgumentException("Invalid language name.")
            };

            workspace.SetAnalyzerFallbackAndGlobalOptions(options);
            return new ChangeSignatureTestState(workspace);
        }

        public static ChangeSignatureTestState Create(XElement workspaceXml)
        {
            var workspace = EditorTestWorkspace.Create(workspaceXml, composition: s_composition);
            return new ChangeSignatureTestState(workspace);
        }

        public ChangeSignatureTestState(EditorTestWorkspace workspace)
        {
            Workspace = workspace;
            _testDocument = Workspace.Documents.SingleOrDefault(d => d.CursorPosition.HasValue);

            if (_testDocument == null)
            {
                throw new ArgumentException("markup does not contain a cursor position", nameof(workspace));
            }

            InvocationDocument = Workspace.CurrentSolution.GetDocument(_testDocument.Id);
            ChangeSignatureService = InvocationDocument.GetLanguageService<AbstractChangeSignatureService>();
        }

        public TestChangeSignatureOptionsService TestChangeSignatureOptionsService
        {
            get
            {
                return (TestChangeSignatureOptionsService)InvocationDocument.Project.Solution.Services.GetRequiredService<IChangeSignatureOptionsService>();
            }
        }

        public async Task<ChangeSignatureResult> ChangeSignatureAsync()
        {
            var context = await ChangeSignatureService.GetChangeSignatureContextAsync(InvocationDocument, _testDocument.CursorPosition.Value, restrictToDeclarations: false, CancellationToken.None).ConfigureAwait(false);
            var options = AbstractChangeSignatureService.GetChangeSignatureOptions(context);
            return await ChangeSignatureService.ChangeSignatureWithContextAsync(context, options, CancellationToken.None);
        }

        public async Task<ParameterConfiguration> GetParameterConfigurationAsync()
        {
            var context = await ChangeSignatureService.GetChangeSignatureContextAsync(InvocationDocument, _testDocument.CursorPosition.Value, restrictToDeclarations: false, CancellationToken.None);
            if (context is ChangeSignatureAnalysisSucceededContext changeSignatureAnalyzedSucceedContext)
            {
                return changeSignatureAnalyzedSucceedContext.ParameterConfiguration;
            }

            throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(((CannotChangeSignatureAnalyzedContext)context).CannotChangeSignatureReason.ToString());
        }

        public void Dispose()
        {
            Workspace?.Dispose();
        }
    }
}
