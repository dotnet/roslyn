// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports
{
    [ExportLanguageService(typeof(IRemoveUnnecessaryImportsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpRemoveUnnecessaryImportsService : AbstractCSharpRemoveUnnecessaryImportsService
    {
        [ImportingConstructor]
        public CSharpRemoveUnnecessaryImportsService()
        {
        }
    }
}
