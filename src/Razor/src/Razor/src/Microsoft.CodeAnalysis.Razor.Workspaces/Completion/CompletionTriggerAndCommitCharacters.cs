// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class CompletionTriggerAndCommitCharacters(IClientCapabilitiesService clientCapabilitiesService)
{
    /// <summary>
    ///  Trigger character that can trigger both Razor and Delegation completion
    /// </summary>
    private const char TransitionCharacter = '@';

    private static readonly string[] s_commitCharacters = [" ", ">", ";", "="];

    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;

    private static readonly FrozenSet<char> _csharpTriggerCharacters = [' ', '(', '=', '#', '.', '<', '[', '{', '"', '/', ':', '~'];
    private static readonly FrozenSet<char> _vsHtmlTriggerCharacters = [TransitionCharacter, ':', '#', '.', '!', '*', ',', '(', '[', '-', '<', '&', '\\', '/', '\'', '"', '=', ':', ' ', '`'];
    private static readonly FrozenSet<char> _vsCodeHtmlTriggerCharacters = [TransitionCharacter, '#', '.', '!', ',', '<'];
    private static readonly FrozenSet<char> _razorTriggerCharacters = [TransitionCharacter, '<', ':', ' '];

    private FrozenSet<char> HtmlTriggerCharacters => _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions
        ? _vsHtmlTriggerCharacters
        : _vsCodeHtmlTriggerCharacters;

    private FrozenSet<char> DelegationTriggerCharacters => field ??= ComputeDelegationTriggerCharacters();

    public string[] AllTriggerCharacters => field ??= ComputeAllTriggerCharacters();

    /// <summary>
    /// This is the intersection of C# and HTML commit characters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We need to specify it so that platform can correctly calculate ApplicableToSpan
    /// This is needed to fix https://github.com/dotnet/razor/issues/10787 in particular
    /// </para>
    /// <para>
    /// However we shouldn't specify commit characters for VSCode, as it doesn't appear to need
    /// them and they interfere with normal item commit. e.g. see https://github.com/dotnet/vscode-csharp/issues/7678
    /// </para>
    /// </remarks>
    public string[] AllCommitCharacters => _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions ? s_commitCharacters : [];

    private FrozenSet<char> ComputeDelegationTriggerCharacters()
    {
        // Delegation trigger characters (include '@' + C# and HTML trigger characters)
        var delegationTriggerCharacters = new HashSet<char> { TransitionCharacter };
        delegationTriggerCharacters.UnionWith(_csharpTriggerCharacters);
        delegationTriggerCharacters.UnionWith(HtmlTriggerCharacters);

        return delegationTriggerCharacters.ToFrozenSet();
    }

    private string[] ComputeAllTriggerCharacters()
    {
        // All trigger characters (include Razor + Delegation trigger characters)
        var allTriggerCharacters = new HashSet<char>();
        allTriggerCharacters.UnionWith(_razorTriggerCharacters);
        allTriggerCharacters.UnionWith(DelegationTriggerCharacters);

        return allTriggerCharacters.Select(static c => c.ToString()).ToArray();
    }

    public bool IsValidCSharpTrigger(CompletionContext completionContext)
        => IsValidTrigger(completionContext, _csharpTriggerCharacters);

    public bool IsValidDelegationTrigger(CompletionContext completionContext)
        => IsValidTrigger(completionContext, DelegationTriggerCharacters);

    public bool IsValidHtmlTrigger(CompletionContext completionContext)
        => IsValidTrigger(completionContext, HtmlTriggerCharacters);

    public bool IsValidRazorTrigger(CompletionContext completionContext)
        => IsValidTrigger(completionContext, _razorTriggerCharacters);

    private static bool IsValidTrigger(CompletionContext completionContext, FrozenSet<char> triggerCharacters)
        => completionContext.TriggerKind != CompletionTriggerKind.TriggerCharacter ||
           completionContext.TriggerCharacter is not [var c] ||
           triggerCharacters.Contains(c);

    public bool IsCSharpTriggerCharacter(string ch)
        => ch is [var c] && _csharpTriggerCharacters.Contains(c);

    public bool IsDelegationTriggerCharacter(string ch)
        => ch is [var c] && DelegationTriggerCharacters.Contains(c);

    public bool IsHtmlTriggerCharacter(string ch)
        => ch is [var c] && HtmlTriggerCharacters.Contains(c);

    public bool IsRazorTriggerCharacter(string ch)
        => ch is [var c] && _razorTriggerCharacters.Contains(c);

    public bool IsTransitionCharacter(string ch)
        => ch is [TransitionCharacter];
}
