// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Test.Utilities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class WorkItemAttribute : Attribute
    {
        private readonly int _id;
        private readonly string _source;

        public WorkItemAttribute(int id, string source)
        {
            _id = id;
            _source = source;
        }
    }
}