// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing.TestGenerators
{
    public class AddEmptyFileWithDiagnostic : AddEmptyFile
    {
        public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            "SG0001",
            "Title",
            "Message",
            "Category",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override void Execute(GeneratorExecutionContext context)
        {
            base.Execute(context);

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                context.Compilation.SyntaxTrees.First().GetLocation(new TextSpan(0, 0))));
        }
    }
}
