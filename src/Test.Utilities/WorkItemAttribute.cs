// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Test.Utilities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class WorkItemAttribute : Attribute
    {
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1801 // Remove unused parameter
        public WorkItemAttribute(int id, string source)
        {
        }
    }
}