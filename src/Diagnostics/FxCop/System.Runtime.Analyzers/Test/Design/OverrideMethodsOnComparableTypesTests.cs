// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    [WorkItem(858659, "DevDiv")]
    public partial class OverrideMethodsOnComparableTypesTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new OverrideMethodsOnComparableTypesAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new OverrideMethodsOnComparableTypesAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassNoWarningCSharp()
        {
            VerifyCSharp(@"
    using System;

    public class A : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public static bool operator ==(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator !=(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }

");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassWrongEqualsCSharp()
        {
            VerifyCSharp(@"
    using System;

    public class A : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public bool Equals;

        public static bool operator ==(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator !=(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }

", GetCA1036CSharpResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassWrongEqualsCSharpwithScope()
        {
            VerifyCSharp(@"
    using System;

    [|public class A : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public static bool operator ==(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator !=(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }|]

    public class B : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public bool Equals;

        public static bool operator ==(B objLeft, B objRight)
        {
            return true;
        }

        public static bool operator !=(B objLeft, B objRight)
        {
            return true;
        }

        public static bool operator <(B objLeft, B objRight)
        {
            return true;
        }

        public static bool operator >(B objLeft, B objRight)
        {
            return true;
        }
    }

");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036StructNoWarningCSharp()
        {
            VerifyCSharp(@"
    using System;

    public struct A : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public static bool operator ==(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator !=(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }

");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036PrivateClassNoOpLessThanNoWarningCSharp()
        {
            VerifyCSharp(@"
    using System;

    public class class1
    {
        private class A : IComparable
        {    
            public override int GetHashCode()
            {
                return 1234;
            }

            public int CompareTo(object obj)
            {
                return 1;
            }

            public override bool Equals(object obj)
            {
                return true;
            }

            public static bool operator ==(A objLeft, A objRight)
            {
                return true;
            }

            public static bool operator !=(A objLeft, A objRight)
            {
                return true;
            }
        }
    }

");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassNoEqualsOperatorCSharp()
        {
            VerifyCSharp(@"
    using System;

    public class A : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public static bool operator ==(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator !=(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }
",
            GetCA1036CSharpResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassNoOpEqualsOperatorCSharp()
        {
            VerifyCSharp(@"
    using System;

    public class A : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }
",
            GetCA1036CSharpResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036StructNoOpLessThanOperatorCSharp()
        {
            VerifyCSharp(@"
    using System;

    public struct A : IComparable
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(object obj)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public static bool operator ==(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator !=(A objLeft, A objRight)
        {
            return true;
        }
    }
",
            GetCA1036CSharpResultAt(4, 19));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassWithGenericIComparableCSharp()
        {
            VerifyCSharp(@"
    using System;

    public class A : IComparable<int>
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(int obj)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }
",
            GetCA1036CSharpResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassWithDerivedIComparableCSharp()
        {
            VerifyCSharp(@"
    using System;

    interface  IDerived : IComparable<int> { }

    public class A : IDerived
    {    
        public override int GetHashCode()
        {
            return 1234;
        }

        public int CompareTo(int obj)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public static bool operator <(A objLeft, A objRight)
        {
            return true;
        }

        public static bool operator >(A objLeft, A objRight)
        {
            return true;
        }
    }
",
            GetCA1036CSharpResultAt(6, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassNoWarningBasic()
        {
            VerifyBasic(@"
Imports System

Public Class A : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator >(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036StructWrongEqualsBasic()
        {
            VerifyBasic(@"
Imports System

Public Structure A : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Shadows Property Equals

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator >(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
",
            GetCA1036BasicResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036StructWrongEqualsBasicWithScope()
        {
            VerifyBasic(@"
Imports System

[|Public Class A : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator >(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class|]

Public Structure B : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Shadows Property Equals

    Public Shared Operator =(objLeft As B, objRight As B) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As B, objRight As B) As Boolean
        Return True
    End Operator

    Public Shared Operator <(objLeft As B, objRight As B) As Boolean
        Return True
    End Operator

    Public Shared Operator >(objLeft As B, objRight As B) As Boolean
        Return True
    End Operator

End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036StructNoWarningBasic()
        {
            VerifyBasic(@"
Imports System

Public Structure A : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator >(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036PrivateClassNoOpLessThanNoWarningBasic()
        {
            VerifyBasic(@"
Imports System

Public Class Class1
    Private Class A : Implements IComparable

        Public Overrides Function GetHashCode() As Integer
            Return 1234
        End Function

        Public Function CompareTo(obj As Object) As Integer
            Return 1
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return True
        End Function

        Public Shared Operator =(objLeft As A, objRight As A) As Boolean
            Return True
        End Operator

        Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
            Return True
        End Operator

    End Class
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassNoEqualsOperatorBasic()
        {
            VerifyBasic(@"
Imports System

Public Class A : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator >(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
",
            GetCA1036BasicResultAt(4, 14));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassNoOpEqualsOperatorBasic()
        {
            VerifyBasic(@"
Imports System

Public Class A : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator <(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator >(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
",
            GetCA1036BasicResultAt(4, 14));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassNoOpLessThanOperatorBasic()
        {
            VerifyBasic(@"
Imports System

Public Structure A : Implements IComparable

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
",
            GetCA1036BasicResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassWithGenericIComparableBasic()
        {
            VerifyBasic(@"
Imports System

Public Structure A : Implements IComparable(Of Integer)

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Integer) As Integer
        Return 1
    End Function

    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
",
            GetCA1036BasicResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1036ClassWithDerivedIComparableBasic()
        {
            VerifyBasic(@"
Imports System

Public Interface IDerived 
    Inherits IComparable(Of Integer)
End Interface

Public Structure A : Implements IDerived

    Public Function Overrides GetHashCode() As Integer
        Return 1234
    End Function

    Public Function CompareTo(obj As Object) As Integer
        Return 1
    End Function

    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

    Public Shared Operator <>(objLeft As A, objRight As A) As Boolean
        Return True
    End Operator

End Class
",
            GetCA1036BasicResultAt(8, 18));
        }

        [WorkItem(1994, "https://github.com/dotnet/roslyn/issues/1994")]
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void Bug1994CSharp()
        {
            VerifyCSharp("enum MyEnum {}");
        }

        [WorkItem(1994, "https://github.com/dotnet/roslyn/issues/1994")]
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void Bug1994VisualBasic()
        {
            VerifyBasic(@"
Enum MyEnum
    ValueOne
    ValueTwo
End Enum");
        }

        internal static string CA1036Name = "CA1036";
        internal static string CA1036Message = "Overload operator Equals and comparison operators when implementing System.IComparable";

        private static DiagnosticResult GetCA1036CSharpResultAt(int line, int column)
        {
            return GetCSharpResultAt(line, column, CA1036Name, CA1036Message);
        }

        private static DiagnosticResult GetCA1036BasicResultAt(int line, int column)
        {
            return GetBasicResultAt(line, column, CA1036Name, CA1036Message);
        }
    }
}
