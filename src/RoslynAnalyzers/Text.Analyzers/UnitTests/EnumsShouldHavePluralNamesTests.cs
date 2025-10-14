// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Text.Analyzers.EnumsShouldHavePluralNamesAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Text.Analyzers.EnumsShouldHavePluralNamesAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Text.Analyzers.UnitTests
{
    public class EnumsShouldHavePluralNamesTests
    {
        [Fact]
        public async Task CA1714_CA1717_Test_EnumWithNoFlags_SingularNameAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               public enum Day 
                                                {
                                                    Sunday = 0,
                                                    Monday = 1,
                                                    Tuesday = 2
                                                       
                                                };
                                            }
                """
                          );

            await VerifyVB.VerifyAnalyzerAsync("""

                                        Public Class A
                	                        Public Enum Day
                		                           Sunday = 0
                		                           Monday = 1
                		                           Tuesday = 2

                	                        End Enum
                                        End Class
                                        
                """);
        }

        [Fact]
        public async Task CA1714_CA1717__Test_EnumWithNoFlags_PluralNameAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               public enum Days 
                                                {
                                                    sunday = 0,
                                                    Monday = 1,
                                                    Tuesday = 2
                                                       
                                                };
                                            }
                """,
                            GetCSharpNoPluralResultAt(4, 44));

            await VerifyVB.VerifyAnalyzerAsync("""

                                        Public Class A
                	                        Public Enum Days
                		                           Sunday = 0
                		                           Monday = 1
                		                           Tuesday = 2

                	                        End Enum
                                        End Class
                                        
                """,
                        GetBasicNoPluralResultAt(3, 38));
        }

        [Fact, WorkItem(1432, "https://github.com/dotnet/roslyn-analyzers/issues/1432")]
        public async Task CA1714_CA1717__Test_EnumWithNoFlags_PluralName_InternalAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                class A 
                { 
                    enum Days 
                    {
                        sunday = 0,
                        Monday = 1,
                        Tuesday = 2
                                                       
                    };
                }

                public class A2
                { 
                    private enum Days 
                    {
                        sunday = 0,
                        Monday = 1,
                        Tuesday = 2
                                                       
                    };
                }

                internal class A3
                { 
                    public enum Days 
                    {
                        sunday = 0,
                        Monday = 1,
                        Tuesday = 2
                                                       
                    };
                }

                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                Class A
                	Private Enum Days
                		Sunday = 0
                		Monday = 1
                		Tuesday = 2
                	End Enum
                End Class

                Public Class A2
                	Private Enum Days
                		Sunday = 0
                		Monday = 1
                		Tuesday = 2
                	End Enum
                End Class

                Friend Class A3
                	Public Enum Days
                		Sunday = 0
                		Monday = 1
                		Tuesday = 2
                	End Enum
                End Class

                """);
        }

        [Fact]
        public async Task CA1714_CA1717__Test_EnumWithNoFlags_PluralName_UpperCaseAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               public enum DAYS 
                                                {
                                                    sunday = 0,
                                                    Monday = 1,
                                                    Tuesday = 2
                                                       
                                                };
                                            }
                """,
                            GetCSharpNoPluralResultAt(4, 44));

            await VerifyVB.VerifyAnalyzerAsync("""

                                        Public Class A
                	                        Public Enum DAYS
                		                           Sunday = 0
                		                           Monday = 1
                		                           Tuesday = 2

                	                        End Enum
                                        End Class
                                        
                """,
                        GetBasicNoPluralResultAt(3, 38));
        }

        [Fact]
        public async Task CA1714_CA1717_Test_EnumWithFlags_SingularNameAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum Day 
                                               {
                                                    sunday = 0,
                                                    Monday = 1,
                                                    Tuesday = 2
                                                       
                                                };
                                            }
                """,
                            GetCSharpPluralResultAt(5, 44));

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                	                    <System.Flags> _
                	                    Public Enum Day
                		                    Sunday = 0
                		                    Monday = 1
                		                    Tuesday = 2
                	                    End Enum
                                        End Class
                """,
                            GetBasicPluralResultAt(4, 34));
        }

        [Fact, WorkItem(1432, "https://github.com/dotnet/roslyn-analyzers/issues/1432")]
        public async Task CA1714_CA1717_Test_EnumWithFlags_SingularName_InternalAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                class A 
                { 
                    [System.Flags] 
                    enum Day 
                    {
                        sunday = 0,
                        Monday = 1,
                        Tuesday = 2
                                                       
                    }
                }

                public class A2
                { 
                    [System.Flags] 
                    private enum Day 
                    {
                        sunday = 0,
                        Monday = 1,
                        Tuesday = 2
                                                       
                    }
                }

                internal class A3
                { 
                    [System.Flags] 
                    public enum Day 
                    {
                        sunday = 0,
                        Monday = 1,
                        Tuesday = 2
                                                       
                    }
                }

                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                Class A
                    <System.Flags> _
                    Enum Day
                	    Sunday = 0
                	    Monday = 1
                	    Tuesday = 2
                    End Enum
                End Class

                Public Class A2
                    <System.Flags> _
                    Private Enum Day
                	    Sunday = 0
                	    Monday = 1
                	    Tuesday = 2
                    End Enum
                End Class

                Friend Class A3
                    <System.Flags> _
                    Public Enum Day
                	    Sunday = 0
                	    Monday = 1
                	    Tuesday = 2
                    End Enum
                End Class

                """);
        }

        [Fact]
        public async Task CA1714_CA1717_Test_EnumWithFlags_PluralNameAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum Days 
                                               {
                                                    sunday = 0,
                                                    Monday = 1,
                                                    Tuesday = 2
                                                       
                                                };
                                            }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                	                    <System.Flags> _
                	                    Public Enum Days
                		                    Sunday = 0
                		                    Monday = 1
                		                    Tuesday = 2
                	                    End Enum
                                        End Class
                """);
        }

        [Fact]
        public async Task CA1714_CA1717_Test_EnumWithFlags_PluralName_UpperCaseAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum DAYS 
                                               {
                                                    sunday = 0,
                                                    Monday = 1,
                                                    Tuesday = 2

                                                };
                                            }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                	                    <System.Flags> _
                	                    Public Enum DAYS
                		                    Sunday = 0
                		                    Monday = 1
                		                    Tuesday = 2
                	                    End Enum
                                        End Class
                """);
        }

        [Fact, WorkItem(1323, "https://github.com/dotnet/roslyn-analyzers/issues/1323")]
        public async Task CA1714_CA1717_Test_EnumWithFlags_NonPluralNameEndsWithSAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum Axis 
                                               {
                                                    x = 0,
                                                    y = 1,
                                                    z = 2
                                                       
                                                };
                                            }
                """,
                            GetCSharpPluralResultAt(5, 44));

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                	                    <System.Flags> _
                	                    Public Enum Axis
                		                    x = 0
                		                    y = 1
                		                    z = 2
                	                    End Enum
                                        End Class
                """,
                        GetBasicPluralResultAt(4, 34));
        }

        [Fact, WorkItem(1323, "https://github.com/dotnet/roslyn-analyzers/issues/1323")]
        public async Task CA1714_CA1717_Test_EnumWithFlags_PluralNameEndsWithSAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum Axes 
                                               {
                                                    x = 0,
                                                    y = 1,
                                                    z = 2
                                                       
                                                };
                                            }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                	                    <System.Flags> _
                	                    Public Enum Axes
                		                    x = 0
                		                    y = 1
                		                    z = 2
                	                    End Enum
                                        End Class
                """);
        }

        [Fact, WorkItem(1323, "https://github.com/dotnet/roslyn-analyzers/issues/1323")]
        public async Task CA1714_CA1717_Test_EnumWithFlags_PluralName_NotEndingWithSAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum Men 
                                               {
                                                    M1 = 0,
                                                    M2 = 1,
                                                    M3 = 2
                                                       
                                                };
                                            }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                                        < System.Flags > _
                                        Public Enum Men
                                            M1 = 0
                                            M2 = 1
                                            M3 = 2
                                        End Enum
                                        End Class
                """);
        }

        [Fact, WorkItem(1323, "https://github.com/dotnet/roslyn-analyzers/issues/1323")]
        public async Task CA1714_CA1717_Test_EnumWithNoFlags_PluralWord_NotEndingWithSAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               public enum Men 
                                               {
                                                    M1 = 0,
                                                    M2 = 1,
                                                    M3 = 2
                                                       
                                                };
                                            }
                """,
                            GetCSharpNoPluralResultAt(4, 44));

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                                        Public Enum Men
                                            M1 = 0
                                            M2 = 1
                                            M3 = 2
                                        End Enum
                                        End Class
                """,
                        GetBasicNoPluralResultAt(3, 37));
        }

        [Fact, WorkItem(1323, "https://github.com/dotnet/roslyn-analyzers/issues/1323")]
        public async Task CA1714_CA1717_Test_EnumWithNoFlags_irregularPluralWord_EndingWith_aeAsync()
        {
            // Humanizer does not recognize 'formulae' as plural, but we skip words ending with 'ae'
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum formulae 
                                               {
                                                    M1 = 0,
                                                    M2 = 1,
                                                    M3 = 2

                                                };
                                            }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                                        < System.Flags > _
                                        Public Enum formulae
                                            M1 = 0
                                            M2 = 1
                                            M3 = 2
                                        End Enum
                                        End Class
                """);
        }

        [Fact, WorkItem(1323, "https://github.com/dotnet/roslyn-analyzers/issues/1323")]
        public async Task CA1714_CA1717_Test_EnumWithNoFlags_irregularPluralWord_EndingWith_iAsync()
        {
            // Humanizer does not recognize 'trophi' as plural, but we skip words ending with 'i'
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum trophi 
                                               {
                                                    M1 = 0,
                                                    M2 = 1,
                                                    M3 = 2
                                                       
                                                };
                                            }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                                        < System.Flags > _
                                        Public Enum trophi
                                            M1 = 0
                                            M2 = 1
                                            M3 = 2
                                        End Enum
                                        End Class
                """);
        }

        [Fact, WorkItem(1323, "https://github.com/dotnet/roslyn-analyzers/issues/1323")]
        public async Task CA1714_CA1717_Test_EnumWithNoFlags_NonAsciiAsync()
        {
            // We skip non-ASCII names.
            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               [System.Flags] 
                                               public enum UnicodeNameΔ
                                               {
                                                    M1 = 0,
                                                    M2 = 1,
                                                    M3 = 2

                                                };
                                            }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""

                                       Public Class A
                                        < System.Flags > _
                                        Public Enum UnicodeNameΔ
                                            M1 = 0
                                            M2 = 1
                                            M3 = 2
                                        End Enum
                                        End Class
                """);
        }

        [Theory, WorkItem(2229, "https://github.com/dotnet/roslyn-analyzers/issues/2229")]
        [InlineData("en-US")]
        [InlineData("es-ES")]
        [InlineData("pl-PL")]
        [InlineData("fi-FI")]
        [InlineData("de-DE")]
        public async Task CA1714_CA1717__Test_EnumWithNoFlags_PluralName_MultipleCulturesAsync(string culture)
        {
            var currentCulture = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(culture);

            await VerifyCS.VerifyAnalyzerAsync("""

                                            public class A 
                                            { 
                                               public enum Days 
                                                {
                                                    sunday = 0,
                                                    Monday = 1,
                                                    Tuesday = 2
                                                       
                                                };
                                            }
                """,
                            GetCSharpNoPluralResultAt(4, 44));

            await VerifyVB.VerifyAnalyzerAsync("""

                                        Public Class A
                	                        Public Enum Days
                		                           Sunday = 0
                		                           Monday = 1
                		                           Tuesday = 2

                	                        End Enum
                                        End Class
                                        
                """,
                        GetBasicNoPluralResultAt(3, 38));

            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
        }

        private static DiagnosticResult GetCSharpPluralResultAt(int line, int column)
#pragma warning disable RS0030 // Do not use banned APIs
            => VerifyCS.Diagnostic(EnumsShouldHavePluralNamesAnalyzer.Rule_CA1714)
                .WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        private static DiagnosticResult GetBasicPluralResultAt(int line, int column)
#pragma warning disable RS0030 // Do not use banned APIs
            => VerifyVB.Diagnostic(EnumsShouldHavePluralNamesAnalyzer.Rule_CA1714)
                .WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        private static DiagnosticResult GetCSharpNoPluralResultAt(int line, int column)
#pragma warning disable RS0030 // Do not use banned APIs
            => VerifyCS.Diagnostic(EnumsShouldHavePluralNamesAnalyzer.Rule_CA1717)
                .WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        private static DiagnosticResult GetBasicNoPluralResultAt(int line, int column)
#pragma warning disable RS0030 // Do not use banned APIs
            => VerifyVB.Diagnostic(EnumsShouldHavePluralNamesAnalyzer.Rule_CA1717)
                .WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs
    }
}
