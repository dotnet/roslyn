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
        private readonly int _id;
        private readonly string _description;

        public int Id
        {
            get { return _id; }
        }

        public string Description
        {
            get { return _description; }
        }

        public WorkItemAttribute(int id)
            : this(id, string.Empty)
        {
        }

        public WorkItemAttribute(int id, string description)
        {
            _id = id;
            _description = description;
        }
    }
}
