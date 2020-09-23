// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation;
using Microsoft.CodeAnalysis.CSharp.UseLocalFunction;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseImplicitObjectCreationTests
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseImplicitObjectCreationDiagnosticAnalyzer,
        CSharpUseImplicitObjectCreationCodeFixProvider>;

    public partial class UseImplicitObjectCreationTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestMissingBeforeCSharp9()
        {
        }
    }
}
