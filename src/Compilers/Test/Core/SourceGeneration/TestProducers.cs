// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities.TestGenerators
{
    internal class SingleFileArtifactProducer : IArtifactProducer
    {
        private readonly string _content;
        private readonly string _hintName;

        public SingleFileArtifactProducer(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public void Initialize(AnalysisContext context, ArtifactContext artifactContext)
        {
            context.RegisterCompilationAction(c => AnalyzeCompilation(c, artifactContext));
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context, ArtifactContext artifactContext)
        {
            artifactContext.WriteArtifact(this._hintName, SourceText.From(_content, Encoding.UTF8));
        }
    }

    internal class SingleFileArtifactProducer2 : SingleFileArtifactProducer
    {
        public SingleFileArtifactProducer2(string content, string hintName = "generatedFile") : base(content, hintName)
        {
        }
    }

    internal class CallbackArtifactProducer : IArtifactProducer
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

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public void Initialize(AnalysisContext context, ArtifactContext artifactContext)
        {
            _onInit(context);
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

    internal class DoNotCloseStreamArtifactProducer : IArtifactProducer
    {
        private readonly string _content;
        private readonly string _hintName;

        public DoNotCloseStreamArtifactProducer(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public void Initialize(AnalysisContext context, ArtifactContext artifactContext)
        {
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
