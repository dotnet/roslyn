// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression;

public partial class CSharpSuppressionTests : AbstractSuppressionDiagnosticTest_NoEditor
{
    #region "Fix selected occurrences tests"

    public abstract class CSharpFixMultipleSuppressionTests : CSharpSuppressionTests
    {
        protected override int CodeActionIndex => 0;

        internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
        }

        private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Decsciptor1 =
                new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic Title", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);
            public static readonly DiagnosticDescriptor Decsciptor2 =
                new DiagnosticDescriptor("InfoDiagnostic2", "InfoDiagnostic2 Title", "InfoDiagnostic2", "InfoDiagnostic2", DiagnosticSeverity.Info, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Decsciptor1, Decsciptor2);

            public override void Initialize(AnalysisContext context)
                => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var classDecl = (ClassDeclarationSyntax)context.Node;
                var location = classDecl.Identifier.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(Decsciptor1, location));
                context.ReportDiagnostic(Diagnostic.Create(Decsciptor2, location));
            }
        }

        #region "Pragma disable tests"

        public class CSharpFixMultiplePragmaWarningSuppressionTests : CSharpFixMultipleSuppressionTests
        {
            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            [WorkItem("https://github.com/dotnet/roslyn/issues/6455")]
            public async Task TestFixMultipleInDocument()
            {
                var input = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInSelection:class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }|}
                    class Class3 { }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """;

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    #pragma warning disable InfoDiagnostic2 // InfoDiagnostic2 Title
                    class Class1
                    #pragma warning restore InfoDiagnostic2 // InfoDiagnostic2 Title
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    #pragma warning disable InfoDiagnostic2 // InfoDiagnostic2 Title
                    class Class2
                    #pragma warning restore InfoDiagnostic2 // InfoDiagnostic2 Title
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                    }
                    class Class3 { }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """;

                await TestInRegularAndScriptAsync(input, expected);
            }
        }

        #endregion
    }

    #endregion
}
