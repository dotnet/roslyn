// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.EditAndContinue;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditAndContinue
{
    public class TraceLogTests
    {
        [Fact]
        public void Write()
        {
            var log = new TraceLog(5, "log");

            log.Write("a");
            log.Write("b {0} {1} {2}", 1, "x", 3);
            log.Write("c");
            log.Write("d {0} {1}", null, null);
            log.Write("e");
            log.Write("f");

            AssertEx.Equal(new[]
            {
                "f",
                "b 1 x 3",
                "c",
                "d <null> <null>",
                "e"
            }, log.GetTestAccessor().Entries.Select(e => e.ToString()));
        }
    }
}
