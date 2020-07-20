// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FileHeaders;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpFileHeaderDiagnosticAnalyzer : AbstractFileHeaderDiagnosticAnalyzer
    {
        public CSharpFileHeaderDiagnosticAnalyzer()
            : base(LanguageNames.CSharp)
        {
        }

        protected override AbstractFileHeaderHelper FileHeaderHelper => CSharpFileHeaderHelper.Instance;
    }
}
