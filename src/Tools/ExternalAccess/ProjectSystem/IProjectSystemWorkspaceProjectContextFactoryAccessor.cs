// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    public interface IProjectSystemWorkspaceProjectContextFactoryAccessor
    {
        ProjectSystemWorkspaceProjectContextWrapper CreateProjectContext(string languageName, string projectUniqueName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath);
    }
}
