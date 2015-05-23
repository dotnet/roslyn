// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.AnalyzerPowerPack;
using Microsoft.AnalyzerPowerPack.Usage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.UnitTests;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    public partial class CA2229Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new SerializationRulesDiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerializationRulesDiagnosticAnalyzer();
        }

        #region CA2229

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229NoConstructor()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                public class CA2229NoConstructor : ISerializable
                {
                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }",
                GetCA2229CSharpResultAt(5, 30, "CA2229NoConstructor", CA2229Message));

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Public Class CA2229NoConstructor
                    Implements ISerializable
                
                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class",
                GetCA2229BasicResultAt(5, 30, "CA2229NoConstructor", CA2229Message));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229NoConstructorInternal()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                internal class CA2229NoConstructorInternal : ISerializable
                {
                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }");

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Friend Class CA2229NoConstructorInternal
                    Implements ISerializable
                
                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructor()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                public class CA2229HasConstructor : ISerializable
                {
                    protected CA2229HasConstructor(SerializationInfo info, StreamingContext context) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }");

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Public Class CA2229HasConstructor
                    Implements ISerializable
                
                    Protected Sub New(info As SerializationInfo, context As StreamingContext)
                    End Sub

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructor1()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                public sealed class CA2229HasConstructor1 : ISerializable
                {
                    private CA2229HasConstructor1(SerializationInfo info, StreamingContext context) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }");

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Public NotInheritable Class CA2229HasConstructor1
                    Implements ISerializable
                
                    Private Sub New(info As SerializationInfo, context As StreamingContext)
                    End Sub

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongAccessibility()
        {
            VerifyCSharp(@"
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
                }",
                GetCA2229CSharpResultAt(7, 28, "CA2229HasConstructorWrongAccessibility", CA2229MessageUnsealed));

            VerifyBasic(@"
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
                End Class",
                GetCA2229BasicResultAt(8, 32, "CA2229HasConstructorWrongAccessibility", CA2229MessageUnsealed));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongAccessibilityWithScope()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;

                [|[Serializable]
                public sealed class CA2229HasConstructor1 : ISerializable
                {
                    private CA2229HasConstructor1(SerializationInfo info, StreamingContext context) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }|]

                [Serializable]
                public class CA2229HasConstructorWrongAccessibility : ISerializable
                {
                    public CA2229HasConstructorWrongAccessibility(SerializationInfo info, StreamingContext context) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }");

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization

                [|<Serializable>
                Public NotInheritable Class CA2229HasConstructor1
                    Implements ISerializable
                
                    Private Sub New(info As SerializationInfo, context As StreamingContext)
                    End Sub

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class|]

                <Serializable>
                Public Class CA2229HasConstructorWrongAccessibility
                    Implements ISerializable
                
                    Public Sub New(info As SerializationInfo, context As StreamingContext)
                    End Sub

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongAccessibility1()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                public class CA2229HasConstructorWrongAccessibility1 : ISerializable
                {
                    internal CA2229HasConstructorWrongAccessibility1(SerializationInfo info, StreamingContext context) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }",
                GetCA2229CSharpResultAt(7, 30, "CA2229HasConstructorWrongAccessibility1", CA2229MessageUnsealed));

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Public Class CA2229HasConstructorWrongAccessibility1
                    Implements ISerializable
                
                    Friend Sub New(info As SerializationInfo, context As StreamingContext)
                    End Sub

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class",
                GetCA2229BasicResultAt(8, 32, "CA2229HasConstructorWrongAccessibility1", CA2229MessageUnsealed));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongAccessibility2()
        {
            VerifyCSharp(@"
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
                }",
                GetCA2229CSharpResultAt(7, 40, "CA2229HasConstructorWrongAccessibility2", CA2229MessageSealed));

            VerifyBasic(@"
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
                End Class",
                GetCA2229BasicResultAt(8, 42, "CA2229HasConstructorWrongAccessibility2", CA2229MessageSealed));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongAccessibility3()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                public class CA2229HasConstructorWrongAccessibility3 : ISerializable
                {
                    protected internal CA2229HasConstructorWrongAccessibility3(SerializationInfo info, StreamingContext context) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }",
                GetCA2229CSharpResultAt(7, 40, "CA2229HasConstructorWrongAccessibility3", CA2229MessageUnsealed));

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Public Class CA2229HasConstructorWrongAccessibility3
                    Implements ISerializable
                
                    Protected Friend Sub New(info As SerializationInfo, context As StreamingContext)
                    End Sub

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class",
                GetCA2229BasicResultAt(8, 42, "CA2229HasConstructorWrongAccessibility3", CA2229MessageUnsealed));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229HasConstructorWrongOrder()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                public class CA2229HasConstructorWrongOrder : ISerializable
                {
                    protected CA2229HasConstructorWrongOrder(StreamingContext context, SerializationInfo info) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }",
                GetCA2229CSharpResultAt(5, 30, "CA2229HasConstructorWrongOrder", CA2229Message));

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Public Class CA2229HasConstructorWrongOrder
                    Implements ISerializable
                
                    Protected Sub New(context As StreamingContext, info As SerializationInfo)
                    End Sub

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class",
                GetCA2229BasicResultAt(5, 30, "CA2229HasConstructorWrongOrder", CA2229Message));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2229SerializableProper()
        {
            VerifyCSharp(@"
                using System;
                using System.Runtime.Serialization;
                [Serializable]
                public class CA2229SerializableProper : ISerializable
                {
                    public void GetObjectData(SerializationInfo info, StreamingContext context)
                    {
                        throw new NotImplementedException();
                    }
                }",
                GetCA2229CSharpResultAt(5, 30, "CA2229SerializableProper", CA2229Message));

            VerifyBasic(@"
                Imports System
                Imports System.Runtime.Serialization
                <Serializable>
                Public Class CA2229SerializableProper 
                    Implements ISerializable

                    Public Sub GetObjectData(info as SerializationInfo, context as StreamingContext)
                        throw new NotImplementedException()
                    End Sub
                End Class",
                GetCA2229BasicResultAt(5, 30, "CA2229SerializableProper", CA2229Message));
        }

        internal static string CA2229Name = "CA2229";
        internal static string CA2229Message = AnalyzerPowerPackRulesResources.SerializableTypeDoesntHaveCtor;
        internal static string CA2229MessageSealed = AnalyzerPowerPackRulesResources.SerializationCtorAccessibilityForSealedType;
        internal static string CA2229MessageUnsealed = AnalyzerPowerPackRulesResources.SerializationCtorAccessibilityForUnSealedType;

        private static DiagnosticResult GetCA2229CSharpResultAt(int line, int column, string objectName, string message)
        {
            return GetCSharpResultAt(line, column, CA2229Name, string.Format(message, objectName));
        }

        private static DiagnosticResult GetCA2229BasicResultAt(int line, int column, string objectName, string message)
        {
            return GetBasicResultAt(line, column, CA2229Name, string.Format(message, objectName));
        }

        #endregion
    }
}
