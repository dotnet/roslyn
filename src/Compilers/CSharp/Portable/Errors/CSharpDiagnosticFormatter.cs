// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    public class CSharpDiagnosticFormatter : DiagnosticFormatter
    {
        internal CSharpDiagnosticFormatter()
        {
        }

        public static new CSharpDiagnosticFormatter Instance { get; } = new CSharpDiagnosticFormatter();

        internal override bool HasDefaultHelpLinkUri(Diagnostic diagnostic)
        {
            return diagnostic.Descriptor.HelpLinkUri == ErrorFacts.GetHelpLink((ErrorCode)diagnostic.Code);
        }
    }
}
