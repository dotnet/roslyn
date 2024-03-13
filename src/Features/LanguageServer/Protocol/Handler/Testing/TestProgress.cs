// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

[DataContract]
internal record struct TestProgress(
    [property: System.Text.Json.Serialization.JsonPropertyName("testsPassed")] long TestsPassed,
    [property: System.Text.Json.Serialization.JsonPropertyName("testsFailed")] long TestsFailed,
    [property: System.Text.Json.Serialization.JsonPropertyName("testsSkipped")] long TestsSkipped,
    [property: System.Text.Json.Serialization.JsonPropertyName("totalTests")] long TotalTests
);
