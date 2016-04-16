// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
