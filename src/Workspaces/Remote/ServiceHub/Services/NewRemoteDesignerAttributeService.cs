// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class NewRemoteDesignerAttributeService : ServiceBase, IRemoteNewDesignerAttributeService
    {
        public NewRemoteDesignerAttributeService(
            Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            StartService();
        }

        public Task ScanForDesignerAttributesAsync(CancellationToken cancellation)
        {
            return RunServiceAsync(() =>
            {
                var workspace = SolutionService.PrimaryWorkspace;
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new NewDesignerAttributeIncrementalAnalyzerProvider(this.EndPoint);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(NewDesignerAttributeIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return Task.CompletedTask;
            }, cancellation);
        }
    }

    internal class NewDesignerAttributeIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly RemoteEndPoint _endPoint;

        public NewDesignerAttributeIncrementalAnalyzerProvider(RemoteEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new NewDesignerAttributeIncrementalAnalyzer(_endPoint, workspace);
    }

    internal class NewDesignerAttributeIncrementalAnalyzer : IncrementalAnalyzerBase
    {
        private const string DataKey = "data";

        private readonly RemoteEndPoint _endPoint;
        private readonly Workspace _workspace;
        private readonly IPersistentStorage _storage;

        public NewDesignerAttributeIncrementalAnalyzer(RemoteEndPoint endPoint, Workspace workspace)
        {
            _endPoint = endPoint;
            _workspace = workspace;
            var storageService = _workspace.Services.GetRequiredService<IPersistentStorageService>();
            _storage = storageService.GetStorage(workspace.CurrentSolution);
        }

        //public override async Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        //{
        //    // Clear out the stream associated with the doc.  The next read will not be able to
        //    // parse this into RemoteDesignerAttributeInfo and will cause us to recompute the
        //    // information for this doc.
        //    await _storage.WriteStreamAsync(document, DataKey, new MemoryStream(), cancellationToken).ConfigureAwait(false);
        //}

        public override Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            => AnalyzeProjectAsync(project, specificDoc: null, cancellationToken);

        public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            // don't need to reanalyze file if just a method body was edited.  That can't
            // affect designer attributes.
            if (bodyOpt != null)
                return Task.CompletedTask;

            // When we register our analyzer we will get called into for every document to
            // 'reanalyze' them all.  Ignore those as we would prefer to analyze the project
            // en-mass.
            if (reasons.Contains(PredefinedInvocationReasons.Reanalyze))
                return Task.CompletedTask;

            return AnalyzeProjectAsync(document.Project, document, cancellationToken);
        }

        private async Task AnalyzeProjectAsync(Project project, Document? specificDoc, CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                return;

            var projectVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            var latestInfos = await ComputeLatestInfosAsync(
                project, projectVersion, specificDoc, cancellationToken).ConfigureAwait(false);

            // Now get all the values that actually changed and notify VS about them. We don't need
            // to tell it about the ones that didn't change since that will have no effect on the
            // user experience.
            var changedInfos = latestInfos.Where(i => i.changed).Select(i => i.info!.Value).ToList();

            await _endPoint.InvokeAsync(
                nameof(INewDesignerAttributeServiceCallback.RegisterDesignerAttributesAsync),
                new object[] { changedInfos },
                cancellationToken).ConfigureAwait(false);

            // now that we've notified VS, persist all the infos we have (changed or otherwise) back
            // to disk.  We want to do this even when the data is unchanged so that our version
            // stamps will be correct for the next time we come around to analyze this project.
            //
            // Note: we have a potential race condition here.  Specifically, for simplicity, the VS
            // side will return immediately, without actually notifying the project system.  That
            // means that we could persist the data to local storage that isn't in sync with what
            // the project system knows about.  i.e. if VS is closed or crashes before that
            // information is persisted, then these two systems will be in disagreement.  this is
            // believed to not be a big issue given how small a time window this would be and how
            // easy it would be to get out of that state (just edit the file).

            await PersistLatestInfosAsync(projectVersion, latestInfos, cancellationToken).ConfigureAwait(false);
        }

        private async Task PersistLatestInfosAsync(VersionStamp projectVersion, (Document, DesignerInfo? info, bool changed)[] latestInfos, CancellationToken cancellationToken)
        {
            foreach (var (doc, info, _) in latestInfos)
            {
                // Skip documents that didn't change contents/version at all.  No point in writing
                // back out the exact same data as before.
                if (info == null)
                    continue;

                using var memoryStream = new MemoryStream();
                using var writer = new ObjectWriter(memoryStream);

                info.Value.WriteTo(writer, projectVersion);

                memoryStream.Position = 0;
                await _storage.WriteStreamAsync(
                    doc, DataKey, memoryStream, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<(Document, DesignerInfo? info, bool changed)[]> ComputeLatestInfosAsync(
            Project project, VersionStamp projectVersion,
            Document? specificDoc, CancellationToken cancellationToken)
        {
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var designerCategoryType = compilation.DesignerCategoryAttributeType();

            using var _1 = ArrayBuilder<Task<(Document, DesignerInfo?, bool changed)>>.GetInstance(out var tasks);
            foreach (var document in project.Documents)
            {
                // If we're only analyzing a specific document, then skip the rest.
                if (specificDoc != null && document != specificDoc)
                    continue;

                tasks.Add(ComputeDesignerAttributeInfoAsync(
                    projectVersion, designerCategoryType, document, cancellationToken));
            }

            var latestInfos = await Task.WhenAll(tasks).ConfigureAwait(false);
            return latestInfos;
        }

        private async Task<(Document, DesignerInfo?, bool changed)> ComputeDesignerAttributeInfoAsync(
            VersionStamp projectVersion, INamedTypeSymbol? designerCategoryType,
            Document document, CancellationToken cancellationToken)
        {
            // First check and see if we have stored information for this doc and if that
            // information is up to date.
            using var stream = await _storage.ReadStreamAsync(document, DataKey, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            var persisted = DesignerInfo.TryRead(reader);
            if (persisted.category != null && persisted.projectVersion == projectVersion)
            {
                // We were able to read out the old data, and it matches our current project
                // version.  Just return back that nothing changed here.
                return default;
            }

            // We either haven't computed the designer info, or our data was out of date.  We need
            // to recompute here.  Figure out what the current category is, and if that's different
            // from what we previously stored.
            var category = await ComputeDesignerAttributeCategoryAsync(
                designerCategoryType, document, cancellationToken).ConfigureAwait(false);
            var info = new DesignerInfo
            {
                Category = category,
                DocumentId = document.Id,
            };

            return (document, info, category != persisted.category);
        }

        private async Task<string?> ComputeDesignerAttributeCategoryAsync(
            INamedTypeSymbol? designerCategoryType,
            Document document,
            CancellationToken cancellationToken)
        {
            // simple case.  If there's no DesignerCategory type in this compilation, then there's
            // definitely no designable types.  Just immediately bail out.
            if (designerCategoryType == null)
                return null;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var firstClass = FindFirstClass(
                syntaxFacts, syntaxFacts.GetMembersOfCompilationUnit(root), cancellationToken);
            if (firstClass == null)
                return null;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var firstClassType = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(firstClass, cancellationToken);
            return TryGetDesignerCategory(firstClassType, designerCategoryType, cancellationToken);
        }

        private string? TryGetDesignerCategory(
            INamedTypeSymbol classType,
            INamedTypeSymbol designerCategoryType,
            CancellationToken cancellationToken)
        {
            foreach (var type in classType.GetBaseTypesAndThis())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // if it has designer attribute, set it
                var attribute = type.GetAttributes().FirstOrDefault(d => designerCategoryType.Equals(d.AttributeClass));
                if (attribute != null && attribute.ConstructorArguments.Length == 1)
                {
                    return GetArgumentString(attribute.ConstructorArguments[0]);
                }
            }

            return null;
        }

        private static SyntaxNode? FindFirstClass(
            ISyntaxFactsService syntaxFacts, SyntaxList<SyntaxNode> members, CancellationToken cancellationToken)
        {
            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (syntaxFacts.IsNamespaceDeclaration(member))
                {
                    var firstClass = FindFirstClass(
                        syntaxFacts, syntaxFacts.GetMembersOfNamespaceDeclaration(member), cancellationToken);
                    if (firstClass != null)
                        return firstClass;
                }
                else if (syntaxFacts.IsClassDeclaration(member))
                {
                    return member;
                }
            }

            return null;
        }

        private static string? GetArgumentString(TypedConstant argument)
        {
            if (argument.Type == null ||
                argument.Type.SpecialType != SpecialType.System_String ||
                argument.IsNull ||
                !(argument.Value is string stringValue))
            {
                return null;
            }

            return stringValue.Trim();
        }
    }
}
