// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.HubServices;
using Microsoft.CodeAnalysis.HubServices.SymbolSearch;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.VsHub;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using static System.FormattableString;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// A service which enables searching for packages matching certain criteria.
    /// It works against an <see cref="Microsoft.CodeAnalysis.Elfie"/> database to find results.
    /// 
    /// This implementation also spawns a task which will attempt to keep that database up to
    /// date by downloading patches on a daily basis.
    /// </summary>
    internal partial class PackageSearchService
    {
        // Internal for testing purposes.
        internal const string ContentAttributeName = "content";
        internal const string ChecksumAttributeName = "checksum";
        internal const string UpToDateAttributeName = "upToDate";
        internal const string TooOldAttributeName = "tooOld";
        internal const string NugetOrgSource = "nuget.org";

        private const string HostId = "RoslynNuGetSearch";
        private const string MicrosoftAssemblyReferencesName = "MicrosoftAssemblyReferences";
        private static readonly LinkedList<string> s_log = new LinkedList<string>();

        private readonly int _dataFormatVersion = AddReferenceDatabase.TextFileFormatVersion;

        /// <summary>
        /// Cancellation support for the task we use to keep the local database up to date.
        /// When VS shuts down it will dispose us.  We'll cancel the task at that point.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private readonly ConcurrentDictionary<string, object> _sourceToUpdateSentinel =
            new ConcurrentDictionary<string, object>();

        private readonly DirectoryInfo _cacheDirectoryInfo;
        //private readonly FileInfo _databaseFileInfo;

        // Interfaces that abstract out the external functionality we need.  Used so we can easily
        // mock behavior during tests.
        private readonly IPackageInstallerService _installerService;
        private readonly IPackageSearchDelayService _delayService;
        private readonly IPackageSearchIOService _ioService;
        private readonly IPackageSearchLogService _logService;
        private readonly IPackageSearchRemoteControlService _remoteControlService;
        private readonly IPackageSearchPatchService _patchService;
        private readonly IPackageSearchDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, bool> _reportAndSwallowException;

        public void Dispose()
        {
            // Cancel any existing work.
            _cancellationTokenSource.Cancel();
        }

        private void LogInfo(string text) => _logService.LogInfo(text);

        private void LogException(Exception e, string text) => _logService.LogException(e, text);

        private void OnPackageSourcesChanged(object sender, EventArgs e)
        {
            // Kick off a database update.  Wait a few seconds before starting so we don't
            // interfere too much with solution loading.
            var sources = _installerService.PackageSources;
            var json = new JObject(
                new JProperty(HubProtocolConstants.CacheDirectoryName, _cacheDirectoryInfo.FullName),
                new JProperty(HubProtocolConstants.PackageSourcesName, 
                    new JArray(sources.Select(ps => new JObject(new JProperty(ps.Name, ps.Source))))));

            var unused = _hubClient.SendRequestAsync(
                WellKnownHubServiceNames.SymbolSearch,
                nameof(SymbolSearchController.OnConfigurationChanged),
                json,
                _cancellationToken);
            unused.ContinueWith(_ =>
            {
                var result = _.Result;
            }, TaskScheduler.Default);
        }
    }
}