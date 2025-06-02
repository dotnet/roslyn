// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal class ProjectSystemProjectOptionsProcessor : IDisposable
{
    private readonly ProjectSystemProject _project;
    private readonly SolutionServices _workspaceServices;
    private readonly ICommandLineParserService _commandLineParserService;

    /// <summary>
    /// A hashed checksum of the last command line we were set to.  We use this
    /// as a low cost (in terms of memory) way to determine if the command line
    /// actually changes and we need to make any downstream updates.
    /// </summary>
    private Checksum? _commandLineChecksum;

    private CommandLineArguments _commandLineArgumentsForCommandLine;
    private IReferenceCountedDisposable<ICacheEntry<string, IRuleSetFile>>? _ruleSetFile = null;

    /// <summary>
    /// To save space in the managed heap, we only cache the command line if we have a ruleset or if we are in a legacy project.
    /// </summary>
    private ImmutableArray<string> _commandLine;

    /// <summary>
    /// Gate to guard all mutable fields in this class.
    /// The lock hierarchy means you are allowed to call out of this class and into <see cref="_project"/> while holding the lock.
    /// </summary>
    protected readonly object _gate = new();

    public ProjectSystemProjectOptionsProcessor(
        ProjectSystemProject project,
        SolutionServices workspaceServices)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _workspaceServices = workspaceServices;
        _commandLineParserService = workspaceServices.GetLanguageServices(project.Language).GetRequiredService<ICommandLineParserService>();

        // Set up _commandLineArgumentsForCommandLine to a default. No lock taken since we're in
        // the constructor so nothing can race.

        // Silence NRT warning.  This will be initialized by the call below to ReparseCommandLineIfChanged_NoLock.
        _commandLineArgumentsForCommandLine = null!;
        ReparseCommandLineIfChanged_NoLock(arguments: []);
    }

    /// <returns><see langword="true"/> if the command line was updated.</returns>
    private bool ReparseCommandLineIfChanged_NoLock(ImmutableArray<string> arguments)
    {
        var checksum = Checksum.Create(arguments);
        if (_commandLineChecksum == checksum)
            return false;

        _commandLineChecksum = checksum;

        ReparseCommandLine_NoLock(arguments);
        _commandLine = ShouldSaveCommandLine(arguments) ? arguments : default;

        return true;
    }

    protected virtual bool ShouldSaveCommandLine(ImmutableArray<string> arguments)
    {
        // Only bother storing the command line if there is an effective ruleset, as that may
        // require a later reparse using it.
        return GetEffectiveRulesetFilePath() != null;
    }

    public void SetCommandLine(string commandLine)
    {
        if (commandLine == null)
            throw new ArgumentNullException(nameof(commandLine));

        var arguments = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: false);

        SetCommandLine([.. arguments]);
    }

    public void SetCommandLine(ImmutableArray<string> arguments)
    {
        lock (_gate)
        {
            // If we actually got a new command line, then update the project options, otherwise
            // we don't need to do anything.
            if (ReparseCommandLineIfChanged_NoLock(arguments))
            {
                UpdateProjectOptions_NoLock();
            }
        }
    }

    /// <summary>
    /// Returns the active path to the rule set file that is being used by this project, or null if there isn't a rule set file.
    /// </summary>
    public string? EffectiveRuleSetFilePath
    {
        get
        {
            // We take a lock when reading this because we might be in the middle of processing a file update on another
            // thread.
            lock (_gate)
            {
                return _ruleSetFile?.Target.Value.FilePath;
            }
        }
    }

    private void DisposeOfRuleSetFile_NoLock()
    {
        if (_ruleSetFile != null)
        {
            _ruleSetFile.Target.Value.UpdatedOnDisk -= RuleSetFile_UpdatedOnDisk;
            _ruleSetFile.Dispose();
            _ruleSetFile = null;
        }
    }

    private void ReparseCommandLine_NoLock(ImmutableArray<string> arguments)
    {
        // If arguments isn't set, we somehow lost the command line
        Debug.Assert(!arguments.IsDefault);

        _commandLineArgumentsForCommandLine = _commandLineParserService.Parse(arguments, Path.GetDirectoryName(_project.FilePath), isInteractive: false, sdkDirectory: null);
    }

    /// <summary>
    /// Returns the parsed command line arguments for the arguments set with <see cref="SetCommandLine(ImmutableArray{string})"/>.
    /// </summary>
    public CommandLineArguments GetParsedCommandLineArguments()
    {
        // Since this is just reading a single reference field, there's no reason to take a lock.
        return _commandLineArgumentsForCommandLine;
    }

    protected virtual string? GetEffectiveRulesetFilePath()
        => _commandLineArgumentsForCommandLine.RuleSetPath;

    protected void UpdateProjectOptions_NoLock()
    {
        var effectiveRuleSetPath = GetEffectiveRulesetFilePath();

        if (_ruleSetFile?.Target.Value.FilePath != effectiveRuleSetPath)
        {
            // We're changing in some way. Be careful: this might mean the path is switching to or from null, so either side so far
            // could be changed.
            DisposeOfRuleSetFile_NoLock();

            if (effectiveRuleSetPath != null)
            {
                // Ruleset service is not required across all our platforms
                _ruleSetFile = _workspaceServices.GetService<IRuleSetManager>()?.GetOrCreateRuleSet(effectiveRuleSetPath);

                if (_ruleSetFile != null)
                {
                    _ruleSetFile.Target.Value.UpdatedOnDisk += RuleSetFile_UpdatedOnDisk;
                }
            }
        }

        var compilationOptions = _commandLineArgumentsForCommandLine.CompilationOptions
            .WithConcurrentBuild(concurrent: false)
            .WithXmlReferenceResolver(new XmlFileResolver(_commandLineArgumentsForCommandLine.BaseDirectory))
            .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
            .WithStrongNameProvider(new DesktopStrongNameProvider([.. _commandLineArgumentsForCommandLine.KeyFileSearchPaths.WhereNotNull()], Path.GetTempPath()));

        // Override the default documentation mode.
        var documentationMode = _commandLineArgumentsForCommandLine.DocumentationPath != null ? DocumentationMode.Diagnose : DocumentationMode.Parse;
        var parseOptions = _commandLineArgumentsForCommandLine.ParseOptions
            .WithDocumentationMode(documentationMode);

        // We've computed what the base values should be; we now give an opportunity for any host-specific settings to be computed
        // before we apply them
        compilationOptions = ComputeCompilationOptionsWithHostValues(compilationOptions, _ruleSetFile?.Target.Value);
        parseOptions = ComputeParseOptionsWithHostValues(parseOptions);

        // For managed projects, AssemblyName has to be non-null, but the command line we get might be a partial command line
        // and not contain the existing value. Only update if we have one.
        _project.AssemblyName = _commandLineArgumentsForCommandLine.CompilationName ?? _project.AssemblyName;
        _project.CompilationOptions = compilationOptions;

        var fullOutputFilePath = (_commandLineArgumentsForCommandLine.OutputDirectory != null && _commandLineArgumentsForCommandLine.OutputFileName != null)
            ? Path.Combine(_commandLineArgumentsForCommandLine.OutputDirectory, _commandLineArgumentsForCommandLine.OutputFileName)
            : _commandLineArgumentsForCommandLine.OutputFileName;

        _project.CompilationOutputAssemblyFilePath = fullOutputFilePath ?? _project.CompilationOutputAssemblyFilePath;
        _project.GeneratedFilesOutputDirectory = _commandLineArgumentsForCommandLine.GeneratedFilesOutputDirectory;
        _project.ParseOptions = parseOptions;
        _project.ChecksumAlgorithm = _commandLineArgumentsForCommandLine.ChecksumAlgorithm;
    }

    private void RuleSetFile_UpdatedOnDisk(object? sender, EventArgs e)
    {
        Contract.ThrowIfNull(sender);

        lock (_gate)
        {
            // This event might have gotten fired "late" if the file change was already in flight. We can see if this is still our current file;
            // it won't be if this is disposed or was already changed to a different file. We hard-cast sender to an IRuleSetFile because if it's
            // something else that means our comparison below is definitely broken.
            if (_ruleSetFile?.Target.Value != (IRuleSetFile)sender)
            {
                return;
            }

            // The IRuleSetFile held by _ruleSetFile is now out of date. We'll dispose our old one first so as to let go of any old cached values.
            // Then, we must reparse: in the case where the command line we have from the project system includes a /ruleset, the computation of the
            // effective values was potentially done by the act of parsing the command line. Even though the command line didn't change textually,
            // the effective result did. Then we call UpdateProjectOptions_NoLock to reapply any values; that will also re-acquire the new ruleset
            // includes in the IDE so we can be watching for changes again.
            DisposeOfRuleSetFile_NoLock();
            ReparseCommandLine_NoLock(_commandLine);
            UpdateProjectOptions_NoLock();
        }
    }

    /// <summary>
    /// Overridden by derived classes to provide a hook to modify a <see cref="CompilationOptions"/> with any host-provided values that didn't come from
    /// the command line string.
    /// </summary>
    protected virtual CompilationOptions ComputeCompilationOptionsWithHostValues(CompilationOptions compilationOptions, IRuleSetFile? ruleSetFile)
        => compilationOptions;

    /// <summary>
    /// Override by derived classes to provide a hook to modify a <see cref="ParseOptions"/> with any host-provided values that didn't come from 
    /// the command line string.
    /// </summary>
    protected virtual ParseOptions ComputeParseOptionsWithHostValues(ParseOptions parseOptions)
        => parseOptions;

    public void Dispose()
    {
        lock (_gate)
        {
            DisposeOfRuleSetFile_NoLock();
        }
    }
}
