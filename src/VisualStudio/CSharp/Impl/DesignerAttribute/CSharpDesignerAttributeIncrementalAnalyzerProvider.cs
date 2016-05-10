// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.DesignerAttribute
{
    [ExportPerLanguageIncrementalAnalyzerProvider(DesignerAttributeIncrementalAnalyzerProvider.Name, LanguageNames.CSharp), Shared]
    internal class CSharpDesignerAttributeIncrementalAnalyzerProvider : IPerLanguageIncrementalAnalyzerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _notificationService;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _asyncListeners;

        [ImportingConstructor]
        public CSharpDesignerAttributeIncrementalAnalyzerProvider(
            SVsServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _serviceProvider = serviceProvider;
            _notificationService = notificationService;
            _asyncListeners = asyncListeners;
        }

        public IIncrementalAnalyzer CreatePerLanguageIncrementalAnalyzer(Workspace workspace, IIncrementalAnalyzerProvider provider)
        {
            return new DesignerAttributeIncrementalAnalyzer(_serviceProvider, _notificationService, _asyncListeners);
        }

        private class DesignerAttributeIncrementalAnalyzer : AbstractDesignerAttributeIncrementalAnalyzer
        {
            public DesignerAttributeIncrementalAnalyzer(
                IServiceProvider serviceProvider,
                IForegroundNotificationService notificationService,
                IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
                base(serviceProvider, notificationService, asyncListeners)
            {
            }

            protected override IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(SyntaxNode node)
            {
                var compilationUnit = node as CompilationUnitSyntax;
                if (compilationUnit == null)
                {
                    return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }

                return compilationUnit.Members.SelectMany(GetAllTopLevelTypeDefined);
            }

            private IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(MemberDeclarationSyntax member)
            {
                var namespaceMember = member as NamespaceDeclarationSyntax;
                if (namespaceMember != null)
                {
                    return namespaceMember.Members.SelectMany(GetAllTopLevelTypeDefined);
                }

                var type = member as ClassDeclarationSyntax;
                if (type != null)
                {
                    return SpecializedCollections.SingletonEnumerable<SyntaxNode>(type);
                }

                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            protected override bool ProcessOnlyFirstTypeDefined()
            {
                return true;
            }

            protected override bool HasAttributesOrBaseTypeOrIsPartial(SyntaxNode typeNode)
            {
                var classNode = typeNode as ClassDeclarationSyntax;
                if (classNode != null)
                {
                    return classNode.AttributeLists.Count > 0 ||
                        classNode.BaseList != null ||
                        classNode.Modifiers.Any(SyntaxKind.PartialKeyword);
                }

                return false;
            }
        }
    }
}
