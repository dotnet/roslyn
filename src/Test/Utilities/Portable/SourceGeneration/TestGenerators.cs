// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities.TestGenerators
{
    internal class SingleFileTestGenerator : ISourceGenerator
    {
        private readonly string _content;
        private readonly string _hintName;

        public SingleFileTestGenerator(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(this._hintName, SourceText.From(_content, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }

    internal class SingleFileTestGenerator2 : SingleFileTestGenerator
    {
        public SingleFileTestGenerator2(string content, string hintName = "generatedFile") : base(content, hintName)
        {
        }
    }

    internal class CallbackGenerator : ISourceGenerator
    {
        private readonly Action<GeneratorInitializationContext> _onInit;
        private readonly Action<GeneratorExecutionContext> _onExecute;
        private readonly string? _source;

        public CallbackGenerator(Action<GeneratorInitializationContext> onInit, Action<GeneratorExecutionContext> onExecute, string? source = "")
        {
            _onInit = onInit;
            _onExecute = onExecute;
            _source = source;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            _onExecute(context);
            if (!string.IsNullOrWhiteSpace(_source))
            {
                context.AddSource("source.cs", SourceText.From(_source, Encoding.UTF8));
            }
        }
        public void Initialize(GeneratorInitializationContext context) => _onInit(context);
    }

    internal class CallbackGenerator2 : CallbackGenerator
    {
        public CallbackGenerator2(Action<GeneratorInitializationContext> onInit, Action<GeneratorExecutionContext> onExecute, string? source = "") : base(onInit, onExecute, source)
        {
        }
    }

    internal class AdditionalFileAddedGenerator : ISourceGenerator
    {
        public bool CanApplyChanges { get; set; } = true;

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var file in context.AdditionalFiles)
            {
                context.AddSource(GetGeneratedFileName(file.Path), SourceText.From("", Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForAdditionalFileChanges(UpdateContext);
        }

        bool UpdateContext(GeneratorEditContext context, AdditionalFileEdit edit)
        {
            if (edit is AdditionalFileAddedEdit add && CanApplyChanges)
            {
                context.AdditionalSources.Add(GetGeneratedFileName(add.AddedText.Path), SourceText.From("", Encoding.UTF8));
                return true;
            }
            return false;
        }

        private string GetGeneratedFileName(string path) => $"{Path.GetFileNameWithoutExtension(path.Replace('\\', Path.DirectorySeparatorChar))}.generated";
    }

    internal class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _content;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _content = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _content;

    }
}
