// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens
{
    [Export(typeof(IAsyncCodeLensDataPointProvider))]
    [Name(Id)]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [LocalizedName(typeof(CodeLensVSResources), "CSharp_VisualBasic_References")]
    [Priority(200)]
    [OptionUserModifiable(userModifiable: false)]
    [DetailsTemplateName("references")]
    internal class ReferenceCodeLensProvider : IAsyncCodeLensDataPointProvider
    {
        // TODO: do we need to localize this?
        private const string Id = "CSVBReferences";

        // these string are never exposed to users but internally used to identify 
        // each provider/servicehub connections and etc
        private const string HubClientId = "ManagedLanguage.IDE.CodeLensOOP";
        private const string RoslynCodeAnalysis = "roslynCodeAnalysis";

        private readonly HubClient _client;

        // this is lazy to get around circular MEF dependency issue
        private Lazy<ICodeLensCallbackService> _lazyCodeLensCallbackService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ReferenceCodeLensProvider(Lazy<ICodeLensCallbackService> codeLensCallbackService)
        {
            _client = new HubClient(HubClientId);

            // use lazy to break circular MEF dependency issue
            _lazyCodeLensCallbackService = codeLensCallbackService;
        }

        public Task<bool> CanCreateDataPointAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            if (descriptorContext is { ApplicableSpan: { HasValue: true } })
            {
                // we allow all reference points. 
                // engine will call this for all points our roslyn code lens (reference) tagger tagged.
                return SpecializedTasks.True;
            }

            return SpecializedTasks.False;
        }

        public async Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            var dataPoint = new DataPoint(
                this,
                _lazyCodeLensCallbackService.Value,
                descriptor,
                await GetConnectionAsync(cancellationToken).ConfigureAwait(false));

            await dataPoint.TrackChangesAsync(cancellationToken).ConfigureAwait(false);

            return dataPoint;
        }

        private async Task<Stream> GetConnectionAsync(CancellationToken cancellationToken)
        {
            // any exception from this will be caught by codelens engine and saved to log file and ignored.
            // this follows existing code lens behavior and user experience on failure is owned by codelens engine
            var hostGroupId = await _lazyCodeLensCallbackService.Value.InvokeAsync<string>(
                this, nameof(ICodeLensContext.GetHostGroupIdAsync), arguments: null, cancellationToken).ConfigureAwait(false);

            var hostGroup = new HostGroup(hostGroupId);
            var serviceDescriptor = new ServiceDescriptor(RoslynCodeAnalysis) { HostGroup = hostGroup };

            return await _client.RequestServiceAsync(serviceDescriptor, cancellationToken).ConfigureAwait(false);
        }

        private class DataPoint : IAsyncCodeLensDataPoint, IDisposable
        {
            private static readonly List<CodeLensDetailHeaderDescriptor> s_header = new List<CodeLensDetailHeaderDescriptor>()
            {
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
            };

            private readonly ReferenceCodeLensProvider _owner;
            private readonly JsonRpc _roslynRpc;
            private readonly ICodeLensCallbackService _callbackService;

            public DataPoint(
                ReferenceCodeLensProvider owner,
                ICodeLensCallbackService callbackService,
                CodeLensDescriptor descriptor,
                Stream stream)
            {
                _owner = owner;
                _callbackService = callbackService;

                Descriptor = descriptor;

                _roslynRpc = stream.CreateStreamJsonRpc(
                    target: new RoslynCallbackTarget(Invalidate),
                    owner._client.Logger,
                    SpecializedCollections.SingletonEnumerable(AggregateJsonConverter.Instance));

                _roslynRpc.StartListening();
            }

            public event AsyncEventHandler InvalidatedAsync;

            public CodeLensDescriptor Descriptor { get; }

            public async Task<CodeLensDataPointDescriptor> GetDataAsync(CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
            {
                var codeElementKind = GetCodeElementKindsString(Descriptor.Kind);

                // we always get data through VS rather than Roslyn OOP directly since we want final data rather than
                // raw data from Roslyn OOP such as razor find all reference results
                var referenceCount = await _callbackService.InvokeAsync<ReferenceCount>(
                    _owner,
                    nameof(ICodeLensContext.GetReferenceCountAsync),
                    new object[] { Descriptor, descriptorContext },
                    cancellationToken).ConfigureAwait(false);
                if (referenceCount == null)
                {
                    return null;
                }

                var referenceCountString = $"{referenceCount.Count}{(referenceCount.IsCapped ? "+" : string.Empty)}";
                return new CodeLensDataPointDescriptor()
                {
                    Description = referenceCount.Count == 1
                        ? string.Format(CodeLensVSResources._0_reference, referenceCountString)
                        : string.Format(CodeLensVSResources._0_references, referenceCountString),
                    IntValue = referenceCount.Count,
                    TooltipText = string.Format(CodeLensVSResources.This_0_has_1_references, codeElementKind, referenceCountString),
                    ImageId = null
                };

                string GetCodeElementKindsString(CodeElementKinds kind)
                {
                    switch (kind)
                    {
                        case CodeElementKinds.Method:
                            return CodeLensVSResources.method;
                        case CodeElementKinds.Type:
                            return CodeLensVSResources.type;
                        case CodeElementKinds.Property:
                            return CodeLensVSResources.property;
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
                var referenceLocationDescriptors = await _callbackService.InvokeAsync<IEnumerable<ReferenceLocationDescriptor>>(
                    _owner,
                    nameof(ICodeLensContext.FindReferenceLocationsAsync),
                    new object[] { Descriptor, descriptorContext },
                    cancellationToken).ConfigureAwait(false);


                var details = new CodeLensDetailsDescriptor
                {
                    Headers = s_header,
                    Entries = referenceLocationDescriptors.Select(referenceLocationDescriptor =>
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
                    }).ToList(),

                    // use default behavior
                    PaneNavigationCommands = null
                };

                return details;
            }

            private void Invalidate()
            {
                // fire and forget
                // this get called from roslyn remote host
                InvalidatedAsync?.InvokeAsync(this, EventArgs.Empty);
            }

            public async Task TrackChangesAsync(CancellationToken cancellationToken)
            {
                var guids = await _callbackService.InvokeAsync<List<Guid>>(
                    _owner,
                    nameof(ICodeLensContext.GetDocumentId),
                    new object[] { Descriptor.ProjectGuid, Descriptor.FilePath },
                    cancellationToken).ConfigureAwait(false);
                if (guids == null)
                {
                    return;
                }

                var documentId = DocumentId.CreateFromSerialized(
                    ProjectId.CreateFromSerialized(guids[0], Descriptor.ProjectGuid.ToString()),
                    guids[1],
                    Descriptor.FilePath);
                if (documentId == null)
                {
                    return;
                }

                // this asks Roslyn OOP to start track workspace changes and call back Invalidate on this type when there is one.
                // each data point owns 1 connection which is alive while data point is alive. and all communication is done through
                // that connection
                await _roslynRpc.InvokeWithCancellationAsync(
                    nameof(IRemoteCodeLensReferencesService.TrackCodeLensAsync), new object[] { documentId }, cancellationToken).ConfigureAwait(false);
            }

            public void Dispose()
            {
                // done. let connection go
                _roslynRpc.Dispose();

                // vsCallbackRpc is shared and we don't own. it is owned by the code lens engine
                // don't dispose it
            }

            private class RoslynCallbackTarget : IRemoteCodeLensDataPoint
            {
                private readonly Action _invalidate;

                public RoslynCallbackTarget(Action invalidate)
                {
                    _invalidate = invalidate;
                }

                public void Invalidate()
                {
                    _invalidate();
                }
            }
        }
    }
}
