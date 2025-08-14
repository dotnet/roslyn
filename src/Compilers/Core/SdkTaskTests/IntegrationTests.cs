// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.BuildTasks.UnitTests;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.Sdk.UnitTests;

public sealed class IntegrationTests(ITestOutputHelper output) : IntegrationTestBase(output);
