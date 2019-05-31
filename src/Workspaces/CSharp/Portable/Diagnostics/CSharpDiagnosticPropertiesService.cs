// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics
{
    [ExportLanguageService(typeof(IDiagnosticPropertiesService), LanguageNames.CSharp), Shared]
    internal class CSharpDiagnosticPropertiesService : AbstractDiagnosticPropertiesService
    {
        private static readonly Compilation s_compilation = CSharpCompilation.Create("empty");

        [ImportingConstructor]
        public CSharpDiagnosticPropertiesService()
        {
        }

        protected override Compilation GetCompilation() => s_compilation;
    }
}
