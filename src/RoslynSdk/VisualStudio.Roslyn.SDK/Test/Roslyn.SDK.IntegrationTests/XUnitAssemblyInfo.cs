// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Sdk;
using Xunit.v3;
using Xunit.Harness;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
[assembly: Parallelization(Mode = ParallelMode.None)]
[assembly: RequireExtension("Roslyn.SDK.vsix")]
