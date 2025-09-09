// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Xunit;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Roslyn.Test.Utilities.WpfFactDiscoverer", "Microsoft.CodeAnalysis.EditorFeatures.Test.Utilities")]
public class WpfFactAttribute : FactAttribute
{
}
