// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveMembers;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveMembers
{
    [ExportWorkspaceService(typeof(IMoveMembersOptionService)), Shared]
    class TestMoveMembersOptionService : IMoveMembersOptionService
    {
        private IEnumerable<(string name, bool makeAbstract)>? _selection;
        public string? DestinationName { get; set; }

        [ImportingConstructor]
        public TestMoveMembersOptionService(
            [Import(AllowDefault = true)] IEnumerable<(string name, bool makeAbstract)>? selection = null,
            [Import(AllowDefault = true)] string? destinationName = null)
        {
            _selection = selection;
            DestinationName = destinationName;
        }

        public MoveMembersOptions? GetMoveMembersOptions(Document document, MoveMembersAnalysisResult analysis, MoveMembersEntryPoint entryPoint)
        {
            if (analysis == null)
            {
                return null;
            }

            var members = analysis.ValidMembersInType;
            var selectedMembers = _selection == null
                ? members.Select(m => (member: m, makeAbstract: false))
                : _selection.Select(s => (member: members.Single(symbol => symbol.Name == s.name), makeAbstract: s.makeAbstract));

            var memberAnalysis = selectedMembers.SelectAsArray(p => new MemberAnalysisResult(p.member, makeMemberDeclarationAbstract: p.makeAbstract));

            var generateNewTypeDestination = entryPoint switch
            {
                MoveMembersEntryPoint.ExtractInterface => true,
                MoveMembersEntryPoint.ExtraceClass => true,
                _ => false
            };

            var destination = generateNewTypeDestination
                ? new DestinationAnalysisResult(GetDestinationType(analysis, entryPoint), memberAnalysis)
                : analysis.DestinationAnalysisResults.Single(d => d.Destination.Name == DestinationName);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return new MoveMembersOptions(
                destination.Destination,
                memberAnalysis,
                fromTypeNode: analysis.SelectedNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration)!,
                originalType: analysis.SelectedType,
                newFileName: destination.Destination.Name + document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb",
                isNewType: generateNewTypeDestination);
        }

        private INamedTypeSymbol GetDestinationType(MoveMembersAnalysisResult analysis, MoveMembersEntryPoint entryPoint)
        {
            var conflictingNames = analysis.SelectedType.ContainingNamespace.GetTypeMembers().Select(s => s.Name);

            var name = DestinationName ?? entryPoint switch
            {
                MoveMembersEntryPoint.ExtractInterface => NameGenerator.GenerateUniqueInterfaceName(analysis.SelectedType.Name, analysis.SelectedType.TypeKind == TypeKind.Interface, name => !conflictingNames.Contains(name)),
                MoveMembersEntryPoint.ExtraceClass => NameGenerator.GenerateBaseTypeName(analysis.SelectedType.Name, (name) => !conflictingNames.Contains(name)),
                _ => throw new Exception()
            };

            return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default,
                accessibility: analysis.SelectedType.DeclaredAccessibility,
                modifiers: default,
                typeKind: entryPoint == MoveMembersEntryPoint.ExtractInterface ? TypeKind.Interface : TypeKind.Class,
                name: name,
                baseType: entryPoint == MoveMembersEntryPoint.ExtractInterface ? null : analysis.SelectedType.BaseType);
        }

    }
}
