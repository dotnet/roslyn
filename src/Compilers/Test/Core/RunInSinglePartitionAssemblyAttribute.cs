// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Test.Utilities;

/// <summary>
/// This is a marker attribute used to define that all tests in this assembly
/// should run in a single partition without other assemblies due to state concerns.
/// For example, Microsoft.CodeAnalysis.CSharp.EndToEnd.UnitTests
/// 
/// This is looked at by RunTests when building test partitions.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class RunTestsInSinglePartitionAttribute : Attribute
{
}
