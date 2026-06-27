// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        var target = documentNode.Target;
        if (target == null)
        {
            var message = Resources.FormatDocumentMissingTarget(
                documentNode.DocumentKind,
                nameof(CodeTarget),
                nameof(DocumentIntermediateNode.Target));
            throw new InvalidOperationException(message);
        }

        // Fork: when the decl phase produced its half, this phase produces the matching
        // impl half -- a minimal partial class containing just the render method body and
        // any compiler-synthesized plumbing. Otherwise (non-component, suppressed primary
        // method body, malformed primary structure, or the decl phase didn't run for any
        // other reason) fall through to the original single-file lowering path.
        if (codeDocument.GetDeclCSharpDocument() is not null &&
            TryWriteImplDocument(documentNode, codeDocument, cancellationToken, out var implDocument))
        {
            return codeDocument.WithImplCSharpDocument(implDocument);
        }

        var csharpDocument = RazorCSharpDocumentWriter.Write(documentNode, codeDocument, cancellationToken: cancellationToken);
        return codeDocument.WithImplCSharpDocument(csharpDocument);
    }

    private static bool TryWriteImplDocument(
        DocumentIntermediateNode documentNode,
        RazorCodeDocument codeDocument,
        CancellationToken cancellationToken,
        out RazorCSharpDocument implDocument)
    {
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        var primaryNamespace = documentNode.FindPrimaryNamespace();

        // Defensive: splittability was already verified by the decl phase before it
        // produced a decl document, so this should not happen in practice. If a caller
        // bypasses the decl phase or the IR shape changes between phases, fall through
        // to the single-file path rather than crash.
        if (primaryClass is null || renderMethod is null || primaryNamespace is null)
        {
            implDocument = null!;
            return false;
        }

        // Build the impl synthetic tree: brand-new spine containing just the namespace,
        // its using directives, and a partial class wrapping renderMethod plus any
        // IsSynthesizedHelper nodes lifted from primaryClass / primaryNamespace. The
        // namespace-level walk preserves original order relative to primaryClass so an
        // attribute decoration that preceded the class in the original tree continues
        // to do so. IsGenericTyped helper namespaces are also lifted as siblings of the
        // primary namespace.
        //
        // Usings whose namespace is listed in codeDocument.GetImplSkipUsings() are omitted
        // here. The decl half (which always emits the full set) is the canonical occurrence
        // of those usings, so the C# compiler's CS0246/CS0234 fires exactly once when the
        // namespace doesn't resolve, instead of duplicating across both halves.
        var usings = primaryNamespace.FindDescendantNodes<UsingDirectiveIntermediateNode>();
        var implSkipUsings = codeDocument.GetImplSkipUsings();
        var implDocNode = RazorCSharpDocumentWriter.CloneContainer(documentNode);
        var implNamespace = RazorCSharpDocumentWriter.CloneContainer(primaryNamespace);
        // CloneContainerForImpl (vs CloneContainer) strips fields whose syntactic occurrence in
        // the impl half would duplicate a diagnostic the decl half already raises (BaseType,
        // Interfaces, type-parameter constraints, NullableContext). Partial-class merging unifies
        // these from the decl half, so the merged class is equivalent.
        var implClass = RazorCSharpDocumentWriter.CloneContainerForImpl(primaryClass);

        implClass.Children.Add(renderMethod);

        // Pull in compiler-synthesized helpers (e.g. __PrivateComponentRenderModeAttribute)
        // AND any user @code chunks the splitter routed to impl. The latter covers user
        // helper methods that contain markup -- they need to live where tag-helper
        // resolution has happened, which is here.
        var splitPlan = ClassBodySplitter.GetOrCreateSplitPlan(primaryClass, renderMethod, usings);

        // Synthesized helpers (compiler plumbing such as __PrivateComponentRenderModeAttribute)
        // are looked up via primary-class iteration; the splitter only owns user @code.
        foreach (var classChild in primaryClass.Children)
        {
            if (classChild.IsSynthesizedHelper)
            {
                implClass.Children.Add(classChild);
            }
        }

        // User @code routing flows entirely through the splitter's RoutedChunks. The
        // chunks are already in source order, and any CSharpCode chunk that straddled
        // members with different routing has been split into per-member / per-body
        // slices.
        //
        // NeedsHelperBody chunks for a single surface property may appear as a run of
        // consecutive entries (e.g. for `RenderFragment<T> Foo => (context) => @<p/>;`
        // the run is `(context) => ` + the markup Template). Each such run is wrapped
        // exactly once by the synth's open/close text so the captured user expression
        // remains syntactically intact. Synths are consumed in declaration order: each
        // time a new NeedsHelperBody run begins, the next pending synth is paired with
        // it; the run ends when a non-body chunk follows.
        HelperSynth? currentSynth = null;
        using var _ = QueuePool<HelperSynth>.GetPooledObject(out var pendingSynths);
        foreach (var synth in splitPlan.HelperSynths)
        {
            pendingSynths.Enqueue(synth);
        }

        void CloseCurrentSynth()
        {
            if (currentSynth is { } open)
            {
                implClass.Children.Add(IntermediateNodeFactory.CSharpCode(open.SynthImplCloseSource));
                currentSynth = null;
            }
        }

        foreach (var routed in splitPlan.RoutedChunks)
        {
            if (routed.Target == ChunkTarget.NeedsHelperBody)
            {
                if (currentSynth is null)
                {
                    if (pendingSynths.Count == 0)
                    {
                        // Defensive: no synth descriptor available for this body
                        // chunk; emit the chunk as-is (no wrapper) so its content
                        // isn't silently dropped.
                        implClass.Children.Add(routed.Chunk);
                        continue;
                    }
                    currentSynth = pendingSynths.Dequeue();
                    implClass.Children.Add(IntermediateNodeFactory.CSharpCode(currentSynth.SynthImplOpenSource));
                }
                implClass.Children.Add(routed.Chunk);
                continue;
            }

            // Any non-body chunk terminates the current synth's body run.
            CloseCurrentSynth();

            if (routed.Target == ChunkTarget.ImplOnly)
            {
                implClass.Children.Add(routed.Chunk);
            }
            // DeclOnly and NeedsHelperOmit chunks don't belong in impl.
        }

        // A NeedsHelperBody run that extends to the end of RoutedChunks still needs
        // its closing wrapper appended.
        CloseCurrentSynth();

        foreach (var usingDirective in usings)
        {
            if (implSkipUsings is not null && implSkipUsings.Contains(usingDirective.Content))
            {
                continue;
            }
            implNamespace.Children.Add(usingDirective);
        }

        foreach (var nsChild in primaryNamespace.Children)
        {
            if (nsChild == primaryClass)
            {
                implNamespace.Children.Add(implClass);
            }
            else if (nsChild.IsSynthesizedHelper)
            {
                implNamespace.Children.Add(nsChild);
            }
        }

        implDocNode.Children.Add(implNamespace);

        foreach (var docChild in documentNode.Children)
        {
            if (docChild is NamespaceDeclarationIntermediateNode { IsGenericTyped: true } genericNs)
            {
                implDocNode.Children.Add(genericNs);
            }
        }

        // Seed the impl root with the full diagnostic set (deduped by checksum) so any
        // diagnostics attached to documentNode / primaryNamespace / primaryClass themselves
        // -- which aren't reachable from the synthetic clone -- still surface on the impl
        // document.
        foreach (var diagnostic in documentNode.GetAllDiagnostics())
        {
            implDocNode.AddDiagnostic(diagnostic);
        }

        implDocument = RazorCSharpDocumentWriter.Write(implDocNode, codeDocument, cancellationToken: cancellationToken);
        return true;
    }
}
