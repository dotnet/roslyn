// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public interface IInjectTargetExtension : ICodeTargetExtension
{
    void WriteInjectProperty(CodeRenderingContext context, InjectIntermediateNode node);
}
