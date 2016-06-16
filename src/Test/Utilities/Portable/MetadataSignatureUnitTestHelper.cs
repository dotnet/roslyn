// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class SignatureDescription
    {
        public string FullyQualifiedTypeName { get; set; }
        public string MemberName { get; set; }
        public string ExpectedSignature { get; set; }
    }
}
