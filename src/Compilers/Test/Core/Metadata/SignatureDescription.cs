// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class SignatureDescription
    {
        public string FullyQualifiedTypeName { get; set; }
        public string MemberName { get; set; }
        public string ExpectedSignature { get; set; }
    }
}
