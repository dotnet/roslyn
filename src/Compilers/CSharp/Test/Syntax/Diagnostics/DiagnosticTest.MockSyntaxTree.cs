// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using System.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticTest
    {
        internal class MockSyntaxTree : CSharpSyntaxTree
        {
            public override string FilePath
            {
                get
                {
                    return "";
                }
            }

            public override SourceText GetText(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override bool TryGetText(out SourceText text)
            {
                throw new NotImplementedException();
            }

            public override Encoding Encoding
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override int Length
            {
                get { throw new NotImplementedException(); }
            }

            public override CSharpParseOptions Options
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override bool TryGetRoot(out CSharpSyntaxNode root)
            {
                throw new NotImplementedException();
            }

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
            {
                throw new NotImplementedException();
            }

            public override SyntaxTree WithFilePath(string path)
            {
                throw new NotImplementedException();
            }

            public override SyntaxTree WithDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> options)
            {
                throw new NotImplementedException();
            }

            public override bool HasCompilationUnitRoot
            {
                get { throw new NotImplementedException(); }
            }

            public override ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions => throw new NotImplementedException();
        }
    }
}
