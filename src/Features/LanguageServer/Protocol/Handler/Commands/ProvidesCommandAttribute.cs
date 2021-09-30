﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Commands
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ProvidesCommandAttribute : ProvidesMethodAttribute
    {
        public ProvidesCommandAttribute(string command) : base(AbstractExecuteWorkspaceCommandHandler.GetRequestNameForCommandName(command))
        {
        }
    }
}
