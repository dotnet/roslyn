// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Simplification;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Simplification)]
public class SimplifierTests
{
    private static Document GetDocument()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("CSharpTest", LanguageNames.CSharp);
        return workspace.AddDocument(project.Id, "CSharpFile.cs", SourceText.From("class C { }"));
    }

    [Fact]
    public async Task ExpandAsync_BadArguments()
    {
        var node = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Test"));
        var semanticModel = await GetDocument().GetRequiredSemanticModelAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentNullException>("node", () => Simplifier.ExpandAsync<SyntaxNode>(node: null!, document: null!));
        await Assert.ThrowsAsync<ArgumentNullException>("document", () => Simplifier.ExpandAsync(node: node, document: null!));
        await Assert.ThrowsAsync<ArgumentNullException>("document", () => Simplifier.ExpandAsync(token: default, document: null!));
    }

    [Fact]
    public async Task Expand_BadArguments()
    {
        var node = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Test"));
        var semanticModel = await GetDocument().GetRequiredSemanticModelAsync(CancellationToken.None);

        Assert.Throws<ArgumentNullException>("node", () => Simplifier.Expand<SyntaxNode>(node: null!, semanticModel: null!, workspace: null!));
        Assert.Throws<ArgumentNullException>("semanticModel", () => Simplifier.Expand(node, semanticModel: null!, workspace: null!));
        Assert.Throws<ArgumentNullException>("workspace", () => Simplifier.Expand(node, semanticModel, workspace: null!));
        Assert.Throws<ArgumentNullException>("semanticModel", () => Simplifier.Expand(token: default, semanticModel: null!, workspace: null!));
        Assert.Throws<ArgumentNullException>("workspace", () => Simplifier.Expand(token: default, semanticModel, workspace: null!));
    }

    [Fact]
    public async Task ReduceAsync_BadArguments()
    {
        var document = GetDocument();

#pragma warning disable RS0030 // Do not used banned APIs
        await Assert.ThrowsAsync<ArgumentNullException>("document", () => Simplifier.ReduceAsync(document: null!));
        await Assert.ThrowsAsync<ArgumentNullException>("document", () => Simplifier.ReduceAsync(document: null!, annotation: null!));
        await Assert.ThrowsAsync<ArgumentNullException>("annotation", () => Simplifier.ReduceAsync(document, annotation: null!));
        await Assert.ThrowsAsync<ArgumentNullException>("document", () => Simplifier.ReduceAsync(document: null!, span: default));
        await Assert.ThrowsAsync<ArgumentNullException>("document", () => Simplifier.ReduceAsync(document: null!, spans: null!));
        await Assert.ThrowsAsync<ArgumentNullException>("spans", () => Simplifier.ReduceAsync(document, spans: null!));
#pragma warning restore
    }

    [Fact]
    public async Task ReduceAsync_Options()
    {
        using var workspace = new AdhocWorkspace();
        var csProject = workspace.AddProject("CS", LanguageNames.CSharp);
        var vbProject = workspace.AddProject("VB", LanguageNames.VisualBasic);
        var csDocument = workspace.AddDocument(csProject.Id, "File.cs", SourceText.From("class C { }"));
        var vbDocument = workspace.AddDocument(vbProject.Id, "File.vb", SourceText.From("Class C : End Class"));

        var updatedOptions = GetOptionSetWithChangedPublicOptions(workspace.CurrentSolution.Options);

        // Validate that options are read from specified OptionSet:

        ValidateCSharpOptions((CSharpSimplifierOptions)await Simplifier.GetOptionsAsync(csDocument, updatedOptions, CancellationToken.None));
        ValidateVisualBasicOptions((VisualBasicSimplifierOptions)await Simplifier.GetOptionsAsync(vbDocument, updatedOptions, CancellationToken.None));

        // Validate that options are read from solution snapshot as a fallback (we have no editorconfig file, so all options should fall back):

        var solutionWithUpdatedOptions = workspace.CurrentSolution.WithOptions(updatedOptions);
        var csDocumentWithUpdatedOptions = solutionWithUpdatedOptions.GetRequiredDocument(csDocument.Id);
        var vbDocumentWithUpdatedOptions = solutionWithUpdatedOptions.GetRequiredDocument(vbDocument.Id);

        ValidateCSharpOptions((CSharpSimplifierOptions)await Simplifier.GetOptionsAsync(csDocumentWithUpdatedOptions, optionSet: null, CancellationToken.None));
        ValidateVisualBasicOptions((VisualBasicSimplifierOptions)await Simplifier.GetOptionsAsync(vbDocumentWithUpdatedOptions, optionSet: null, CancellationToken.None));

        static OptionSet GetOptionSetWithChangedPublicOptions(OptionSet options)
        {
            // all public options and their non-default values:

            var publicOptions = new (IOption, object)[]
            {
                (CodeStyleOptions.QualifyFieldAccess, false),
                (CodeStyleOptions.QualifyPropertyAccess, false),
                (CodeStyleOptions.QualifyMethodAccess, false),
                (CodeStyleOptions.QualifyEventAccess, false),
                (CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false),
                (CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false),
                (CSharpCodeStyleOptions.VarForBuiltInTypes, false),
                (CSharpCodeStyleOptions.VarWhenTypeIsApparent, false),
                (CSharpCodeStyleOptions.VarElsewhere, false),
                (CSharpCodeStyleOptions.PreferSimpleDefaultExpression, false),
                (CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.WhenMultiline),
            };

            var updatedOptions = options;
            foreach (var (option, newValue) in publicOptions)
            {
                var languages = (option is IPerLanguageOption) ? new[] { LanguageNames.CSharp, LanguageNames.VisualBasic } : new string?[] { null };

                foreach (var language in languages)
                {
                    var key = new OptionKey(option, language);
                    var current = (ICodeStyleOption)options.GetOption(key)!;
                    updatedOptions = updatedOptions.WithChangedOption(key, current.WithValue(newValue));
                }
            }

            return updatedOptions;
        }

        static void ValidateCommonOptions(SimplifierOptions simplifierOptions)
        {
            Assert.False(simplifierOptions.QualifyFieldAccess.Value);
            Assert.False(simplifierOptions.QualifyPropertyAccess.Value);
            Assert.False(simplifierOptions.QualifyMethodAccess.Value);
            Assert.False(simplifierOptions.QualifyEventAccess.Value);
            Assert.False(simplifierOptions.PreferPredefinedTypeKeywordInMemberAccess.Value);
            Assert.False(simplifierOptions.PreferPredefinedTypeKeywordInDeclaration.Value);
        }

        static void ValidateCSharpOptions(CSharpSimplifierOptions simplifierOptions)
        {
            ValidateCommonOptions(simplifierOptions);

            Assert.False(simplifierOptions.VarForBuiltInTypes.Value);
            Assert.False(simplifierOptions.VarWhenTypeIsApparent.Value);
            Assert.False(simplifierOptions.VarElsewhere.Value);
            Assert.False(simplifierOptions.PreferSimpleDefaultExpression.Value);
            Assert.Equal(PreferBracesPreference.WhenMultiline, simplifierOptions.PreferBraces.Value);
        }

        static void ValidateVisualBasicOptions(VisualBasicSimplifierOptions simplifierOptions)
        {
            ValidateCommonOptions(simplifierOptions);
        }
    }
}
