﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.ChangeSignature;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
{
    internal sealed class ChangeSignatureTestState : IDisposable
    {
        private TestHostDocument _testDocument;
        public TestWorkspace Workspace { get; }
        public Document InvocationDocument { get; }
        public AbstractChangeSignatureService ChangeSignatureService { get; }
        public string ErrorMessage { get; private set; }
        public NotificationSeverity ErrorSeverity { get; private set; }

        public static ChangeSignatureTestState Create(string markup, string languageName, ParseOptions parseOptions = null)
        {
            var workspace = languageName == LanguageNames.CSharp
                  ? TestWorkspace.CreateCSharp(markup, exportProvider: s_exportProvider, parseOptions: (CSharpParseOptions)parseOptions)
                  : TestWorkspace.CreateVisualBasic(markup, exportProvider: s_exportProvider, parseOptions: parseOptions, compilationOptions: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return new ChangeSignatureTestState(workspace);
        }

        public static ChangeSignatureTestState Create(XElement workspaceXml)
        {
            var workspace = TestWorkspace.Create(workspaceXml);
            return new ChangeSignatureTestState(workspace);
        }

        public ChangeSignatureTestState(TestWorkspace workspace)
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
                return (TestChangeSignatureOptionsService)InvocationDocument.Project.Solution.Workspace.Services.GetService<IChangeSignatureOptionsService>();
            }
        }

        public ChangeSignatureResult ChangeSignature()
        {
            Roslyn.Test.Utilities.WpfTestCase.RequireWpfFact($"{nameof(AbstractChangeSignatureService.ChangeSignature)} currently needs to run on a WPF Fact because it's factored in a way that tries popping up UI in some cases.");

            return ChangeSignatureService.ChangeSignature(
                InvocationDocument,
                _testDocument.CursorPosition.Value,
                (errorMessage, severity) =>
                {
                    this.ErrorMessage = errorMessage;
                    this.ErrorSeverity = severity;
                },
                CancellationToken.None);
        }

        private static readonly ExportProvider s_exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic
                    .WithPart(typeof(TestChangeSignatureOptionsService))
                    .WithPart(typeof(CSharpChangeSignatureService))
                    .WithPart(typeof(VisualBasicChangeSignatureService)));

        public void Dispose()
        {
            if (Workspace != null)
            {
                Workspace.Dispose();
            }
        }
    }
}
