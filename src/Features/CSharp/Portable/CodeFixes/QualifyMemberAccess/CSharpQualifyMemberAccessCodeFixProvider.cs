// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Qualify;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.QualifyMemberAccess
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.QualifyMemberAccess), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal class CSharpQualifyMemberAccessCodeFixProvider : AbstractQualifyMemberAccessCodeFixprovider<SimpleNameSyntax>
    {
    }
}
