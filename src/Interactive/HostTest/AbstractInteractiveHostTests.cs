// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    public abstract class AbstractInteractiveHostTests : CSharpTestBase
    {
        internal readonly static string HostRootPath = Path.Combine(Path.GetDirectoryName(typeof(AbstractInteractiveHostTests).Assembly.Location)!, "Host");
    }
}
