// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Test.Utilities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class WorkItemAttribute : Attribute
    {
        public WorkItemAttribute(int id, string source)
        {
            Id = id;
            Source = source;
        }

        public WorkItemAttribute(string issueUri) : this(-1, issueUri) { }

        public int Id { get; }
        public string Source { get; }
    }
}
