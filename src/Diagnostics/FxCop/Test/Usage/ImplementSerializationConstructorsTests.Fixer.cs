// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class CA2229FixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new SerializationRulesDiagnosticAnalyzer();
        }

        [WorkItem(858655)]
        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new ImplementSerializationConstructorsFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerializationRulesDiagnosticAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ImplementSerializationConstructorsFixer();
        }

        #region CA2229

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229NoConstructorFix()
        {
            VerifyCSharpFix(@"
using System;
using System.Runtime.Serialization;
[Serializable]
public class CA2229NoConstructor : ISerializable
{
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotImplementedException();
    }
}", @"
using System;
using System.Runtime.Serialization;
[Serializable]
public class CA2229NoConstructor : ISerializable
{
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotImplementedException();
    }

    protected CA2229NoConstructor(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
        throw new NotImplementedException();
    }
}");

            VerifyBasicFix(@"
Imports System
Imports System.Runtime.Serialization
<Serializable>
Public Class CA2229NoConstructor
    Implements ISerializable

    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
        throw new NotImplementedException()
    End Sub
End Class", @"
Imports System
Imports System.Runtime.Serialization
<Serializable>
Public Class CA2229NoConstructor
    Implements ISerializable

    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
        throw new NotImplementedException()
    End Sub

    Protected Sub New(serializationInfo As SerializationInfo, streamingContext As StreamingContext)
        Throw New NotImplementedException()
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongAccessibilityFix()
        {
            VerifyCSharpFix(@"
using System;
using System.Runtime.Serialization;
[Serializable]
public class CA2229HasConstructorWrongAccessibility : ISerializable
{
    public CA2229HasConstructorWrongAccessibility(SerializationInfo info, StreamingContext context) { }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotImplementedException();
    }
}", @"
using System;
using System.Runtime.Serialization;
[Serializable]
public class CA2229HasConstructorWrongAccessibility : ISerializable
{
    protected CA2229HasConstructorWrongAccessibility(SerializationInfo info, StreamingContext context) { }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotImplementedException();
    }
}");

            VerifyBasicFix(@"
Imports System
Imports System.Runtime.Serialization
<Serializable>
Public Class CA2229HasConstructorWrongAccessibility
    Implements ISerializable

    Public Sub New(info As SerializationInfo, context As StreamingContext)
    End Sub

    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
        throw new NotImplementedException()
    End Sub
End Class", @"
Imports System
Imports System.Runtime.Serialization
<Serializable>
Public Class CA2229HasConstructorWrongAccessibility
    Implements ISerializable

    Protected Sub New(info As SerializationInfo, context As StreamingContext)
    End Sub

    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
        throw new NotImplementedException()
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongAccessibility2Fix()
        {
            VerifyCSharpFix(@"
using System;
using System.Runtime.Serialization;
[Serializable]
public sealed class CA2229HasConstructorWrongAccessibility2 : ISerializable
{
    protected internal CA2229HasConstructorWrongAccessibility2(SerializationInfo info, StreamingContext context) { }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotImplementedException();
    }
}", @"
using System;
using System.Runtime.Serialization;
[Serializable]
public sealed class CA2229HasConstructorWrongAccessibility2 : ISerializable
{
    private CA2229HasConstructorWrongAccessibility2(SerializationInfo info, StreamingContext context) { }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotImplementedException();
    }
}");

            VerifyBasicFix(@"
Imports System
Imports System.Runtime.Serialization
<Serializable>
Public NotInheritable Class CA2229HasConstructorWrongAccessibility2
    Implements ISerializable

    Protected Friend Sub New(info As SerializationInfo, context As StreamingContext)
    End Sub

    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
        throw new NotImplementedException()
    End Sub
End Class", @"
Imports System
Imports System.Runtime.Serialization
<Serializable>
Public NotInheritable Class CA2229HasConstructorWrongAccessibility2
    Implements ISerializable

    Private Sub New(info As SerializationInfo, context As StreamingContext)
    End Sub

    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
        throw new NotImplementedException()
    End Sub
End Class");
        }

        #endregion
    }
}
