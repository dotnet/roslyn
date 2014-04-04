// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    internal static partial class Features
    {
#if false
        public static class Workspace
        {
            /// <summary>
            /// All IWorkspaceServiceFactory implementations.
            /// </summary>
            public static ImmutableList<Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>> ServiceFactories
            {
                get
                {
                    return workspaceServiceFactories.Value;
                }
            }

            /// <summary>
            /// All IWorkspaceService implementations.
            /// </summary>
            public static ImmutableList<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> Services
            {
                get
                {
                    return workspaceServices.Value;
                }
            }

            /// <summary>
            /// All IOption implementations listed in the Workspace Feature Pack.
            /// </summary>
            public static ImmutableList<Lazy<IOption>> Options
            {
                get
                {
                    return workspaceOptions.Value;
                }
            }

            /// <summary>
            /// All IOptionProvider implementations.
            /// </summary>
            public static ImmutableList<Lazy<IOptionProvider>> OptionProviders
            {
                get
                {
                    return workspaceOptionProviders.Value;
                }
            }

            /// <summary>
            /// All IWorkspaceServiceProviderFactory implementations. 
            /// Implicitly constructs workspace service and all language services (C# and VB). 
            /// </summary>
            public static ImmutableList<Lazy<IWorkspaceServiceProviderFactory>> ProviderFactories
            {
                get
                {
                    return workspaceProviderFactories.Value;
                }
            }

            /// <summary>
            /// All ICodeCleanupProvider implementations. 
            /// </summary>
            public static ImmutableList<Lazy<ICodeCleanupProvider>> CodeCleanupProviders
            {
                get
                {
                    return codeCleanupProviders.Value;
                }
            }

            private static Lazy<ImmutableList<KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>>> services =
                new Lazy<ImmutableList<KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>>>(
                () => EnumerateWorkspaceServices().Select(ws =>
                    new KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>(ws.Metadata, (wsp) => ws.Value))
                        .Concat(EnumerateWorkspaceServiceFactories().Select(wsf =>
                            new KeyValuePair<WorkspaceServiceMetadata, Func<IWorkspaceServiceProvider, IWorkspaceService>>(wsf.Metadata, (wsp) =>
                                wsf.Value.CreateService(wsp)))).ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>>> workspaceServiceFactories =
                new Lazy<ImmutableList<Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>>>(() => EnumerateWorkspaceServiceFactories().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IWorkspaceService, WorkspaceServiceMetadata>>> workspaceServices =
                new Lazy<ImmutableList<Lazy<IWorkspaceService, WorkspaceServiceMetadata>>>(() => EnumerateWorkspaceServices().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IOption>>> workspaceOptions = new Lazy<ImmutableList<Lazy<IOption>>>(() => EnumerateWorkspaceOptions().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IWorkspaceServiceProviderFactory>>> workspaceProviderFactories =
                new Lazy<ImmutableList<Lazy<IWorkspaceServiceProviderFactory>>>(() => EnumerateWorkspaceServiceProviderFactory().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<ICodeCleanupProvider>>> codeCleanupProviders =
                new Lazy<ImmutableList<Lazy<ICodeCleanupProvider>>>(() => EnumerateCodeCleanupProviders().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IOptionProvider>>> workspaceOptionProviders =
                new Lazy<ImmutableList<Lazy<IOptionProvider>>>(() => EnumerateOptionProviders().ToImmutableList(), true);

            private static IEnumerable<Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>> EnumerateWorkspaceServiceFactories()
            {
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new LanguageServiceProviderFactoryWorkspaceServiceFactory(Exports.AllLanguages),
                    new WorkspaceServiceMetadata(typeof(ILanguageServiceProviderFactory), WorkspaceKind.Any), true);

                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new ServicesLayerExtensionManager(),
                    new WorkspaceServiceMetadata(typeof(IExtensionManager), WorkspaceKind.Any), true);

                // options
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new OptionsServiceFactory(Exports.Workspace.Options),
                    new WorkspaceServiceMetadata(typeof(IOptionService), WorkspaceKind.Any), true);

                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new DefaultBaseIndentationFormattingRuleFactoryServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(IBaseIndentationFormattingRuleFactoryService), WorkspaceKind.Any), true);

                // Background Compiler
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new BackgroundCompilerFactoryFactory(),
                    new WorkspaceServiceMetadata(typeof(IBackgroundCompilerFactory), WorkspaceKind.Any), true);

                // Background Parser
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new BackgroundParserFactoryFactory(),
                    new WorkspaceServiceMetadata(typeof(IBackgroundParserFactory), WorkspaceKind.Any), true);

                // Compilation Cache
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new CompilationCacheServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(ICompilationCacheService), WorkspaceKind.Any), true);

                // SyntaxTree Cache
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new SyntaxTreeCacheServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(ISyntaxTreeCacheService), WorkspaceKind.Any), true);

                // Text Cache
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new TextCacheServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(ITextCacheService), WorkspaceKind.Any), true);

                // File Tracking Service
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new FileTrackingServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(IFileTrackingService), WorkspaceKind.Any), true);

                // Shadow Copy Provider
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new AssemblyShadowCopyProviderServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(IMetadataReferenceProviderService), WorkspaceKind.Any), true);

                // Persistent Storage Service
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new PersistentStorageServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(IPersistentStorageService), WorkspaceKind.Any), true);

                // Temporary Storage Service
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new TemporaryStorageServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(ITemporaryStorageService), WorkspaceKind.Any), true);

                // Task Scheduler
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new WorkspaceTaskSchedulerFactoryFactory(),
                    new WorkspaceServiceMetadata(typeof(IWorkspaceTaskSchedulerFactory), WorkspaceKind.Any), true);

                // Text Factory
                yield return new Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>(
                    () => new TextFactoryServiceFactory(),
                    new WorkspaceServiceMetadata(typeof(ITextFactoryService), WorkspaceKind.Any), true);
            }

            private static IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> EnumerateWorkspaceServices()
            {
                yield return new Lazy<IWorkspaceService, WorkspaceServiceMetadata>(
                    () => new ProjectMetadataService(),
                    new WorkspaceServiceMetadata(typeof(IProjectMetadataService), WorkspaceKind.Any), true);
            }

            private static IEnumerable<Lazy<IOption>> EnumerateWorkspaceOptions()
            {
                // general recommendation options
                yield return new Lazy<IOption>(
                    () => RecommendationOptions.FilterOutOfScopeLocals);

                yield return new Lazy<IOption>(
                    () => RecommendationOptions.HideAdvancedMembers);

                // general formatting options
                yield return new Lazy<IOption>(
                    () => FormattingOptions.UseTabs);

                yield return new Lazy<IOption>(
                    () => FormattingOptions.TabSize);

                yield return new Lazy<IOption>(
                    () => FormattingOptions.IndentationSize);

                yield return new Lazy<IOption>(
                    () => FormattingOptions.DebugMode);
            }

            private static IEnumerable<Lazy<IOptionProvider>> EnumerateOptionProviders()
            {
                // the option provider that gathers all options that were exported as IOption's directly
                yield return new Lazy<IOptionProvider>(
                    () => new ExportedOptionProvider(Features.Workspace.Options), true);
            }

            private static IEnumerable<Lazy<IWorkspaceServiceProviderFactory>> EnumerateWorkspaceServiceProviderFactory()
            {
                // the top level factory that creates workspace service providers
                yield return new Lazy<IWorkspaceServiceProviderFactory>(
                    () => new WorkspaceServiceProviderFactory(Exports.Workspace.Services), true);
            }

            private static IEnumerable<Lazy<ICodeCleanupProvider>> EnumerateCodeCleanupProviders()
            {
                // code cleanup: It doesn't look like anyone imports these directly anymore
                yield return new Lazy<ICodeCleanupProvider>(
                    () => new SimplificationCodeCleanupProvider(), true);
            }
        }
#endif
    }
}
