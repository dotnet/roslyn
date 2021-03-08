// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Roslyn.Test.Utilities.TestGenerators
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), ArtifactProducer]
    internal class SingleFileArtifactProducer : DiagnosticAnalyzer
    {
        private readonly string _content;
        private readonly string _hintName;

        public SingleFileArtifactProducer(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public override void Initialize(AnalysisContext context)
        {
            Assert.True(context.TryGetArtifactContext(out var artifactContext));
            context.RegisterCompilationAction(c => AnalyzeCompilation(c, artifactContext));
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context, ArtifactContext artifactContext)
        {
            artifactContext.WriteArtifact(this._hintName, SourceText.From(_content, Encoding.UTF8));
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DiagnosticAnalyzerWithoutArtifactProducerAttribute : DiagnosticAnalyzer
    {
        private readonly string _content;
        private readonly string _hintName;

        public DiagnosticAnalyzerWithoutArtifactProducerAttribute(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(new DiagnosticDescriptor("TEST0000", "Title", "Message", "Category", DiagnosticSeverity.Error, isEnabledByDefault: true));

        public override void Initialize(AnalysisContext context)
        {
            Assert.True(context.TryGetArtifactContext(out var artifactContext));
            context.RegisterCompilationAction(c => AnalyzeCompilation(c, artifactContext));
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context, ArtifactContext artifactContext)
        {
            artifactContext.WriteArtifact(this._hintName, SourceText.From(_content, Encoding.UTF8));
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp), ArtifactProducer]
    internal class DiagnosticAnalyzerWithoutCommandLineArgGetsNoContext : DiagnosticAnalyzer
    {
        private readonly string _content;
        private readonly string _hintName;

        public DiagnosticAnalyzerWithoutCommandLineArgGetsNoContext(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(new DiagnosticDescriptor("TEST0000", "Title", "Message", "Category", DiagnosticSeverity.Error, isEnabledByDefault: true));

        public override void Initialize(AnalysisContext context)
        {
            Assert.False(context.TryGetArtifactContext(out var artifactContext));
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp), ArtifactProducer]
    internal class DiagnosticAnalyzerWithoutCommandLineArgThrowsWhenUsingContext : DiagnosticAnalyzer
    {
        private readonly string _content;
        private readonly string _hintName;

        public DiagnosticAnalyzerWithoutCommandLineArgThrowsWhenUsingContext(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(new DiagnosticDescriptor("TEST0000", "Title", "Message", "Category", DiagnosticSeverity.Error, isEnabledByDefault: true));

        public override void Initialize(AnalysisContext context)
        {
            Assert.False(context.TryGetArtifactContext(out var artifactContext));
            artifactContext.WriteArtifact(this._hintName, SourceText.From(_content, Encoding.UTF8));
        }
    }

    internal class SingleFileArtifactProducer2 : SingleFileArtifactProducer
    {
        public SingleFileArtifactProducer2(string content, string hintName = "generatedFile") : base(content, hintName)
        {
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp), ArtifactProducer]
    internal class CallbackArtifactProducer : DiagnosticAnalyzer
    {
        private readonly Action<AnalysisContext> _onInit;
        private readonly Action<CompilationAnalysisContext> _onExecute;
        private readonly string? _source;

        public CallbackArtifactProducer(Action<AnalysisContext> onInit, Action<CompilationAnalysisContext> onExecute, string? source = "")
        {
            _onInit = onInit;
            _onExecute = onExecute;
            _source = source;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public override void Initialize(AnalysisContext context)
        {
            _onInit(context);

            Assert.True(context.TryGetArtifactContext(out var artifactContext));
            context.RegisterCompilationAction(c => Execute(c, artifactContext));
        }

        private void Execute(CompilationAnalysisContext context, ArtifactContext artifactContext)
        {
            _onExecute(context);
            if (!string.IsNullOrWhiteSpace(_source))
            {
                artifactContext.WriteArtifact("source", SourceText.From(_source, Encoding.UTF8));
            }
        }
    }

    internal class CallbackArtifactProducer2 : CallbackArtifactProducer
    {
        public CallbackArtifactProducer2(Action<AnalysisContext> onInit, Action<CompilationAnalysisContext> onExecute, string? source = "") : base(onInit, onExecute, source)
        {
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp), ArtifactProducer]
    internal class DoNotCloseStreamArtifactProducer : DiagnosticAnalyzer
    {
        private readonly string _content;
        private readonly string _hintName;

        public DoNotCloseStreamArtifactProducer(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public override void Initialize(AnalysisContext context)
        {
            Assert.True(context.TryGetArtifactContext(out var artifactContext));
            context.RegisterCompilationAction(c => AnalyzeCompilation(c, artifactContext));
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context, ArtifactContext artifactContext)
        {
            var stream = artifactContext.CreateArtifactStream(_hintName);
            var bytes = Encoding.UTF8.GetBytes(_content);
            stream.Write(bytes, 0, bytes.Length);

            // purposefully do not close the stream.
        }
    }
}
