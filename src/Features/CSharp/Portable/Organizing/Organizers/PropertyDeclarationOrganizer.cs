// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class PropertyDeclarationOrganizer : AbstractSyntaxNodeOrganizer<PropertyDeclarationSyntax>
    {
        [ImportingConstructor]
        public PropertyDeclarationOrganizer()
        {
        }

        protected override PropertyDeclarationSyntax Organize(
            PropertyDeclarationSyntax syntax,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            return syntax.WithModifiers(ModifiersOrganizer.ForCodeStyle(optionSet).Organize(syntax.Modifiers));
        }
    }
}
