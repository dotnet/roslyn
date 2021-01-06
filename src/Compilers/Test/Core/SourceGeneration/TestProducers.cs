// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities.TestGenerators
{
    internal class SingleFileArtifactProducer : ArtifactProducer
    {
        private readonly string _content;
        private readonly string _hintName;

        public SingleFileArtifactProducer(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            this.WriteArtifact(this._hintName, SourceText.From(_content, Encoding.UTF8));
        }
    }

    internal class SingleFileArtifactProducer2 : SingleFileArtifactProducer
    {
        public SingleFileArtifactProducer2(string content, string hintName = "generatedFile") : base(content, hintName)
        {
        }
    }

    internal class CallbackArtifactProducer : ArtifactProducer
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

        public override void Initialize(AnalysisContext context)
        {
            _onInit(context);
            context.RegisterCompilationAction(Execute);
        }

        private void Execute(CompilationAnalysisContext context)
        {
            _onExecute(context);
            if (!string.IsNullOrWhiteSpace(_source))
            {
                this.WriteArtifact("source", SourceText.From(_source, Encoding.UTF8));
            }
        }
    }

    internal class CallbackArtifactProducer2 : CallbackArtifactProducer
    {
        public CallbackArtifactProducer2(Action<AnalysisContext> onInit, Action<CompilationAnalysisContext> onExecute, string? source = "") : base(onInit, onExecute, source)
        {
        }
    }
}
