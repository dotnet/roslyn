// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
{
    public abstract class AbstractSquiggleProducerTests
    {
        protected static IEnumerable<ITagSpan<IErrorTag>> GetErrorSpans(TestWorkspace workspace)
        {
            var registrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            var diagnosticWaiter = new DiagnosticServiceWaiter();
            var diagnosticListeners = SpecializedCollections.SingletonEnumerable(new Lazy<IAsynchronousOperationListener, FeatureMetadata>(
                () => diagnosticWaiter, new FeatureMetadata(new Dictionary<string, object>() { { "FeatureName", FeatureAttribute.DiagnosticService } })));

            var optionsService = workspace.Services.GetService<IOptionService>();
            var compilerAnalyzersMap = DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap();
            var analyzerService = new DiagnosticAnalyzerService(compilerAnalyzersMap);
            var diagnosticService = new DiagnosticService(SpecializedCollections.SingletonEnumerable<IDiagnosticUpdateSource>(analyzerService), diagnosticListeners);

            var document = workspace.Documents.First();
            var buffer = document.GetTextBuffer();

            var squiggleWaiter = new ErrorSquiggleWaiter();
            var foregroundService = new TestForegroundNotificationService();
            var taggerSource = new DiagnosticsSquiggleTaggerProvider.TagSource(buffer, foregroundService, diagnosticService, optionsService, squiggleWaiter);

            var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>() as SolutionCrawlerRegistrationService;
            service.WaitUntilCompletion_ForTestingPurposesOnly(workspace, ImmutableArray.Create(analyzerService.CreateIncrementalAnalyzer(workspace)));

            diagnosticWaiter.CreateWaitTask().PumpingWait();
            squiggleWaiter.CreateWaitTask().PumpingWait();

            var snapshot = buffer.CurrentSnapshot;
            var intervalTree = taggerSource.GetTagIntervalTreeForBuffer(buffer);
            var spans = intervalTree.GetIntersectingSpans(new SnapshotSpan(snapshot, 0, snapshot.Length)).ToImmutableArray();

            taggerSource.TestOnly_Dispose();

            registrationService.Unregister(workspace);

            return spans;
        }

        private class DiagnosticServiceWaiter : AsynchronousOperationListener { }
        private class ErrorSquiggleWaiter : AsynchronousOperationListener { }
    }
}
