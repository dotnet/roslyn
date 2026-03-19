// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

internal record struct TestProgress(
    [property: JsonPropertyName("testsPassed")] long TestsPassed,
    [property: JsonPropertyName("testsFailed")] long TestsFailed,
    [property: JsonPropertyName("testsSkipped")] long TestsSkipped,
    [property: JsonPropertyName("totalTests")] long TotalTests
);
