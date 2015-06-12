// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack;
using Microsoft.AnalyzerPowerPack.CSharp.Usage;
using Microsoft.AnalyzerPowerPack.VisualBasic.Usage;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    public partial class CA2200Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicCA2200DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpCA2200DiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200CSharpTestWithLegalExceptionThrow()
        {
            VerifyCSharp(@"
using System;

class Program
{
    void CatchAndRethrowImplicitly()
    {
        try
        {
            throw new ArithmeticException();
        }
        catch (ArithmeticException e)
        { 
            throw;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200CSharpTestWithLegalExceptionThrowMultiple()
        {
            VerifyCSharp(@"
using System;

class Program
{
    void CatchAndRethrowExplicitly()
    {
        try
        {
            throw new ArithmeticException();
            throw new Exception();
        }
        catch (ArithmeticException e)
        {
            var i = new Exception();
            throw i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200CSharpTestWithLegalExceptionThrowNested()
        {
            VerifyCSharp(@"
using System;

class Program
{
    void CatchAndRethrowExplicitly()
    {   
        try
        {
            try
            {
                throw new ArithmeticException();
            }
            catch (ArithmeticException e)
            {
                throw;
            }
            catch (ArithmeticException)
                try
                {
                    throw new ArithmeticException();
                }
                catch (ArithmeticException i)
                {
                    throw e;
                }
            }
        }
        catch (Exception e)
        {
            throw;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200CSharpTestWithIllegalExceptionThrow()
        {
            VerifyCSharp(@"
using System;

class Program
{
    void CatchAndRethrowExplicitly()
    {
        try
        {
            ThrowException();
        }
        catch (ArithmeticException e)
        {
            throw e;
        }
    }

    void ThrowException()
    {
        throw new ArithmeticException();
    }
}",
           GetCA2200CSharpResultAt(14, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200CSharpTestWithIllegalExceptionThrowwithScope()
        {
            VerifyCSharp(@"
using System;

class Program
{
    void CatchAndRethrowExplicitly()
    {
        try
        {
            ThrowException();
        }
        catch (ArithmeticException e)
        {
            throw e;
        }
    }

    [|void ThrowException()
    {
        throw new ArithmeticException();
    }|]
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200CSharpTestWithIllegalExceptionThrowMultiple()
        {
            VerifyCSharp(@"
using System;

class Program
{
    void CatchAndRethrowExplicitly()
    {
        try
        {
            ThrowException();
        }
        catch (ArithmeticException e)
        {
            throw e;
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    void ThrowException()
    {
        throw new ArithmeticException();
    }
}",
           GetCA2200CSharpResultAt(14, 13),
           GetCA2200CSharpResultAt(18, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200CSharpTestWithIllegalExceptionThrowNested()
        {
            VerifyCSharp(@"
using System;

class Program
{
    void CatchAndRethrowExplicitly()
    {
        try
        {
            throw new ArithmeticException();
        }
        catch (ArithmeticException e)
        {
            try
            {
                throw new ArithmeticException();
            }
            catch (ArithmeticException i)
            {
                throw e;
            }
        }
    }
}",
           GetCA2200CSharpResultAt(20, 17));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200yVisualBasicTestWithLegalExceptionThrow()
        {
            VerifyBasic(@"
Imports System
Class Program
    Sub CatchAndRethrowExplicitly()
        Try
            Throw New ArithmeticException()
        Catch ex As Exception
            Throw
        End Try
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200VisualBasicTestWithLegalExceptionThrowMultiple()
        {
            VerifyBasic(@"
Imports System
Class Program
    Sub CatchAndRethrowExplicitly()
        Try
            Throw New ArithmeticException()
            Throw New Exception()
        Catch ex As Exception
            Dim i As New Exception()
            Throw i
        End Try
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200VisualBasicTestWithLegalExceptionThrowNested()
        {
            VerifyBasic(@"
Imports System
Class Program
    Sub CatchAndRethrowExplicitly()
        Try
            Try
                Throw New ArithmeticException()
            Catch ex As ArithmeticException
                Throw
            Catch i As ArithmeticException
                Try
                    Throw New ArithmeticException()
                Catch e As Exception
                    Throw ex
                End Try
            End Try
        Catch ex As Exception
            Throw
        End Try
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200VisualBasicTestWithIllegalExceptionThrow()
        {
            VerifyBasic(@"
Imports System
Class Program
    Sub CatchAndRethrowExplicitly()

        Try
            Throw New ArithmeticException()
        Catch e As ArithmeticException
            Throw e
        End Try
    End Sub
End Class",
           GetCA2200BasicResultAt(9, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200VisualBasicTestWithIllegalExceptionThrowMultiple()
        {
            VerifyBasic(@"
Imports System
Class Program
    Sub CatchAndRethrowExplicitly()

        Try
            Throw New ArithmeticException()
        Catch e As ArithmeticException
            Throw e
        Catch e As Exception
            Throw e
        End Try
    End Sub
End Class",
           GetCA2200BasicResultAt(9, 13),
           GetCA2200BasicResultAt(11, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200VisualBasicTestWithIllegalExceptionThrowMultipleWithScope()
        {
            VerifyBasic(@"
Imports System
Class Program
    Sub CatchAndRethrowExplicitly()

        Try
            Throw New ArithmeticException()
        Catch e As ArithmeticException
            Throw e
        [|Catch e As Exception
            Throw e
        End Try|]
    End Sub
End Class",
           GetCA2200BasicResultAt(11, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2200VisualBasicTestWithIllegalExceptionThrowNested()
        {
            VerifyBasic(@"
Imports System
Class Program
    Sub CatchAndRethrowExplicitly()

        Try
            Throw New ArithmeticException()
        Catch e As ArithmeticException
            Try
                Throw New ArithmeticException()
            Catch ex As Exception
                Throw e
            End Try
        End Try
    End Sub
End Class",
           GetCA2200BasicResultAt(12, 17));
        }

        internal static string CA2200Name = "CA2200";
        internal static string CA2200Message = AnalyzerPowerPackRulesResources.RethrowException;

        private static DiagnosticResult GetCA2200BasicResultAt(int line, int column)
        {
            return GetBasicResultAt(line, column, CA2200Name, CA2200Message);
        }

        private static DiagnosticResult GetCA2200CSharpResultAt(int line, int column)
        {
            return GetCSharpResultAt(line, column, CA2200Name, CA2200Message);
        }
    }
}
