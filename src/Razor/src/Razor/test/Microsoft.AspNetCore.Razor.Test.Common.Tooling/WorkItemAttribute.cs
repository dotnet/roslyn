// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Test.Common;

/// <summary>
/// Used to tag test methods or types which are created for a given WorkItem
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class WorkItemAttribute : Attribute
{
    public string Location
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkItemAttribute"/>.
    /// </summary>
    /// <param name="issueUri">The URI where the original work item can be viewed.</param>
    public WorkItemAttribute(string issueUri)
    {
        Location = issueUri;
    }
}
