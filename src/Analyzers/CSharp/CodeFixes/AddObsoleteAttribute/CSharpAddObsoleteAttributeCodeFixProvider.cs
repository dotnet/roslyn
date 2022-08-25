// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddObsoleteAttribute;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.AddObsoleteAttribute
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddObsoleteAttribute), Shared]
    internal class CSharpAddObsoleteAttributeCodeFixProvider
        : AbstractAddObsoleteAttributeCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                "CS0612", //  'C' is obsolete 
                "CS0618", //  'C' is obsolete (msg)
                "CS0672", // Member 'D.F()' overrides obsolete member 'C.F()'
                "CS1062", // The best overloaded Add method 'MyCollection.Add(int)' for the collection initializer element is obsolete. (msg)
                "CS1064"  // The best overloaded Add method 'MyCollection.Add(int)' for the collection initializer element is obsolete"
            );

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpAddObsoleteAttributeCodeFixProvider()
            : base(CSharpSyntaxFacts.Instance, CSharpCodeFixesResources.Add_Obsolete)
        {
        }
    }
}
