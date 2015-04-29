// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class CA2235Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new SerializationRulesDiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerializationRulesDiagnosticAnalyzer();
        }

        [WorkItem(858655, "DevDiv")]
        #region CA2235

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2235WithOnlySerializableFields()
        {
            VerifyCSharp(@"
                using System;
                [Serializable]
                public class SerializableType { }
    
                [Serializable]
                public class CA2235WithOnlySerializableFields
                {
                    public SerializableType s1;
                    internal SerializableType s2;
                    private SerializableType s3;
                }");

            VerifyBasic(@"
                Imports System
                <Serializable>
                Public Class SerializableType
                End Class

                <Serializable>
                Public Class CA2235WithOnlySerializableFields 

                    Public s1 As SerializableType;
                    Friend s2 As SerializableType;
                    Private s3 As SerializableType;
                End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2235WithNonPublicNonSerializableFields()
        {
            VerifyCSharp(@"
                using System;
                public class NonSerializableType { }

                [Serializable]
                public class SerializableType { }
    
                [Serializable]
                public class CA2235WithNonPublicNonSerializableFields
                {
                    public SerializableType s1;
                    internal NonSerializableType s2;
                    private NonSerializableType s3;
                }",
                GetCA2235CSharpResultAt(12, 50, "s2", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"),
                GetCA2235CSharpResultAt(13, 49, "s3", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"));

            VerifyBasic(@"
                Imports System
                Public Class NonSerializableType
                End Class
                <Serializable>
                Public Class SerializableType
                End Class

                <Serializable>
                Public Class CA2235WithNonPublicNonSerializableFields 
                    Public s1 As SerializableType;
                    Friend s2 As NonSerializableType;
                    Private s3 As NonSerializableType;
                End Class",
                GetCA2235BasicResultAt(12, 28, "s2", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"),
                GetCA2235BasicResultAt(13, 29, "s3", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2235WithNonPublicNonSerializableFieldsWithScope()
        {
            VerifyCSharp(@"
                using System;
                public class NonSerializableType { }

                [Serializable]
                public class SerializableType { }
    
                [|[Serializable]
                public class CA2235WithNonPublicNonSerializableFields
                {
                    public SerializableType s1;
                    internal NonSerializableType s2;
                    private NonSerializableType s3;
                }|]

                [Serializable]
                public class Sample
                {
                    public SerializableType s1;
                    internal NonSerializableType s2;
                    private NonSerializableType s3;
                }",
                GetCA2235CSharpResultAt(12, 50, "s2", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"),
                GetCA2235CSharpResultAt(13, 49, "s3", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"));

            VerifyBasic(@"
                Imports System
                Public Class NonSerializableType
                End Class
                <Serializable>
                Public Class SerializableType
                End Class

                [|<Serializable>
                Public Class CA2235WithNonPublicNonSerializableFields 
                    Public s1 As SerializableType;
                    Friend s2 As NonSerializableType;
                    Private s3 As NonSerializableType;
                End Class|]

                <Serializable>
                Public Class Sample 
                    Public s1 As SerializableType;
                    Friend s2 As NonSerializableType;
                    Private s3 As NonSerializableType;
                End Class",
                GetCA2235BasicResultAt(12, 28, "s2", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"),
                GetCA2235BasicResultAt(13, 29, "s3", "CA2235WithNonPublicNonSerializableFields", "NonSerializableType"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2235InternalWithNonPublicNonSerializableFields()
        {
            VerifyCSharp(@"
                using System;
                public class NonSerializableType { }

                [Serializable]
                public class SerializableType { }
    
                [Serializable]
                internal class CA2235InternalWithNonPublicNonSerializableFields
                {
                    public NonSerializableType s1;
                    internal SerializableType s2;
                    private NonSerializableType s3;
                }",
                GetCA2235CSharpResultAt(11, 48, "s1", "CA2235InternalWithNonPublicNonSerializableFields", "NonSerializableType"),
                GetCA2235CSharpResultAt(13, 49, "s3", "CA2235InternalWithNonPublicNonSerializableFields", "NonSerializableType"));

            VerifyBasic(@"
                Imports System
                Public Class NonSerializableType
                End Class
                <Serializable>
                Public Class SerializableType
                End Class

                <Serializable>
                Friend Class CA2235InternalWithNonPublicNonSerializableFields 
                    Public s1 As NonSerializableType;
                    Friend s2 As SerializableType;
                    Private s3 As NonSerializableType;
                End Class",
                GetCA2235BasicResultAt(11, 28, "s1", "CA2235InternalWithNonPublicNonSerializableFields", "NonSerializableType"),
                GetCA2235BasicResultAt(13, 29, "s3", "CA2235InternalWithNonPublicNonSerializableFields", "NonSerializableType"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2235AutoProperties()
        {
            VerifyCSharp(@"
                using System;
                public class NonSerializableType { }

                [Serializable]
                public class SerializableType { }
    
                [Serializable]
                internal class CA2235WithAutoProperties
                {
                    public SerializableType s1;
                    internal NonSerializableType s2 {get; set; }
                }",
                GetCA2235CSharpResultAt(12, 50, "s2", "CA2235WithAutoProperties", "NonSerializableType"));

            VerifyBasic(@"
                Imports System
                Public Class NonSerializableType
                End Class
                <Serializable>
                Public Class SerializableType
                End Class

                <Serializable>
                Friend Class CA2235WithAutoProperties 
                    Public s1 As SerializableType
                    Friend Property s2 As NonSerializableType
                End Class",
                GetCA2235BasicResultAt(12, 37, "s2", "CA2235WithAutoProperties", "NonSerializableType"));
        }

        internal static string CA2235Name = "CA2235";
        internal static string CA2235Message = FxCopRulesResources.FieldIsOfNonSerializableType;

        private static DiagnosticResult GetCA2235CSharpResultAt(int line, int column, string fieldName, string containerName, string typeName)
        {
            return GetCSharpResultAt(line, column, CA2235Name, string.Format(CA2235Message, fieldName, containerName, typeName));
        }

        private static DiagnosticResult GetCA2235BasicResultAt(int line, int column, string fieldName, string containerName, string typeName)
        {
            return GetBasicResultAt(line, column, CA2235Name, string.Format(CA2235Message, fieldName, containerName, typeName));
        }
        #endregion
    }
}
