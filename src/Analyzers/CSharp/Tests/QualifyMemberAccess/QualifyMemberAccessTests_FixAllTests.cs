// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QualifyMemberAccess
{
    public partial class QualifyMemberAccessTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution_QualifyMemberAccess()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
                using System;

                class C
                {
                    int Property { get; set; }
                    int OtherProperty { get; set; }

                    void M()
                    {
                        {|FixAllInSolution:Property|} = 1;
                        var x = OtherProperty;
                    }
                }
                        </Document>
                        <Document>
                using System;

                class D
                {
                    string StringProperty { get; set; }
                    int field;

                    void N()
                    {
                        StringProperty = string.Empty;
                        field = 0; // ensure this doesn't get qualified
                    }
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

                class C
                {
                    int Property { get; set; }
                    int OtherProperty { get; set; }

                    void M()
                    {
                        this.Property = 1;
                        var x = this.OtherProperty;
                    }
                }
                        </Document>
                        <Document>
                using System;

                class D
                {
                    string StringProperty { get; set; }
                    int field;

                    void N()
                    {
                        this.StringProperty = string.Empty;
                        field = 0; // ensure this doesn't get qualified
                    }
                }
                        </Document>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(
                initialMarkup: input,
                expectedMarkup: expected,
                options: Option(CodeStyleOptions2.QualifyPropertyAccess, true, NotificationOption2.Suggestion));
        }
    }
}
