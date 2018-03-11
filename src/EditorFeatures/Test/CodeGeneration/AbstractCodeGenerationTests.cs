// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    public abstract class AbstractCodeGenerationTests
    {
        internal void Test(
            Func<SyntaxGenerator, SyntaxNode> nodeCreator,
            string cs, string vb)
        {
            var hostServices = MefV1HostServices.Create(TestExportProvider.ExportProviderWithCSharpAndVisualBasic.AsExportProvider());
            var workspace = new AdhocWorkspace(hostServices);

            if (cs != null)
            {
                var csharpCodeGenService = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ICodeGenerationService>();
                var codeDefFactory = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<SyntaxGenerator>();

                var node = nodeCreator(codeDefFactory);
                node = node.NormalizeWhitespace();
                TokenUtilities.AssertTokensEqual(cs, node.ToFullString(), LanguageNames.CSharp);
            }

            if (vb != null)
            {
                var visualBasicCodeGenService = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<ICodeGenerationService>();
                var codeDefFactory = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<SyntaxGenerator>();

                var node = nodeCreator(codeDefFactory);
                node = node.NormalizeWhitespace();
                TokenUtilities.AssertTokensEqual(vb, node.ToString(), LanguageNames.VisualBasic);
            }
        }

        protected static ITypeSymbol CreateClass(string name)
        {
            return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                default(ImmutableArray<AttributeData>), default(Accessibility), default(DeclarationModifiers), TypeKind.Class, name);
        }
    }
}
