// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using IAsyncCodeLensDataPoint = Microsoft.VisualStudio.Language.CodeLens.Remoting.IAsyncCodeLensDataPoint;
using IAsyncCodeLensDataPointProvider = Microsoft.VisualStudio.Language.CodeLens.Remoting.IAsyncCodeLensDataPointProvider;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens
{
    [Export(typeof(IAsyncCodeLensDataPointProvider))]
    [Name(Id)]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [LocalizedName(typeof(FeaturesResources), nameof(FeaturesResources.CSharp_VisualBasic_References))]
    [Priority(200)]
    [OptionUserModifiable(userModifiable: false)]
    [DetailsTemplateName("references")]
    internal class ReferenceCodeLensProvider : IAsyncCodeLensDataPointProvider, IDisposable
    {
        // TODO: do we need to localize this?
        private const string Id = "CSVBReferences";

        // this is lazy to get around circular MEF dependency issue
        private readonly Lazy<ICodeLensCallbackService> _lazyCodeLensCallbackService;

        // Map of project GUID -> data points
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Task? _pollingTask;
        private readonly Dictionary<Guid, (string version, HashSet<DataPoint> dataPoints)> _dataPoints = [];

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ReferenceCodeLensProvider(Lazy<ICodeLensCallbackService> codeLensCallbackService)
        {
            // use lazy to break circular MEF dependency issue
            _lazyCodeLensCallbackService = codeLensCallbackService;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public Task<bool> CanCreateDataPointAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            if (descriptorContext != null && descriptorContext.ApplicableSpan.HasValue)
            {
                // we allow all reference points. 
                // engine will call this for all points our roslyn code lens (reference) tagger tagged.
                return SpecializedTasks.True;
            }

            return SpecializedTasks.False;
        }

        public Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            var dataPoint = new DataPoint(
                this,
                _lazyCodeLensCallbackService.Value,
                descriptor);

            AddDataPoint(dataPoint);
            return Task.FromResult<IAsyncCodeLensDataPoint>(dataPoint);
        }

        // The current CodeLens OOP design does not allow us to register an event handler for WorkspaceChanged events
        // which occur in devenv.exe. We instead poll for changes to the projects, and invalidate data points when
        // changes are detected.
        //
        // This behavior is expected to change when CodeLens is rewritten using LSP.
        private async Task PollForUpdatesAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.5), _cancellationTokenSource.Token).ConfigureAwait(false);

                ImmutableArray<Guid> keys;
                lock (_dataPoints)
                {
                    keys = _dataPoints.Keys.ToImmutableArray();
                }

                var projectVersions = await _lazyCodeLensCallbackService.Value.InvokeAsync<ImmutableDictionary<Guid, string>>(
                    this,
                    nameof(ICodeLensContext.GetProjectVersionsAsync),
                    [keys],
                    _cancellationTokenSource.Token).ConfigureAwait(false);

                lock (_dataPoints)
                {
                    foreach (var (projectGuid, newVersion) in projectVersions)
                    {
                        if (_dataPoints.TryGetValue(projectGuid, out var oldVersionedPoints)
                            && newVersion != oldVersionedPoints.version)
                        {
                            foreach (var dataPoint in oldVersionedPoints.dataPoints)
                                dataPoint.Invalidate();

                            _dataPoints[projectGuid] = (newVersion, oldVersionedPoints.dataPoints);
                        }
                    }
                }
            }
        }

        private void AddDataPoint(DataPoint dataPoint)
        {
            lock (_dataPoints)
            {
                var versionedPoints = _dataPoints.GetOrAdd(dataPoint.Descriptor.ProjectGuid, _ => (version: VersionStamp.Default.ToString(), dataPoints: new HashSet<DataPoint>()));
                versionedPoints.dataPoints.Add(dataPoint);

                _pollingTask ??= Task.Run(PollForUpdatesAsync).ReportNonFatalErrorAsync();
            }
        }

        private void RemoveDataPoint(DataPoint dataPoint)
        {
            lock (_dataPoints)
            {
                if (_dataPoints.TryGetValue(dataPoint.Descriptor.ProjectGuid, out var points)
                    && points.dataPoints.Remove(dataPoint)
                    && points.dataPoints.Count == 0)
                {
                    _dataPoints.Remove(dataPoint.Descriptor.ProjectGuid);
                }
            }
        }

        private class DataPoint : IAsyncCodeLensDataPoint, IDisposable
        {
            private static readonly List<CodeLensDetailHeaderDescriptor> s_header =
            [
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.FilePath },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.LineNumber },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.ColumnNumber },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.ReferenceText },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.ReferenceStart },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.ReferenceEnd },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.ReferenceLongDescription },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.ReferenceImageId },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.TextBeforeReference2 },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.TextBeforeReference1 },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.TextAfterReference1 },
                new CodeLensDetailHeaderDescriptor() { UniqueName = ReferenceEntryFieldNames.TextAfterReference2 },
            ];

            private readonly ReferenceCodeLensProvider _owner;
            private readonly ICodeLensCallbackService _callbackService;

            private ReferenceCount? _calculatedReferenceCount;

            public DataPoint(
                ReferenceCodeLensProvider owner,
                ICodeLensCallbackService callbackService,
                CodeLensDescriptor descriptor)
            {
                _owner = owner;
                _callbackService = callbackService;

                Descriptor = descriptor;
            }

            public void Dispose()
            {
                _owner.RemoveDataPoint(this);
            }

            public event AsyncEventHandler? InvalidatedAsync;

            public CodeLensDescriptor Descriptor { get; }

            public async Task<CodeLensDataPointDescriptor?> GetDataAsync(CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
            {
                var codeElementKind = GetCodeElementKindsString(Descriptor.Kind);

                // we always get data through VS rather than Roslyn OOP directly since we want final data rather than
                // raw data from Roslyn OOP such as razor find all reference results
                var referenceCountOpt = await _callbackService.InvokeAsync<ReferenceCount?>(
                    _owner,
                    nameof(ICodeLensContext.GetReferenceCountAsync),
                    [Descriptor, descriptorContext, _calculatedReferenceCount],
                    cancellationToken).ConfigureAwait(false);

                if (!referenceCountOpt.HasValue)
                {
                    return null;
                }

                var referenceCount = referenceCountOpt.Value;

                return new CodeLensDataPointDescriptor()
                {
                    Description = referenceCount.GetDescription(),
                    IntValue = referenceCount.Count,
                    TooltipText = referenceCount.GetToolTip(codeElementKind),
                    ImageId = null
                };

                static string GetCodeElementKindsString(CodeElementKinds kind)
                {
                    switch (kind)
                    {
                        case CodeElementKinds.Method:
                            return FeaturesResources.method;
                        case CodeElementKinds.Type:
                            return FeaturesResources.type;
                        case CodeElementKinds.Property:
                            return FeaturesResources.property_;
                        default:
                            // code lens engine will catch and ignore exception
                            // basically not showing data point
                            throw new NotSupportedException(nameof(kind));
                    }
                }
            }

            public async Task<CodeLensDetailsDescriptor> GetDetailsAsync(CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
            {
                // we always get data through VS rather than Roslyn OOP directly since we want final data rather than
                // raw data from Roslyn OOP such as razor find all reference results
                var referenceLocationDescriptors = await _callbackService.InvokeAsync<(string projectVersion, ImmutableArray<ReferenceLocationDescriptor> references)?>(
                    _owner,
                    nameof(ICodeLensContext.FindReferenceLocationsAsync),
                    [Descriptor, descriptorContext],
                    cancellationToken).ConfigureAwait(false);

                // Keep track of the exact reference count
                if (referenceLocationDescriptors.HasValue)
                {
                    var newCount = new ReferenceCount(referenceLocationDescriptors.Value.references.Length, IsCapped: false, Version: referenceLocationDescriptors.Value.projectVersion);
                    if (newCount != _calculatedReferenceCount)
                    {
                        _calculatedReferenceCount = newCount;
                        await InvalidatedAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
                    }
                }

                var entries = referenceLocationDescriptors?.references.Select(referenceLocationDescriptor =>
                {
                    ImageId imageId = default;
                    if (referenceLocationDescriptor.Glyph.HasValue)
                    {
                        var moniker = referenceLocationDescriptor.Glyph.Value.GetImageMoniker();
                        imageId = new ImageId(moniker.Guid, moniker.Id);
                    }

                    return new CodeLensDetailEntryDescriptor()
                    {
                        // use default since reference codelens don't require special behaviors
                        NavigationCommand = null,
                        NavigationCommandArgs = null,
                        Tooltip = null,
                        Fields = new List<CodeLensDetailEntryField>()
                        {
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.FilePath },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.LineNumber.ToString() },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.ColumnNumber.ToString() },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.ReferenceLineText },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.ReferenceStart.ToString() },
                            new CodeLensDetailEntryField() { Text = (referenceLocationDescriptor.ReferenceStart + referenceLocationDescriptor.ReferenceLength).ToString() },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.LongDescription },
                            new CodeLensDetailEntryField() { ImageId = imageId },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.BeforeReferenceText2 },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.BeforeReferenceText1 },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.AfterReferenceText1 },
                            new CodeLensDetailEntryField() { Text = referenceLocationDescriptor.AfterReferenceText2 }
                        },
                    };
                }).ToList();

                return new CodeLensDetailsDescriptor
                {
                    Headers = s_header,
                    Entries = entries ?? SpecializedCollections.EmptyList<CodeLensDetailEntryDescriptor>(),

                    // use default behavior
                    PaneNavigationCommands = null
                };
            }

            internal void Invalidate()
            {
                // fire and forget
                // this get called from roslyn remote host
                InvalidatedAsync?.InvokeAsync(this, EventArgs.Empty);
            }
        }
    }
}
