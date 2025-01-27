// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// Context and preferences.
/// </summary>
internal abstract class CodeGenerationContextInfo
{
    public readonly CodeGenerationContext Context;

    protected CodeGenerationContextInfo(CodeGenerationContext context)
    {
        Context = context;
    }

    public CodeGenerationContextInfo WithContext(CodeGenerationContext value)
        => WithContextImpl(value);

    public SyntaxGenerator Generator => GeneratorImpl;
    public CodeGenerationOptions Options => OptionsImpl;
    public ICodeGenerationService Service => ServiceImpl;

    protected abstract SyntaxGenerator GeneratorImpl { get; }
    protected abstract CodeGenerationOptions OptionsImpl { get; }
    protected abstract ICodeGenerationService ServiceImpl { get; }
    protected abstract CodeGenerationContextInfo WithContextImpl(CodeGenerationContext value);
}
