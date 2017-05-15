using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDefaultLiteral
{
    public class UseDefaultLiteralTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseThrowExpressionDiagnosticAnalyzer(), new UseThrowExpressionCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task WithoutBraces()
        {
    }
}
