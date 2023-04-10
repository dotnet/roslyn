// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[DataContract]
internal record struct TestProgress
{
    public TestProgress()
    {
    }

    [DataMember(Name = "testsPassed")]
    public long TestsPassed { get; set; } = 0;

    [DataMember(Name = "testsFailed")]
    public long TestsFailed { get; set; } = 0;

    [DataMember(Name = "testsSkipped")]
    public long TestsSkipped { get; set; } = 0;

    [DataMember(Name = "totalTests")]
    public long TotalTests { get; set; } = 0;
}