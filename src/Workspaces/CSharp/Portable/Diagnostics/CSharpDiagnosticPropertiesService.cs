﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
