// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Used to tag test methods or types which are created for a given WorkItem
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WorkItemAttribute : Attribute
    {
        private readonly int id;
        private readonly string description;

        public int Id
        {
            get { return id; }
        }

        public string Description
        {
            get { return description; }
        }

        public WorkItemAttribute(int id)
            : this(id, string.Empty)
        {
        }

        public WorkItemAttribute(int id, string description)
        {
            this.id = id;
            this.description = description;
        }
    }
}