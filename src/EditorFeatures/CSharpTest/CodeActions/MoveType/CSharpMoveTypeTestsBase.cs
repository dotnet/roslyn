// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.MoveType;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public abstract class CSharpMoveTypeTestsBase : AbstractMoveTypeTest
    {
        protected override ParseOptions GetScriptOptions() => TestOptions.Script;

        protected internal override string GetLanguage() => LanguageNames.CSharp;
    }
}
