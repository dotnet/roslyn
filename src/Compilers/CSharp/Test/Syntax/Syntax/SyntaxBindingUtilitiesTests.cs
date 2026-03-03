// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxBindingUtilitiesTests
    {
        [Fact]
        public void BindsToTryStatement_IncludesFixedAndStatementForms()
        {
            var fixedStatement = (FixedStatementSyntax)SyntaxFactory.ParseStatement("fixed (int* p = null) { }");
            Assert.True(SyntaxBindingUtilities.BindsToTryStatement(fixedStatement));

            var lockStatement = (LockStatementSyntax)SyntaxFactory.ParseStatement("lock (obj.Member) { }");
            Assert.True(SyntaxBindingUtilities.BindsToTryStatement(lockStatement));
            Assert.False(SyntaxBindingUtilities.BindsToTryStatement(lockStatement.Expression));

            var usingStatement = (UsingStatementSyntax)SyntaxFactory.ParseStatement("using (obj.Member) { }");
            Assert.True(SyntaxBindingUtilities.BindsToTryStatement(usingStatement));

            var usingDeclaration = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement("using var x = obj.Member;");
            var variableDeclarator = usingDeclaration.Declaration.Variables.Single();
            Assert.True(SyntaxBindingUtilities.BindsToTryStatement(variableDeclarator));
        }
    }
}
