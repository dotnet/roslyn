// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpSyntaxTree
    {
        private class ParsedSyntaxTree : CSharpSyntaxTree
        {
            private readonly CSharpParseOptions _options;
            private readonly string _path;
            private readonly CSharpSyntaxNode _root;
            private readonly bool _hasCompilationUnitRoot;
            private readonly Encoding _encodingOpt;
            private readonly SourceHashAlgorithm _checksumAlgorithm;
            private readonly ImmutableDictionary<string, ReportDiagnostic> _diagnosticOptions;
            private SourceText _lazyText;

            internal ParsedSyntaxTree(
                SourceText textOpt,
                Encoding encodingOpt,
                SourceHashAlgorithm checksumAlgorithm,
                string path,
                CSharpParseOptions options,
                CSharpSyntaxNode root,
                Syntax.InternalSyntax.DirectiveStack directives,
                ImmutableDictionary<string, ReportDiagnostic> diagnosticOptions,
                bool? isGeneratedCode,
                bool cloneRoot)
            {
                Debug.Assert(root != null);
                Debug.Assert(options != null);
                Debug.Assert(textOpt == null || textOpt.Encoding == encodingOpt && textOpt.ChecksumAlgorithm == checksumAlgorithm);

                _lazyText = textOpt;
                _encodingOpt = encodingOpt ?? textOpt?.Encoding;
                _checksumAlgorithm = checksumAlgorithm;
                _options = options;
                _path = path ?? string.Empty;
                _root = cloneRoot ? this.CloneNodeAsRoot(root) : root;
                _hasCompilationUnitRoot = root.Kind() == SyntaxKind.CompilationUnit;
                _diagnosticOptions = diagnosticOptions ?? EmptyDiagnosticOptions;
                if (isGeneratedCode is bool b)
                {
                    _isGenerationConfigured = true;
                    _lazyIsGeneratedCode = b.ToThreeState();
                }

                this.SetDirectiveStack(directives);
            }

            public override string FilePath
            {
                get { return _path; }
            }

            public override SourceText GetText(CancellationToken cancellationToken)
            {
                if (_lazyText == null)
                {
                    Interlocked.CompareExchange(ref _lazyText, this.GetRoot(cancellationToken).GetText(_encodingOpt, _checksumAlgorithm), null);
                }

                return _lazyText;
            }

            public override bool TryGetText(out SourceText text)
            {
                text = _lazyText;
                return text != null;
            }

            public override Encoding Encoding
            {
                get { return _encodingOpt; }
            }

            public override int Length
            {
                get { return _root.FullSpan.Length; }
            }

            public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken)
            {
                return _root;
            }

            public override bool TryGetRoot(out CSharpSyntaxNode root)
            {
                root = _root;
                return true;
            }

            public override bool HasCompilationUnitRoot
            {
                get
                {
                    return _hasCompilationUnitRoot;
                }
            }

            public override CSharpParseOptions Options
            {
                get
                {
                    return _options;
                }
            }

            public override ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions => _diagnosticOptions;

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                return new SimpleSyntaxReference(node);
            }

            public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
            {
                if (ReferenceEquals(_root, root) && ReferenceEquals(_options, options))
                {
                    return this;
                }

                return new ParsedSyntaxTree(
                    null,
                    _encodingOpt,
                    _checksumAlgorithm,
                    _path,
                    (CSharpParseOptions)options,
                    (CSharpSyntaxNode)root,
                    _directives,
                    _diagnosticOptions,
                    isGeneratedCode: _isGenerationConfigured
                        ? (bool?)_lazyIsGeneratedCode.Value()
                        : null,
                    cloneRoot: true);
            }

            public override SyntaxTree WithFilePath(string path)
            {
                if (_path == path)
                {
                    return this;
                }

                return new ParsedSyntaxTree(
                    _lazyText,
                    _encodingOpt,
                    _checksumAlgorithm,
                    path,
                    _options,
                    _root,
                    _directives,
                    _diagnosticOptions,
                    isGeneratedCode: _isGenerationConfigured
                        ? (bool?)_lazyIsGeneratedCode.Value()
                        : null,
                    cloneRoot: true);
            }

            public override SyntaxTree WithDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> options)
            {
                if (options is null)
                {
                    options = EmptyDiagnosticOptions;
                }

                if (ReferenceEquals(_diagnosticOptions, options))
                {
                    return this;
                }

                return new ParsedSyntaxTree(
                    _lazyText,
                    _encodingOpt,
                    _checksumAlgorithm,
                    _path,
                    _options,
                    _root,
                    _directives,
                    options,
                    isGeneratedCode: _isGenerationConfigured
                        ? (bool?)_lazyIsGeneratedCode.Value()
                        : null,
                    cloneRoot: true);
            }
        }
    }
}
