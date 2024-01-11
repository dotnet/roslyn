// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class LocalizationInfraTests : CSharpTestBase
{
    /// <summary>
    /// This is a test for our infra to ensure we don't regress on our intented way that 
    /// <see cref="CultureInfo" /> should be expressed when executing tests. Our approach
    /// to testing localization is predicated on these invariants holding.
    /// </summary>
    [Fact]
    public void VerifyCulture()
    {
        var source = """
            using System;
            using System.Globalization;

            Console.WriteLine(CultureInfo.CurrentCulture.Name);
            Console.WriteLine(CultureInfo.CurrentUICulture.Name);
            Console.WriteLine((double)2.1);
            Console.WriteLine((decimal)2.1);
            """;

        // Our tests should be forcing the UI culture to the current culture if they 
        // are different. This forces better analysis in situations where machines are
        // say culture=es-ES but ui culture=en-US. 
        var uiCulture = CultureInfo.CurrentUICulture.Name == CultureInfo.CurrentCulture.Name
            ? CultureInfo.CurrentUICulture
            : CultureInfo.CurrentCulture;
        var expectedOutput = $"""
            {CultureInfo.CurrentCulture}
            {CultureInfo.CurrentCulture}
            {((double)2.1).ToString(CultureInfo.CurrentCulture)}
            {((decimal)2.1).ToString(CultureInfo.CurrentCulture)}
            """;
        _ = CompileAndVerify(source, expectedOutput: expectedOutput);
    }
}
