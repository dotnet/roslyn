// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Used to tag test methods or types which are created for a given WorkItem
    /// </summary>
    [TraitDiscoverer("Microsoft.CodeAnalysis.Test.Utilities.WorkItemTraitDiscoverer", assemblyName: "Microsoft.CodeAnalysis.Test.Utilities")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WorkItemAttribute : Attribute, ITraitAttribute
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

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkItemAttribute"/>.
        /// </summary>
        /// <param name="issueUri">The URI where the work item can be viewed. This is a link to work item in the
        /// original source.</param>
        public WorkItemAttribute(string issueUri) : this(-1, issueUri)
        {
        }
    }
}
