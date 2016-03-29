// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input
{
    /// <summary>
    ///     Specifies the command group and ID of a given <see cref="AbstractProjectCommand"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = false)]
    [MetadataAttribute]
    internal class ProjectCommandAttribute : ExportAttribute
    {
        public ProjectCommandAttribute(string group, long commandId)
            : this(group, new long[] { commandId })
        {
        }

        public ProjectCommandAttribute(string group, params long[] commandIds)
            : base(typeof(IAsyncCommandGroupHandler))
        {
            Group = group;
            CommandIds = commandIds;
        }

        public long[] CommandIds
        {
            get;
        }

        public string Group
        {
            get;
        }
    }
}
