// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ResultProviderTests
    {
        [Fact]
        public void DkmEvaluationFlagsConflict()
        {
            var values = Enum.GetValues<DkmEvaluationFlags>();
            Assert.False(values.Contains(ResultProvider.NoResults));
            Assert.False(values.Contains(ResultProvider.NotRoot));
        }
    }
}
