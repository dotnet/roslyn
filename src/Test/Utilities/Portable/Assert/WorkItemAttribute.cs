// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Used to tag test methods or types which are created for a given WorkItem
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WorkItemAttribute : Attribute
    {
        public int Id
        {
            get;
        }

        public string Location
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkItemAttribute"/>.
        /// </summary>
        /// <param name="id">The ID of the issue in the original tracker where the work item was first reported. This
        /// could be a GitHub issue or pull request number, or the number of a Microsoft-internal bug.</param>
        /// <param name="issueUri">The URI where the work item can be viewed. This is a link to work item
        /// <paramref name="id"/> in the original source.</param>
        public WorkItemAttribute(int id, string issueUri)
        {
            Id = id;
            Location = issueUri;
        }
    }
}
