// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Test.Utilities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class WorkItemAttribute(int id, string source) : Attribute
    {
        public int Id { get; } = id;
        public string Source { get; } = source;
    }
}