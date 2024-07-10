// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ImplementInterface;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementInterface), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementAbstractClass)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class CSharpImplementInterfaceCodeFixProvider() : AbstractImplementInterfaceCodeFixProvider<TypeSyntax>
{
    private const string CS0535 = nameof(CS0535); // 'Program' does not implement interface member 'System.Collections.IEnumerable.GetEnumerator()'
    private const string CS0737 = nameof(CS0737); // 'Class' does not implement interface member 'IInterface.M()'. 'Class.M()' cannot implement an interface member because it is not public.
    private const string CS0738 = nameof(CS0738); // 'C' does not implement interface member 'I.Method1()'. 'B.Method1()' cannot implement 'I.Method1()' because it does not have the matching return type of 'void'.

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [CS0535, CS0737, CS0738];

    protected override bool IsTypeInInterfaceBaseList(TypeSyntax type)
        => type.Parent is BaseTypeSyntax { Parent: BaseListSyntax } baseTypeParent && baseTypeParent.Type == type;
}
