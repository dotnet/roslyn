// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Enables legacy APIs to access global options from workspace.
/// Not available OOP. Only use in client code and when IGlobalOptionService can't be MEF imported.
/// </summary>
internal interface ILegacyGlobalOptionsWorkspaceService : IWorkspaceService
{
    public bool RazorUseTabs { get; }
    public int RazorTabSize { get; }

    public bool GenerateOverrides { get; set; }

    public bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language);
    public void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value);

    public bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language);
    public void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value);

    public bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language);
    public void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value);
}
