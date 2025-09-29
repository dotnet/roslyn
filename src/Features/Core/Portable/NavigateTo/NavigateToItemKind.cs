// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.NavigateTo;

internal static class NavigateToItemKind
{
    public const string Line = nameof(Line);
    public const string File = nameof(File);
    public const string Class = nameof(Class);
    public const string Structure = nameof(Structure);
    public const string Interface = nameof(Interface);
    public const string Delegate = nameof(Delegate);
    public const string Enum = nameof(Enum);
    public const string Module = nameof(Module);
    public const string Constant = nameof(Constant);
    public const string EnumItem = nameof(EnumItem);
    public const string Field = nameof(Field);
    public const string Method = nameof(Method);
    public const string Property = nameof(Property);
    public const string Event = nameof(Event);
    public const string OtherSymbol = nameof(OtherSymbol);
}
