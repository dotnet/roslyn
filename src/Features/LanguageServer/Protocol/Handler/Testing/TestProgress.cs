// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

[DataContract]
internal record struct TestProgress(
    [property: DataMember(Name = "testsPassed")] long TestsPassed,
    [property: DataMember(Name = "testsFailed")] long TestsFailed,
    [property: DataMember(Name = "testsSkipped")] long TestsSkipped,
    [property: DataMember(Name = "totalTests")] long TotalTests
);
