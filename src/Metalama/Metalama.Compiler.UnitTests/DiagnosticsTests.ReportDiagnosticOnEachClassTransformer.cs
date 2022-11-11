// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Compiler.UnitTests;

public partial class DiagnosticsTests
{
    private class ReportDiagnosticOnEachClassTransformer : ISourceTransformer
    {
        private readonly StringWriter? _outWriter;

        private readonly DiagnosticDescriptor _diagnostic;


        public ReportDiagnosticOnEachClassTransformer(string id, DiagnosticSeverity severity,
            StringWriter? outWriter = null)
        {
            _outWriter = outWriter;
            _diagnostic = new DiagnosticDescriptor(id, "", "Found a class '{0}'.", "test", severity, true);
        }


        public void Execute(TransformerContext context)
        {
            foreach ( var tree in context.Compilation.SyntaxTrees )
            {
                foreach (var c in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    _outWriter?.WriteLine($"Analyzing '{c.Identifier.Text}'.");
                    context.ReportDiagnostic(
                        Microsoft.CodeAnalysis.Diagnostic.Create(_diagnostic, c.Location, c.Identifier.Text));
                }
            }
        }
    }
}
