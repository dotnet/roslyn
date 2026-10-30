// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;

internal enum ProjectSystemReferenceType
{
    Unknown = 0,
    Assembly = 1,
    Package = 2,
    Project = 3,
    SDK = 4,
}
