// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer,
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpPartsExportedWithMEFv2MustBeMarkedAsSharedFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer,
    Roslyn.Diagnostics.VisualBasic.Analyzers.BasicPartsExportedWithMEFv2MustBeMarkedAsSharedFixer>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class PartsExportedWithMEFv2MustBeMarkedAsSharedTests
    {
        private const string CSharpWellKnownAttributesDefinition = """

            namespace System.Composition
            {
                public class ExportAttribute : System.Attribute
                {
                    public ExportAttribute(System.Type contractType){ }
                }

                public class SharedAttribute : System.Attribute
                {
                }
            }

            """;
        private const string BasicWellKnownAttributesDefinition = """

            Namespace System.Composition
            	Public Class ExportAttribute
            		Inherits System.Attribute
            		Public Sub New(contractType As System.Type)
            		End Sub
            	End Class

            	Public Class SharedAttribute
            		Inherits System.Attribute
            	End Class
            End Namespace


            """;

        #region No Diagnostic Tests

        [Fact]
        public async Task NoDiagnosticCases_ResolvedTypesAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                using System;
                using System.Composition;

                [Export(typeof(C)), Shared]
                public class C
                {
                }

                """ + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyAnalyzerAsync("""

                Imports System
                Imports System.Composition

                <Export(GetType(C)), [Shared]> _
                Public Class C
                End Class

                """ + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public async Task NoDiagnosticCases_UnresolvedTypesAsync()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """

                        using System;
                        using System.{|CS0234:Composition|};

                        [{|CS0246:{|CS0246:Export|}|}(typeof(C)), {|CS0246:{|CS0246:Shared|}|}]
                        public class C
                        {
                        }

                        """,
                    },
                },
                ReferenceAssemblies = ReferenceAssemblies.Default,
            }.RunAsync();

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """

                        Imports System
                        Imports System.Composition

                        <{|BC30002:Export|}(GetType(C)), {|BC30002:[Shared]|}> _
                        Public Class C
                        End Class

                        """
                    },
                },
                ReferenceAssemblies = ReferenceAssemblies.Default,
            }.RunAsync();
        }

        #endregion

        #region Diagnostic Tests

        [Fact]
        public async Task DiagnosticCases_NoSharedAttributeAsync()
        {
            await VerifyCS.VerifyCodeFixAsync("""

                using System;
                using System.Composition;

                [[|Export(typeof(C))|]]
                public class C
                {
                }

                """ + CSharpWellKnownAttributesDefinition, """

                using System;
                using System.Composition;

                [Export(typeof(C))]
                [Shared]
                public class C
                {
                }

                """ + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyCodeFixAsync("""

                Imports System
                Imports System.Composition

                <[|Export(GetType(C))|]> _
                Public Class C
                End Class

                """ + BasicWellKnownAttributesDefinition, """

                Imports System
                Imports System.Composition

                <Export(GetType(C))> _
                <[Shared]>
                Public Class C
                End Class

                """ + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public async Task DiagnosticCases_DifferentSharedAttributeAsync()
        {
            await VerifyCS.VerifyCodeFixAsync("""

                using System;

                [[|System.Composition.Export(typeof(C))|], Shared]
                public class C
                {
                }

                public class SharedAttribute: Attribute
                {
                }

                """ + CSharpWellKnownAttributesDefinition, """

                using System;

                [System.Composition.Export(typeof(C)), Shared]
                [System.Composition.Shared]
                public class C
                {
                }

                public class SharedAttribute: Attribute
                {
                }

                """ + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyCodeFixAsync("""

                Imports System

                <[|System.Composition.Export(GetType(C))|], [Shared]> _
                Public Class C
                End Class

                Public Class SharedAttribute
                    Inherits Attribute
                End Class

                """ + BasicWellKnownAttributesDefinition, """

                Imports System

                <System.Composition.Export(GetType(C)), [Shared]> _
                <Composition.Shared>
                Public Class C
                End Class

                Public Class SharedAttribute
                    Inherits Attribute
                End Class

                """ + BasicWellKnownAttributesDefinition);
        }

        #endregion
    }
}
