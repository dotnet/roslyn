// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Test.Common;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer($"Microsoft.AspNetCore.Razor.Test.Common.{nameof(FormattingFactDiscoverer)}", "Microsoft.AspNetCore.Razor.Test.Common")]
internal sealed class FormattingTestFactAttribute : FactAttribute
{
}
