// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;

internal enum ProjectSystemUpdateAction
{
    SetTreatAsUsed = 0,
    UnsetTreatAsUsed = 1,
    Remove = 2,
}
