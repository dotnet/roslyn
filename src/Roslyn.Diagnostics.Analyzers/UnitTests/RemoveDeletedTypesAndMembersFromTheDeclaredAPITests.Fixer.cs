// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class RemoveDeletedTypesAndMembersFromTheDeclaredAPIFixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicRemoveDeletedTypesAndMembersFromTheDeclaredAPIAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpRemoveDeletedTypesAndMembersFromTheDeclaredAPIAnalyzer();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new BasicRemoveDeletedTypesAndMembersFromTheDeclaredAPIFixer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CSharpRemoveDeletedTypesAndMembersFromTheDeclaredAPIFixer();
        }
    }
}