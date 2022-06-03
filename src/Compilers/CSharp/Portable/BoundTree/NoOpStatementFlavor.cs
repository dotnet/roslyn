// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum NoOpStatementFlavor
    {
        Default,

        // <summary> 
        // Marks a control yield point for emitted await operator; is processed by codegen; 
        // only allowed inside MoveNext methods generated for Async methods
        // </summary>
        AwaitYieldPoint,

        // <summary> 
        // Marks a control resume point for emitted await operator; is processed by codegen; 
        // only allowed inside MoveNext methods generated for Async methods
        // </summary>
        AwaitResumePoint
    }
}
