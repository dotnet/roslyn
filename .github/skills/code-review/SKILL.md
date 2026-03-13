---
name: code-review
description: Review code changes in dotnet/runtime for correctness, performance, and consistency with project conventions. Use when reviewing PRs or code changes.
---

# dotnet/runtime Code Review

Review code changes against conventions and patterns established by dotnet/runtime maintainers. These rules were extracted from 43,000+ maintainer review comments across 6,600+ PRs and represent the actual standards enforced in practice.

**Reviewer mindset:** Be polite but very skeptical. Your job is to help speed the review process for maintainers, which includes not only finding problems the PR author may have missed but also questioning the value of the PR in its entirety. Treat the PR description and linked issues as claims to verify, not facts to accept. Question the stated direction, probe edge cases, and don't hesitate to flag concerns even when unsure.

## When to Use This Skill

Use this skill when:
- Reviewing a PR or code change in dotnet/runtime
- Checking code for correctness, performance, style, or consistency issues before submitting a PR
- Asked to review, critique, or provide feedback on code changes
- Validating that a change follows dotnet/runtime conventions

## Review Process

### Step 0: Gather Code Context (No PR Narrative Yet)

Before analyzing anything, collect as much relevant **code** context as you can. **Critically, do NOT read the PR description, linked issues, or existing review comments yet.** You must form your own independent assessment of what the code does, why it might be needed, what problems it has, and whether the approach is sound — before being exposed to the author's framing. Reading the author's narrative first anchors your judgment and makes you less likely to find real problems.

1. **Diff and file list**: Fetch the full diff and the list of changed files.
2. **Full source files**: For every changed file, read the **entire source file** (not just the diff hunks). You need the surrounding code to understand invariants, locking protocols, call patterns, and data flow. Diff-only review is the #1 cause of false positives and missed issues.
3. **Consumers and callers**: If the change modifies a public/internal API, a type that others depend on, or a virtual/interface method, search for how consumers use the functionality. Grep for callers, usages, and test sites. Understanding how the code is consumed reveals whether the change could break existing behavior or violate caller assumptions.
4. **Sibling types and related code**: If the change fixes a bug or adds a pattern in one type, check whether sibling types (e.g., other abstraction implementations, other collection types, platform-specific variants) have the same issue or need the same fix. Fetch and read those files too.
5. **Key utility/helper files**: If the diff calls into shared utilities, read those to understand the contracts (thread-safety, idempotency, etc.).
6. **Git history**: Check recent commits to the changed files (`git log --oneline -20 -- <file>`). Look for related recent changes, reverts, or prior attempts to fix the same problem. This reveals whether the area is actively churning, whether a similar fix was tried and reverted, or whether the current change conflicts with recent work.

### Step 1: Form an Independent Assessment

Based **only** on the code context gathered above (without the PR description or issue), answer these questions:

1. **What does this change actually do?** Describe the behavioral change in your own words by reading the diff and surrounding code. What was the old behavior? What is the new behavior?
2. **Why might this change be needed?** Infer the motivation from the code itself. What bug, gap, or improvement does it appear to address?
3. **Is this the right approach?** Would a simpler alternative be more consistent with the codebase? Could the goal be achieved with existing functionality? Are there correctness, performance, or safety concerns?
4. **What problems do you see?** Identify bugs, edge cases, missing validation, thread-safety issues, performance regressions, API design problems, test gaps, and anything else that concerns you.

Write down your independent assessment before proceeding. You must produce a holistic assessment (see [Holistic PR Assessment](#holistic-pr-assessment)) at this stage.

### Step 2: Incorporate PR Narrative and Reconcile

Now read the PR description, labels, linked issues (in full), author information, existing review comments, and any related open issues in the same area. Treat all of this as **claims to verify**, not facts to accept.

1. **PR metadata**: Fetch the PR description, labels, linked issues, and author. Read linked issues in full — they often contain the repro, root cause analysis, and constraints the fix must satisfy.
2. **Related issues**: Search for other open issues in the same area (same labels, same component). This can reveal known problems the PR should also address, or constraints the author may not be aware of.
3. **Existing review comments**: Check if there are already review comments on the PR to avoid duplicating feedback.
4. **Reconcile your assessment with the author's claims.** Where your independent reading of the code disagrees with the PR description or issue, investigate further — but do not simply defer to the author's framing. If the PR claims a bug fix, a performance improvement, or a behavioral correction, verify those claims against the code and any provided evidence. If your independent assessment found problems the PR narrative doesn't acknowledge, those problems are more likely to be real, not less.
5. **Update your holistic assessment** if the additional context reveals information that genuinely changes your evaluation (e.g., a linked issue proves the bug is real, or an existing review comment already identified the same concern). But do not soften findings just because the PR description sounds reasonable.

### Step 3: Detailed Analysis

1. **Focus on what matters.** Prioritize bugs, performance regressions, safety issues, race conditions, resource management problems, incorrect assumptions about data or state, and API design problems. Do not comment on trivial style issues unless they violate an explicit rule below.
2. **Consider collateral damage.** For every changed code path, actively brainstorm: what other scenarios, callers, or inputs flow through this code? Could any of them break or behave differently after this change? If you identify any plausible risk — even one you can't fully confirm — surface it so the author can evaluate. Do not dismiss behavioral changes because you believe the fix justifies them. The tradeoff is the author's decision — your job is to make it visible.
3. **Be specific and actionable.** Every comment should tell the author exactly what to change and why. Reference the relevant convention. Include evidence of how you verified the issue is real, e.g., "looked at all callers and none of them validate this parameter".
4. **Flag severity clearly:**
   - ❌ **error** — Must fix before merge. Bugs, security issues, API violations, test gaps for behavior changes.
   - ⚠️ **warning** — Should fix. Performance issues, missing validation, inconsistency with established patterns.
   - 💡 **suggestion** — Consider changing. Style improvements, minor readability wins, optional optimizations.
5. **Don't pile on.** If the same issue appears many times, flag it once on the primary file with a note listing all affected files. Do not leave separate comments for each occurrence.
6. **Respect existing style.** When modifying existing files, the file's current style takes precedence over general guidelines.
7. **Don't flag what CI catches.** Do not flag issues that a linter, typechecker, compiler, analyzer, or CI build step would catch, e.g., missing usings, unsupported syntax, formatting. Assume CI will run separately.
8. **Avoid false positives.** Before flagging any issue:
   - **Verify the concern actually applies** given the full context, not just the diff. Open the surrounding code to check. Confirm the issue isn't already handled by a caller, callee, or wrapper layer before claiming something is missing.
   - **Skip theoretical concerns with negligible real-world probability.** "Could happen" is not the same as "will happen."
   - **If you're unsure, either investigate further until you're confident, or surface it explicitly as a low-confidence question rather than a firm claim.** Do not speculate about issues you have no concrete basis for. Every comment should be worth the reader's time.
   - **Trust the author's context.** The author knows their codebase. If a pattern seems odd but is consistent with the repo, assume it's intentional.
   - **Never assert that something "does not exist," "is deprecated," or "is unavailable" based on training data alone.** Your knowledge has a cutoff date. When uncertain, ask rather than assert.
9. **Ensure code suggestions are valid.** Any code you suggest must be syntactically correct and complete. Ensure any suggestion would result in working code.
10. **Label in-scope vs. follow-up.** Distinguish between issues the PR should fix and out-of-scope improvements. Be explicit when a suggestion is a follow-up rather than a blocker.

## Multi-Model Review

When the environment supports launching sub-agents with different models (e.g., the `task` tool with a `model` parameter), run the review in parallel across multiple model families to get diverse perspectives. Different models catch different classes of issues. If the environment does not support this, proceed with a single-model review.

**How to execute (when supported):**
1. Inspect the available model list and select one model from each distinct model family (e.g., one Anthropic Claude, one Google Gemini, one OpenAI GPT). Use at least 2 and at most 4 models. **Model selection rules:**
   - Pick only from models explicitly listed as available in the environment. Do not guess or assume model names.
   - From each family, pick the model with the highest capability tier (prefer "premium" or "standard" over "fast/cheap").
   - Never pick models labeled "mini", "fast", or "cheap" for code review.
   - If multiple standard-tier models exist in the same family (e.g., `gpt-5` and `gpt-5.1`), pick the one with the highest version number.
   - Do not select the same model that is already running the primary review (i.e., your own model). The goal is diverse perspectives from different model families.
2. Launch a sub-agent for each selected model in parallel, giving each the same review prompt: the PR diff, the review rules from this skill, and instructions to produce findings in the severity format defined above.
3. Wait for all agents to complete, then synthesize: deduplicate findings that appear across models, elevate issues flagged by multiple models (higher confidence), and include unique findings from individual models that meet the confidence bar. **Timeout handling:** If a sub-agent has not completed after 10 minutes and you have results from other agents, proceed with the results you have. Do not block the review indefinitely waiting for a single slow model. Note in the output which models contributed.
4. Present a single unified review to the user, noting when an issue was flagged by multiple models.

---

## Review Output Format

When presenting the final review (whether as a PR comment or as output to the user), use the following structure. This ensures consistency across reviews and makes the output easy to scan.

### Structure

```
## 🤖 Copilot Code Review — PR #<number>

### Holistic Assessment

**Motivation**: <1-2 sentences on whether the PR is justified and the problem is real>

**Approach**: <1-2 sentences on whether the fix/change takes the right approach>

**Summary**: <✅ LGTM / ⚠️ Needs Human Review / ⚠️ Needs Changes / ❌ Reject>. <2-3 sentence summary of the overall verdict and key points. If "Needs Human Review," explicitly state which findings you are uncertain about and what a human reviewer should focus on.>

---

### Detailed Findings

#### ✅/⚠️/❌ <Category Name> — <Brief description>

<Explanation with specifics. Reference code, line numbers, interleavings, etc.>

(Repeat for each finding category. Group related findings under a single heading.)
```

### Guidelines

- **Holistic Assessment** comes first and covers Motivation, Approach, and Summary.
- **Detailed Findings** uses emoji-prefixed category headers:
  - ✅ for things that are correct / look good (use to confirm important aspects were verified)
  - ⚠️ for warnings or impactful suggestions (should fix, or follow-up)
  - ❌ for errors (must fix before merge)
  - 💡 for minor suggestions or observations (nice-to-have)
- **Cross-cutting analysis** should be included when relevant: check whether related code (sibling types, callers, other platforms) is affected by the same issue or needs a similar fix.
- **Test quality** should be assessed as its own finding when tests are part of the PR.
- **Summary** gives a clear verdict: LGTM (no blocking issues — use only when confident), Needs Human Review (code may be correct but you have unresolved concerns or uncertainty that require human judgment), Needs Changes (with blocking issues listed), or Reject (explaining why this should be closed outright). **Never give a blanket LGTM when you are unsure.** When in doubt, use "Needs Human Review" and explain what a human should focus on.
- Keep the review concise but thorough. Every claim should be backed by evidence from the code.

### Verdict Consistency Rules

The summary verdict **must** be consistent with the findings in the body. Follow these rules:

1. **The verdict must reflect your most severe finding.** If you have any ⚠️ findings, the verdict cannot be "LGTM." Use "Needs Human Review" or "Needs Changes" instead. Only use "LGTM" when all findings are ✅ or 💡 and you are confident the change is correct and complete.

2. **When uncertain, always escalate to human review.** If you are unsure whether a concern is valid, whether the approach is sufficient, or whether you have enough context to judge, the verdict must be "Needs Human Review" — not LGTM. Your job is to surface concerns for human judgment, not to give approval when uncertain. A false LGTM is far worse than an unnecessary escalation.

3. **Separate code correctness from approach completeness.** A change can be correct code that is an incomplete approach. If you believe the code is right for what it does but the approach is insufficient (e.g., treats symptoms without investigating root cause, silently masks errors that should be diagnosed, fixes one instance but not others), the verdict must reflect the gap — do not let "the code itself looks fine" collapse into LGTM.

4. **Classify each ⚠️ and ❌ finding as merge-blocking or advisory.** Before writing your summary, decide for each finding: "Would I be comfortable if this merged as-is?" If any answer is "no," the verdict must be "Needs Changes." If any answer is "I'm not sure," the verdict must be "Needs Human Review."

5. **Devil's advocate check before finalizing.** Re-read all your ⚠️ findings. For each one, ask: does this represent an unresolved concern about the approach, scope, or risk of masking deeper issues? If so, the verdict must reflect that tension. Do not default to optimism because the diff is small or the code is obviously correct at a syntactic level.

---

## Holistic PR Assessment

Before reviewing individual lines of code, evaluate the PR as a whole. Consider whether the change is justified, whether it takes the right approach, and whether it will be a net positive for the codebase.

### Motivation & Justification

- **Every PR must articulate what problem it solves and why.** Don't accept vague or absent motivation. Ask "What's the rationale?" and block progress until the contributor provides a clear answer.
  > "I am not sure why is this needed. ... It's not immediately obvious whether this happens only for the bridge comparison tests or whether it can happen for real-life scenarios too."

- **Challenge every addition with "Do we need this?"** New code, APIs, abstractions, and flags must justify their existence. If an addition can be avoided without sacrificing correctness or meaningful capability, it should be.
  > "I don't think we should take this change, at all. A change which makes the VS runner see the same assets as the CLI runner, sure. But random extra hacking on the side, no."

- **Demand real-world use cases and customer scenarios.** Hypothetical benefits are insufficient motivation for expanding API surface area or adding features. Require evidence that real users need this.
  > "It is not clear to me whether you can hit a real-world scenario on 32-bit platforms where it makes a difference."

### Evidence & Data

- **Require measurable performance data before accepting optimization PRs.** Demand BenchmarkDotNet results or equivalent proof — never accept performance claims at face value.
  > "Can you please share a benchmark using BenchmarkDotNet against public System.Text.Json APIs that demonstrates the improvement?"

- **Distinguish real performance wins from micro-benchmark noise.** Trivial benchmarks with predictable inputs overstate gains from jump tables, branch elimination, and similar tricks. Require evidence from realistic, varied inputs.
  > "Try to benchmark it with an input that varies randomly. Jump tables are great for trivial micro-benchmarks, but they are less great for real world code."

- **Investigate and explain regressions before merging.** Even if a PR shows a net improvement, regressions in specific scenarios must be understood and explicitly addressed — not hand-waved.
  > "Could you please inspect the regressions on why exactly it's an improvement there?"

### Approach & Alternatives

- **Check whether the PR solves the right problem at the right layer.** Look for whether it addresses root cause or applies a band-aid. Prefer fixing the actual source of an issue over adding workarounds to production code.
  > "The offset behind `Flags.IndexMask` should always be correct. Instead of checking that the index is in range in all its usages, we should fix the root cause where the offset wasn't computed/updated correctly."

- **When a PR takes a fundamentally wrong approach, redirect early.** Don't iterate on implementation details of a flawed design. Push back on the overall direction before the contributor invests more time.
  > "I'm still hesitating whether separating FEATURE_HW_INTRINSICS from SIMD and MASKED_HW_INTRINSICS is the right approach ... An alternative would be to handle them like #113689 and fix the value numbering."

- **Ask "Why not just X?" — always prefer the simplest solution.** When a PR uses a complex approach, challenge it with the simplest alternative that could work. The burden of proof is on the complex solution.
  > "Wouldn't it be simpler to just do a regular mono stackwalk when we need to record and raise a sample?"

### Cost-Benefit & Complexity

- **Explicitly weigh whether the change is a net positive.** A performance trade-off that shifts costs around is not automatically beneficial. Demand clarity that the change is a win in the typical configuration, not just in a narrow scenario.
  > "It is a performance trade-off. You will shift the costs around. It is not clear to me whether it would be a win at the end in the typical configuration."

- **Reject overengineering — complexity is a first-class cost.** Unnecessary abstraction, extra indirections, and elaborate solutions for marginal gains are actively rejected.
  > "This optimization smells funny. It seems overly complicated for little win. Is this path hot? Can we instead store the home directory?"

- **Every addition creates a maintenance obligation.** Long-term maintenance cost outweighs short-term convenience. Code that is hard to maintain, increases surface area, or creates technical debt needs stronger justification.
  > "The primary goal of this project is to minimize our long-term maintenance costs. Building multiple optimizing code generators would go against that goal."

### Scope & Focus

- **Require large or mixed PRs to be split into focused changes.** Each PR should address one concern. Mixed concerns make review harder and increase regression risk.
  > "I think I'm going to break this into two pieces, even though that's more work."

- **Defer tangential improvements to follow-up PRs.** Police scope creep by asking contributors to separate concerns. Even good ideas should wait if they're not part of the PR's core purpose.
  > "Should probably be a separate PR."

### Risk & Compatibility

- **Flag breaking changes and require formal process.** Any behavioral change that could affect downstream consumers needs documentation, API review, and explicit approval — even when the change improves the codebase internally.
  > "Introduce the new API in this PR. Remove the old check in another PR, mark it as breaking change and document it (as any other breaking change)."

- **Assess regression risk proportional to the change's blast radius.** High-risk changes to stable code need proportionally higher value and more thorough validation.
  > "I wanted to backport this change to .NET 10, potentially .NET 9, and wouldn't want to introduce any risky changes."

### Codebase Fit & History

- **Ensure new code matches existing patterns and conventions.** Deviations from established patterns create confusion and inconsistency. If a rename or restructuring is warranted, do it uniformly in a dedicated PR — not piecemeal.
  > "This change is inconsistent with the rest of the global pointers. If we want to consider renaming these, I think we should do it in a separate PR and apply it consistently to all global pointers."

- **Check whether a similar approach has been tried and rejected before.** If a prior attempt didn't work, require a clear explanation of what's different this time.
  > "If it's not worthwhile, especially if it was previously tried and it wasn't obviously beneficial, then we should close the issue."

---

## Correctness & Safety

### Error Handling & Assertions

- **Use `Debug.Assert` for internal invariants, not exceptions.** For internal-only callers, assert assumptions rather than throwing `ArgumentException`. Prefer `Debug.Assert(value != null)` over the null-forgiving operator (`!`).
  > "Since there are no public callers, this should be an assert, not an ArgumentException." — bartonjs

- **Use `throw` for reachable error paths, `UnreachableException` for exhaustive switches.** When a code path might be hit at runtime, throw an exception rather than asserting. Use `throw new UnreachableException()` for default cases in exhaustive switches. Use `PlatformNotSupportedException` (not `NotSupportedException`) for platform gaps. In native code, use `_ASSERTE(!"message")`.
  > "We prefer throw rather than asserts so it is more apparent if some scenario makes it here." — VSadov

- **Include actionable details in exception messages.** Use `nameof` for parameter names. Include the unsupported type or unexpected value. Never throw empty exceptions.
  > "You should add some message here: `throw new ArgumentException($\"Unknown ArrayFunctionType: {functionType}.\", nameof(method));`" — jkoritzinsky

- **Initialize output parameters in all code paths.** When a method has `out` parameters or pointer outputs (`bytesWritten`, `numLocals`), ensure they are initialized to a defined value in all error paths.
  > "Clear numLocals here (or at start of the method) so that it is initialized in all error cases?" — jkotas

- **Handle OOM with exceptions or fail-fast, never asserts.** Use `ThrowOutOfMemory` or `EEPOLICY_HANDLE_FATAL_ERROR`, not asserts. In interpreter loops, use `nothrow new` and check for null.
  > "OutOfMemory handling should not be done by asserts. It should throw exception; or if exception is not viable, fail with fail fast." — jkotas

- **Use `ThrowIf` helpers over manual checks.** Use `ArgumentOutOfRangeException.ThrowIfNegative`, `ObjectDisposedException.ThrowIf`, etc. instead of manual if-then-throw patterns.
  > "This if condition should not be necessary, as it's going to be checked by ThrowIfNegative." — stephentoub

- **Challenge exception swallowing that masks unexpected errors.** When a PR adds try/catch blocks that silently discard exceptions (`catch { continue; }`, `catch { return null; }`), question whether the exception represents a truly expected, recoverable condition or an unexpected error signaling a deeper problem (race conditions, memory corruption, build environment issues). Silently catching exceptions that "shouldn't happen" hides root causes and makes debugging harder. The default disposition should be to let unexpected exceptions propagate or fail fast so the real issue gets investigated.
  > "Why do we want to mask this error? ... Our general strategy in AOT compilers is to fail the compilation when the input is malformed. It is expected that the malformed input can and will cause the compiler to crash or fail." — jkotas

### Thread Safety

- **Use `Volatile` or `Interlocked` for cross-thread field access.** Fields written on one thread and read on another must use `Volatile<T>`, `Volatile.Read/Write`, or `Interlocked`. The `??=` operator is not thread-safe. `Nullable<T>` is not safe for caching (two-field struct tears). Do not use shared mutable arrays without synchronization.
  > "field ??= is not thread-safe." — jkotas; "Nullable\<int\> is a struct with two fields. This pattern has a race condition caused by tearing." — jkotas

- **Use `TickCount64` for timeout calculations.** Use `Environment.TickCount64` (long) instead of `Environment.TickCount` (int) to avoid integer overflow.
  > "Should this use long and `Environment.TickCount64` to avoid integer overflow issues?" — jkotas

### Security

- **Guard integer arithmetic against overflow.** Guard size computations involving multiplication (e.g., `newCapacity * sizeof(T)`) against integer overflow. Use patterns correct by construction.
  > "We have a lot of scars from integer overflow security bugs... This change is switching from code that follows best practices to a potentially vulnerable pattern." — jkotas

- **Clean sensitive cryptographic data after use.** Always clear key material with `CryptographicOperations.ZeroMemory`. When using `PinAndClear` but copying to another buffer, clear the original too. Use non-short-circuit operators (`|`) in verification code to prevent timing leaks.
  > "This is using PinAndClear to avoid the GC making a copy... but it's itself making a copy and not clearing the original." — bartonjs

- **Don't proactively send credentials without opt-in.** Never send authentication credentials (especially Basic auth) before receiving a challenge.
  > "This is problematic and we will have difficulty with security group as it especially with basic AUTH leaks the credentials." — wfurt

- **Limit `stackalloc` to ~1KB and validate size.** Don't stackalloc based on user-controlled or large input sizes. Move stackalloc to just before usage, not before early returns.
  > "We typically limit stackallocs to ~1K." — stephentoub

### Correctness Patterns

- **Fix root cause, not symptoms or workarounds.** Investigate and fix the root cause rather than adding workarounds or suppressing warnings. Revert broken commits before layering fixes.
  > "Let's try to investigate the root cause instead of taking this fix as-is since there could be other issues/AVs related to the mangling of the list." — jkotas

- **Prefer safe code over unsafe micro-optimizations.** Do not introduce `Unsafe.As`, `Unsafe.AsRef`, or raw pointers without demonstrable performance need. Prefer Span-based APIs. If performance is the issue, prefer fixing the JIT.
  > "I do not want to introduce unsafe code for things like this. If it's material, the cast should be elided by the JIT." — stephentoub

- **Use `Unsafe.BitCast` for same-size type punning.** Prefer `Unsafe.BitCast<TFrom, TTo>` over `Unsafe.As<TFrom, TTo>` for type punning between value types of the same size.
  > "Unsafe.BitCast is more correct (avoids undeclared misaligned access) and less dangerous than Unsafe.As here." — jkotas

- **Delete dead code and unnecessary wrappers.** Remove dead code, unnecessary wrappers, obsolete fields, and unused variables when encountered or when the only caller changes.
  > "Unnecessary wrapper", "Dead code that I happen to notice", "This is the only use of m_canBeRuntimeImpl. It can be deleted." — jkotas

- **Handle `SafeHandle.IsInvalid` before `Dispose`.** Check `IsInvalid` (not null) on returned SafeHandles. Get the exception before calling `Dispose`, since Dispose might clear the error state.
  > "`if (handle.IsInvalid) { Exception ex = Interop.Crypto.CreateOpenSslCryptographicException(); handle.Dispose(); throw ex; }`" — vcsjones

- **Seal classes when `Equals` uses exact type matching.** If a class implements `Equals` with `GetType()` comparison, seal the class to prevent subtle inheritance bugs.
  > "Is there a reason why this Equals implementation is exact type match only even though ContextHolder isn't sealed? I'd prefer to block this class of failure by sealing the class." — kg

- **Use `Environment.ProcessPath` and `AppContext.BaseDirectory`.** Use these instead of `Process.GetCurrentProcess().MainModule?.FileName` and `Assembly.Location` for NativeAOT/single-file compatibility.
  > "Process.GetCurrentProcess().MainModule?.FileName should be same as Environment.ProcessPath." — jkotas

- **File name casing must match csproj references exactly.** Linux is case-sensitive. New source files must be listed in the `.csproj` if other files in that folder are explicitly listed.
  > "The build is failing because Linux is a case-sensitive file system and your csproj file and the file name differ by case." — vcsjones

- **Prefer correct-by-construction designs.** Prefer designs that are correct by construction (e.g., scanning IL) over manually maintained parallel data structures. A missed optimization is better than silent bad codegen.
  > "I'd go with the correct-by-construction approach if it's an option." — MichalStrehovsky

- **Allocate on the correct loader allocator for collectibility.** When allocating runtime data structures for generic instantiations, use the correct loader allocator accounting for collectibility of type arguments.
  > "Consider MethodInNonCollectibleAssembly\<CollectibleType\>(): This method instantiation should be allocated on collectible loader allocator. As written, it will have a memory leak." — jkotas

- **Backport targeted fixes, not refactorings.** When backporting to servicing branches, create small targeted fixes. Backporting large refactorings introduces unnecessary risk.
  > "If we need the fix in .NET 10 for Android, we should do a small targeted fix that just adds the few lines under Android ifdef." — jkotas

### JIT-Specific Correctness

- **JIT lowering must not double-lower nodes.** Never call `LowerNode` on an already-lowered node. Return newly created nodes for the caller to lower. Constant folding belongs in import/morph, not lowering.
  > "Lower is not supposed to be called twice on the same node generally." — EgorBo

- **Mark collectible ALC test methods `NoInlining`.** Methods that touch collectible assembly load contexts must be `[MethodImpl(MethodImplOptions.NoInlining)]` to prevent the JIT from keeping references alive.
  > "This needs to be marked as no-inlining too. It is valid for JIT to inline this method and leave a reference to collectible assembly load context in a local." — jkotas

---

## Performance & Allocations

### Measurement & Evidence

- **Performance changes require benchmark evidence.** Include BenchmarkDotNet or EgorBot numbers before merging. Validate with real-world scenarios, not just microbenchmarks.
  > "Performance related changes without numbers have high probability of being performance regressions in practice." — jkotas

- **Justify binary size increases with real-world measurements.** Changes that increase binary size require measured wall-clock improvements on real-world apps, not just instruction counts.
  > "I would like to see number for a Blazor app (total size / bytes saved by this change)." — jkotas

- **Avoid premature optimization with object pools and caches.** Do not introduce global caches or object pools without evidence they are needed. Prefer making the underlying operation faster.
  > "This pool looks like a premature optimization." — jkotas

### Allocation Avoidance

- **Avoid closures and allocations in hot paths.** When a lambda captures locals creating a closure, consider using a static delegate with a state parameter (value tuple). Avoid string concatenation; use span-based operations.
  > "Since this is capturing `data` and `context` in the closure it's allocating a closure for every call. The usual fix is to make the callback take a TState, and just pass through a value-tuple." — bartonjs

- **Pre-allocate collections when size is known.** Pass capacity to `Dictionary`, `HashSet`, `List` constructors when the expected count is available.
  > "We can pre-allocate the dictionary of the right size." — jkotas

- **Structs in dictionaries need `IEquatable<T>` and `GetHashCode`.** Without these, the runtime falls back to boxing allocations for equality comparison.
  > "If a struct doesn't override equality comparison logic, the runtime will end up using a fallback that boxes the value." — MihaZupan

- **Avoid Pinned Object Heap for non-permanent objects.** POH is never compacted and effectively gen2. Only use for objects surviving as long as the process.
  > "We avoid the POH for objects that may have a shorter lifespan, typically only using it for objects that will survive as long as the process does." — stephentoub

- **Suppress `ExecutionContext` flow for infrastructure timers.** When allocating `Timer` or similar background infrastructure, suppress EC flow to avoid capturing unrelated `AsyncLocal`s that leak memory.
  > "We'll want to suppress the ExecutionContext during the timer allocation to avoid capturing unrelated asynclocals." — MihaZupan

### Code Structure for Performance

- **Place cheap checks before expensive operations.** Order conditionals so cheapest/most-common checks come first. Move expensive work after early-exit checks.
  > "These checks are cached cheap bit tests. We should do them first, and run the more expensive IL header decoder only when the modes do not match." — jkotas

- **Allocate resources lazily where possible.** Allocate expensive resources on first use, not during initialization. Avoid forcing type initialization during startup.
  > "We try to do things lazily where possible since it is good for performance." — jkotas; "Do not force initialization during runtime startup just to make cDAC work. Startup is our number one perf problem." — jkotas

- **Extract throw helpers into `[DoesNotReturn]` methods.** Move throwing logic from error paths into separate static local functions or helper methods to allow the JIT to inline the success path.
  > "Please move the body of this `if (throwOnFailure)` block to a separate [DoesNotReturn] throwing static local function." — stephentoub

- **Avoid O(n²) patterns in collections and hot paths.** Watch for linear scans inside loops, repeated `RemoveAt` in loops. Use `RemoveAll`, single-pass restructuring, or appropriate data structures.
  > "Since RemoveParsedValue performs a linear scan, this change makes the setter have a quadratic worst-case complexity." — MihaZupan

- **Cache repeated accessor calls in locals.** Store the result of repeated property/getter calls in a local variable.
  > "Maybe read it out once into local to reduce number of calls to m_type_data_get_type?" — lateralusX

- **Separate hot data from rarely-used data in runtime structures.** Keep frequently accessed data inline; move rarely-used data (GCInfo, DebugInfo) to separate structures.
  > "The code header structure is intentionally designed to separate hot and rarely used data." — jkotas

- **Compute constant data at compile time, not execution time.** In interpreter and similar hot paths, pre-compute metadata lookups and type checks during the compilation phase.
  > "This computation is constant and should be done at compile time." — BrzVlad

- **Consider scalability, not just throughput.** Evaluate whether data structures, caches, and locking strategies will hold up at high cardinality or under concurrent load. Watch for unbounded collection growth, lock contention that worsens with core count, and O(1) assumptions that break at scale.

### Specific API Choices

- **Use `AppContext.TryGetSwitch` with a static readonly property.** Cache AppContext switches in `static bool Prop { get; } = AppContext.TryGetSwitch(...)` so the JIT can dead-code-eliminate unreachable paths.
  > "`private static bool SwitchEnabled { get; } = AppContext.TryGetSwitch(..., out bool enabled) && enabled;` This makes it readonly and lets the JIT delete the unreachable code paths." — MihaZupan

- **Do not cache `typeof` expressions in .NET Core.** `typeof(...)` is JITed into a constant; caching it is a de-optimization. Similarly, don't store `ArrayPool.Shared` in variables—it breaks devirtualization.
  > "Caching typeof(...) is de-optimization in .NET Core. typeof(...) is JITed into a constant." — jkotas

- **Use `CollectionsMarshal` for large value-type dictionary lookups.** Use `GetValueRefOrAddDefault` or `GetValueRefOrNullRef` to avoid copying large structs. Use `ValueListBuilder` on hot paths.
  > "You can use CollectionsMarshal.GetValueRefOrAddDefault here... Avoids copy of the large EventMetadata struct." — jkotas

- **Use `sizeof` instead of `Marshal.SizeOf` for blittable structs.** `sizeof` is more correct and significantly faster when no marshalling is involved.
  > "It is more correct and a lot faster to use sizeof instead of Marshal.SizeOf." — jkotas

- **Use the idiomatic `(uint)index >= (uint)length` bounds check.** The JIT recognizes this pattern and optimizes it. Slice spans before iterating to avoid per-element bounds checks.
  > "The JIT recognizes the idiomatic pattern and optimizes it down where it is safe already." — tannergooding

- **Source generators must be properly incremental.** Do not store Roslyn symbols (`ISymbol`, `Compilation`) in incremental pipeline steps. Output must be deterministic with Ordinal-sorted lists.
  > "Make your generators properly incremental or don't ship them at all, because the alternative is that you'll murder the IDE." — Sergio0694

- **Avoid LINQ and records in low-level compiler codebases.** In CG2/ILC and AOT tools, use direct loops instead of LINQ and readonly structs instead of records. Use concrete types over interfaces in private code.
  > "I'd avoid `using System.Linq` to steer clear of perf traps." — MichalStrehovsky

- **Use `ValueListBuilder` for dynamic array building in BCL.** Use `ValueListBuilder<T>` (with pooling) or `ArrayBuilder<T>`. Use stackalloc for small sizes, array pool when too large.
  > "ValueListBuilder is the centralized type for building arrays in BCL." — huoyaoyuan

---

## API Design & Contracts

- **New public APIs require approved proposals before PR submission.** All new API surface must go through API review. PRs adding unapproved APIs will be closed. The implementation must match exactly what was approved.
  > "We do not accept PRs for unapproved APIs." — jkotas

- **Use `internal` for new APIs pending API review.** If the API is needed immediately for implementation, mark it `internal` and file a review request separately.
  > "This needs to go through API Review for it to be public. You should use internal for now." — jozkee

- **Parameter names must match between ref and src.** Renaming a public API parameter (including case changes) is a breaking change affecting named arguments and late-bound scenarios.
  > "The approved API calls the second parameter value, not result. The mismatch is also why the build failed." — vcsjones

- **Align exception types and validation order across platforms.** Validate arguments first (`ArgumentNullException`, then `ArgumentException`), then `PNSE`, then `ObjectDisposedException`, then perform the operation. Throw the same exception types on all platforms.
  > "My exception order is: 1. ArgumentExceptions (null first, then logical) 2. PNSE 3. ObjectDisposedException 4. 'Do the thing' exceptions." — vcsjones

- **`Try` APIs should return `false` only for the common expected failure.** Throw for everything else (corruption, permissions, invalid arguments). Try methods must always throw on invalid arguments.
  > "The usual contract for Try... API is to return false for the specific (most common) reason only and throw for everything else." — jkotas

- **Don't expose mutable options after construction.** If values are captured at construction time, don't expose a mutable options object. Don't reference private field names or internal types in user-facing error messages.
  > "Exposing a mutable ZLibCompressionOptions object after construction could be misleading." — iremyux

- **Use `PlatformNotSupportedException` for platform limitations.** When an operation can't complete in the current environment but could on a different platform, throw PNSE. Don't impose artificial limits beyond OS capabilities.
  > "Our general policy for System.IO is to surface underlying system limitation without imposing additional artificial restrictions." — jkotas

- **.NET APIs should compensate for platform quirks.** Public APIs should work consistently across platforms. When adding overloads, check F# compatibility for implicit conversion ambiguities.
  > "The added value of .NET as a platform is that we compensate for quirks of the underlying platforms and we make things just work." — jkotas

- **Follow the obsoletion process for deprecated APIs.** Pick the next available SYSLIB diagnostic ID, add `[Obsolete]`, and use `[EditorBrowsable(Never)]` with `[OverloadResolutionPriority(-1)]` for overload fixes.
  > "We have an obsoletion process in place which involves picking the next available SYSLIB id." — eiriktsarpalis

- **New GC-EE interface methods must be appended last.** Always add new methods as the last method on the interface to preserve vtable slot ordering.
  > "This needs to be the last method on the interface to avoid changing vtable slots for existing methods." — jkotas

- **New virtual methods must work with unoverridden derived types.** The default implementation must behave identically to calling the pre-existing equivalent APIs.
  > "We have to expect that the new method will be used with derived writers that won't yet have overridden it." — stephentoub

- **Avoid unsigned types for lengths in public APIs.** Prefer `int` or `long` for length parameters. Use named types instead of `ValueTuple` across file boundaries.
  > "We generally don't use unsigned types for lengths in public APIs." — rzikm

- **Start core component changes with an issue.** Changes to host, VM, or JIT should start with a GitHub issue describing the problem and motivation before submitting a PR.
  > "This sort of change really should have started in an issue as it involves changing a core component and there is a lot to consider." — AaronRobinsonMSFT

---

## Code Style & Formatting

- **Use well-named constants instead of magic numbers.** No raw hex or decimal constants without explanation. Don't duplicate magic constants across files.
  > "0x7F00 says nothing to the typical reader. Adding a comment that explains it means `(float)Int128.MaxValue`... then means a lot." — tannergooding

- **Use `var` only when the type is obvious from context.** Use explicit types for casts, method returns, and async infrastructure. Never use `var` for numeric types.
  > "These seem like unacceptable uses of var." — bartonjs

- **Use PascalCase for constants; descriptive names for booleans.** All constant locals and fields use PascalCase (except interop constants matching external names). Boolean fields should be positive and descriptive (`_hasCurrent` not `valid`).
  > "We use PascalCasing to name all our constant local variables and fields." — bartonjs

- **Name methods to accurately reflect their behavior.** Update names when behavior changes. `Get*` implies a return value; use `Print*/Display*` for void. `ThrowIf` not `ThrowExceptionIf`.
  > "This method is not returning anything after this change, so Get... name does not fit." — jkotas

- **Prefer early return to reduce nesting.** Use early returns for short/error cases to avoid unnecessary nesting. Put the error case first, success return last.
  > "Rather than having the extra layer of indentation for the else block, could we do it as: `if (...) { return ...; }`" — stephentoub

- **Avoid `using static` and `#region` in new code.** `using static` is costly when reading code outside IDEs (e.g., GitHub review). `#region` gets out of date quickly.
  > "`using static` adds an astronomical cost when an IDE isn't available." — bartonjs

- **Place local functions at method end, fields first in types.** Local functions go at the end of the containing method. Fields are the first members declared in a type.
  > "The generally agreed upon pattern has been that local functions are placed at the end of the method." — AaronRobinsonMSFT

- **Narrow warning suppression to smallest scope.** Avoid file-wide `#pragma` suppressions. Disable only around the specific line that triggers the warning.
  > "Suppressing warnings broadly is generally bad practice." — AaronRobinsonMSFT

- **Use pattern matching and `is`/`or`/`and` patterns.** Prefer `is` patterns and C# pattern matching over manual type checks and comparisons. Use named parameters for boolean arguments.
  > "`return !(typeDesc.Category is TypeFlags.Boolean or TypeFlags.Char);`" — jkotas

- **Do not initialize fields to default values (CA1805).** The CLR zero-initializes fields. Explicit `= false`, `= 0`, `= null` is redundant.
  > "CA1805: Do not initialize unnecessarily." — MichalStrehovsky

- **Sealed classes do not need the full Dispose pattern.** A simple `Dispose()` is sufficient since no derived class can introduce a finalizer.
  > "Given that the class is marked sealed now, I personally don't think the full dispose pattern is needed." — Youssef1313

- **Prefer table-driven approaches over excessive case statements.** For hardware intrinsics and pattern-heavy code, use lookup tables (`AuxiliaryJitType`, `SpecialCodeGen` flags) instead of many explicit case entries.
  > "I think it'd be better to use the AuxiliaryJitType and mark these as SpecialCodeGen than to add a bunch of extra table entries." — tannergooding

- **Order struct fields to minimize padding.** In C/C++ struct definitions, order fields by size (pointers first) to reduce padding.
  > "Maybe put pointers first to prevent padding." — lateralusX

---

## Consistency with Codebase Patterns

### PR Hygiene

- **Keep PRs focused on their stated scope.** No accidental file modifications, no unrelated refactoring, no whitespace noise, no build artifacts. Each PR should serve a single purpose.
  > "Please be more deliberate about your pull requests... it makes for a very muddy source history." — bartonjs

- **Do large refactorings and renames in separate PRs.** Separate no-diff refactors from functional changes. Mechanical renames should be separate from logic changes.
  > "I always prefer to do the no-diff refactors first and build the diff changes on top." — AndyAyersMS

- **Merge to main first, then backport to release branches.** Use the `/backport` command. Backports to servicing are limited to security bugs, regressions, and reliability issues.
  > "In general, performance related fixes do not meet the bar, unless they are fixing significant regression." — jkotas

### Code Reuse & Deduplication

- **Extract duplicated logic into shared helper methods.** Fix improvements inside shared helpers so all callers benefit.
  > "Would it be better to move this to a helper method instead of duplicating it?" — tarekgh

- **Move shared code to shared files, not duplicated across runtimes.** When identical code exists across CoreCLR and NativeAOT, move it to the shared partition (using `#if !MONO` if needed).
  > "There is quite a bit of identical code between NativeAOT and CoreCLR. Can we move it into the shared file?" — jkotas

- **Use existing APIs instead of creating parallel ones.** Before introducing new types, enums, or helpers, check if existing ones serve the same purpose. Fix existing utilities rather than introducing duplicates.
  > "Can you use the existing SignatureAttributes.Instance instead? It means the same thing." — jkotas

- **Delete dead code and unused declarations aggressively.** When removing code, also remove helper methods, enum values, function declarations, and resx strings that are no longer used.
  > "This function isn't used. Please delete." — davidwrighton

### Established Conventions

- **Store error strings in `.resx`, not inline code.** Reference via the `SR` class. When removing code that uses a resx string, delete the unused string entry.
  > "We do not store string message in code. Instead, they should be stored in .resx, with optional format argument and referenced with SR." — huoyaoyuan

- **Sort lists and entries alphabetically.** Lists of areas, configuration entries, resx entries, entrypoint/export lists, and ref source members should be maintained in alphabetical order.
  > "The list of areas looks sorted in alphabetical order." — jkotas

- **Don't modify auto-generated files or `eng/common` manually.** Change the generator or source definition instead. Files in `eng/common` are synced from dotnet/arcade.
  > "Things in eng/common come from the dotnet/arcade repository. Your fixes here will be undone the next time arcade is synced." — vcsjones

- **Use `DOTNET_` prefix for environment variables, not `COMPlus_`.** New runtime environment variables must use `DOTNET_` exclusively.
  > "The COMPlus names are legacy that we'd like to phase out. I don't think we should add support for them in new features." — agocke

- **Match existing style in modified files.** The existing style in a file takes precedence over general guidelines. Do not change existing code for style alone.
  > "If a file happens to differ in style from these guidelines, the existing style in that file takes precedence." — huoyaoyuan

- **Prefer `sizeof` over `Unsafe.SizeOf` consistently.** A pass was done to replace all `Unsafe.SizeOf` uses. Do not reintroduce them.
  > "We have done a pass to delete all Unsafe.SizeOf uses and replaced them with sizeof." — jkotas

### Runtime-Specific Patterns

- **Consider NativeAOT parity for runtime changes.** When changing CoreCLR behavior, verify whether the same change is needed for NativeAOT.
  > "The code you have changed is not used on NativeAOT. Do we need the same change for NativeAOT as well?" — jkotas

- **Keep interpreter behavior consistent with the regular JIT.** Follow the same patterns, naming, error codes (`CORJIT_BADCODE`), and macros (`NO_WAY`). Use `FEATURE_INTERPRETER` guards.
  > "Should we call it NO_WAY like in a regular JIT? I think the more similar the interpreter JIT to the regular JIT, the better." — jkotas

- **Source generators: no file locks, diagnostics from analyzers only.** Generators should bypass invalid state gracefully. A separate analyzer should produce diagnostics.
  > "A generator should never lock files on disk." — jkoritzinsky

- **Ref assembly conventions.** No `using` directives (fully qualify types), empty method bodies or `throw null`, genapi-style formatting, alphabetical member order. TFM-specific APIs go in separate files.
  > "Generally the ref source does not have usings." — vcsjones

---

## Testing

- **Always add regression tests for bug fixes and behavior changes.** Prefer adding `[InlineData]` test cases to existing test files rather than creating new ones. Ensure new test files are included in the csproj.
  > "The PR needs a regression test added. TypeInfoTests.cs is a good place to add it (add new InlineData)." — jkotas

- **Use platform-specific test attributes correctly.** Use `[PlatformSpecific]`, `[ConditionalFact]`, or `[ActiveIssue]` for skip logic rather than runtime if-checks. `ConditionalFact` is required for `SkipTestException` to work.
  > "This needs to be conditional fact for throw new SkipTestException inside the test." — jkotas

- **Test edge cases, error paths, and all affected types.** Include empty strings, negative values, boundary conditions, Turkish 'i', surrogate pairs. Test both true and false for boolean options. Choose inputs that can't accidentally pass if output wasn't touched.
  > "Pick an input that doesn't decode to all 0s so that the test can't pass even if the output wasn't touched at all." — MihaZupan

- **Test assertions must be specific.** Assert exact expected values (exact `OperationStatus`, exact byte counts), not broad conditions. Ensure tests actually fail when the fix is reverted.
  > "The current asserts are too broad to be useful." — MihaZupan

- **Delete flaky and low-value tests rather than patching them.** Do not add tests known to be flaky. If a test relies on fragile runtime details and cannot be made reliable, prefer deletion.
  > "It would be better to delete the test. No point in adding flaky tests." — jkotas

- **Make test data deterministic and culture-independent.** Create `CultureInfo` with explicit format settings. Use `[Theory]` with `[InlineData]` over individual `[Fact]` methods.
  > "I would suggest in the test you create a culture like `var culture = new CultureInfo(\"de-DE\"); culture.DateTimeFormat.AbbreviatedMonthGenitiveNames = [...]`" — tarekgh

- **Use `PLACEHOLDER` for test passwords.** Avoids false positives from credential scanning tools.
  > "Sometimes credscan gets tetchy about tests when we don't want it to. Their recommendation is to use the value PLACEHOLDER." — bartonjs

- **Use checked builds for CI, lower priority for regression tests.** Use checked (not debug) CoreCLR builds for CI. New JIT regression tests should typically be `CLRTestPriority 1`.
  > "Debug build of CoreCLR is very slow. We typically use checked build for testing." — jkotas

- **Use `RemoteExecutor` for tests with process-wide shared state.** Tests that modify shared state should use `RemoteExecutor` for isolation. Avoid hardcoded paths; use temp files. Do not add heavy dependencies like `Microsoft.CodeAnalysis.CSharp` to test assemblies.
  > "Avoid dependency on Microsoft.CodeAnalysis.CSharp in these tests. It would prevent this whole test assembly from being able to run on devices, wasm and nativeaot." — jkotas

- **Catch only expected exceptions in fuzz tests.** Catching all exceptions masks bugs like undocumented exceptions escaping the API.
  > "Would it be possible to catch only the exceptions that we expect here?... It has helped me to find that the library was throwing undocumented exceptions." — adamsitnik

- **Use modern xUnit patterns for xUnit-based tests.** In xUnit test projects (for example, most libraries tests), use `Assert.*` instead of the legacy `return 100 == success` pattern, use `[Fact]`/`[Theory]`, prefer `ThrowsAnyAsync<OperationCanceledException>` for cancellation, and name regression test classes after the issue number (e.g., `Runtime_117605`). Legacy non-xUnit tests under `src/tests` may continue to use the existing `return 100` convention.
  > "Can we change the tests here to use Asserts and not the legacy 'return 100 == success' model?" — jkoritzinsky

- **Reduce test output volume.** Avoid megabytes of console output. Use `Thread.Sleep` with fewer iterations instead of busy loops.
  > "This will produce megabytes of output. Can we do something less visible, like Thread.Sleep(10) and change the for-loop to go till like 200?" — jkotas

- **Follow naming conventions for regression test directories.** In `src/tests/Regressions/coreclr/`, use `GitHub_<issue_number>` for the directory and `test<issue_number>` for the test name.
  > "Please follow the pre-existing pattern for naming. The directory name should be GitHub_122933 and the test name should be test122933." — jkotas

---

## Documentation & Comments

- **Comments should explain why, not restate code.** Delete comments like `// Get the types` that just duplicate the code in English. Don't include historical context about why code changed.
  > "Comments that just duplicate the code in plain English are not very useful. This comment should explain why we are doing this." — jkotas

- **Delete or update obsolete comments when code changes.** Stale comments describing old behavior are worse than no comments.
  > "The whole comment starting with `Note:` can be deleted. It is no longer applicable." — jkotas

- **Track deferred work with GitHub issues and searchable TODOs.** Reference a tracking issue in TODO comments with a consistent prefix (e.g., `TODO-Async:`). Remove ancient TODOs that will never be addressed.
  > "Could you please tag all these places that need review with async TODO so that they can be found easily and none of them falls through the cracks?" — jkotas

- **Don't duplicate comments on interface implementations.** Documentation comments belong on the interface definition. Duplicating leads to divergence.
  > "It is enough to have these comments on the interface. Duplicating them is just going to lead to the comments diverging over time." — jkotas

- **Add XML doc comments on all new public APIs.** These seed the official API documentation on learn.microsoft.com. Properties should start with "Gets the ..." or "Gets or sets the ...". Do not add XML docs to test code.
  > "Please also include /// comments on all the new APIs (they seed the api docs)." — MihaZupan

- **Use SHA-specific or commit-based links in documentation.** Don't use branch-relative links that break when files move.
  > "Best to use sha-specific links." — richlander

- **Reference ECMA-335 and spec sources in metadata code.** When parsing signatures and metadata, cite the relevant ECMA-335 section. Cite CAVP/ACVP sources in crypto test vectors.
  > "Perhaps reference the signature format from ECMA-335 we are following here." — AaronRobinsonMSFT

- **File breaking change documentation for behavioral changes.** Open an issue in dotnet/docs using the template, send notification to the .NET Breaking Change Notification DL. Applies even to prerelease-to-prerelease changes.
  > "We just need to open an issue describing the break using the breaking change template." — tannergooding

- **Use established terminology in user-facing text.** Do not expose internal type names, private field names, or codenames like "Roslyn" in public docs or error messages.
  > "'non-explicit type' is not an established term." — jkotas; "Roslyn is our internal codename. It should not be used in public docs." — jkotas

- **Retain copyright headers and license information.** All C# and C++ source files must include the standard license header, including test files. When porting from other projects, retain original copyright and update THIRD-PARTY-NOTICES.TXT.
  > "All C# and C++ source files should have license header (incl tests)." — jkotas

---

## Platform & Cross-Platform

- **Use `BinaryPrimitives` for endianness-safe reads.** Use `ReadInt32LittleEndian`/`BigEndian` rather than pointer casts. Separate endianness-specific reads from target-endianness reads.
  > "If we're reading this as a 64-bit value... On big endian the result is OverflowException." — tmds

- **Use cross-platform vector APIs over ISA-specific intrinsics.** Prefer `Vector128/256/512.IsHardwareAccelerated` and cross-platform APIs (`.Shuffle`, `.Min`) over `Avx512BW`, `SSE2`. Use `BitOperations` for portable bit manipulation.
  > "Would you want to update this to use the xplat APIs instead? Swap Avx512BW.IsSupported -> Vector512.IsHardwareAccelerated." — tannergooding

- **Use correct platform/feature defines.** Use `TARGET_*`/`HOST_*` defines rather than compiler-provided defines (`__wasm__`). Use `HOST_*` for build machine code, `TARGET_*` for target platform. Use `PORTABILITY_ASSERT` for unimplemented platform code.
  > "The combinations of styles e.g. HOST_WINDOWS and __wasm__ next to each other does not look good." — jkotas

---

## Native Code & Interop

### C++ Style

- **Don't use `auto` in the runtime C++ codebase.** Use explicit types. Exception: unspeakable types like lambdas.
  > "We don't use auto in the runtime code base." — AaronRobinsonMSFT

- **Use `nullptr`, `void*`, and native C++ types over legacy aliases.** Prefer `nullptr` over `NULL`, `void*` over `LPVOID`. Use `WCHAR` (not `wchar_t`) in Windows host code. Use `.inc` suffix for multiply-included files.
  > "We prefer `nullptr` in large chunks of new code." — jkotas; "LPVOID is Windows SDK alias for void* with legacy baggage." — jkotas

- **Match `#endif` comments to `#ifdef` exactly.** Add comments on `#else`/`#endif` for non-trivial blocks. Consistent brace placement and four-space indentation.
  > "The common style in CoreCLR is to match the `#ifdef` exactly even if there is `#else`." — jkotas

- **Prefer `static_cast` over C-style casts.** C-style casts are more permissive than needed and can silently degrade to `reinterpret_cast`.
  > "`static_cast<>` is about enforcing the narrowest contract we can afford... a C style cast is a lurking `reinterpret_cast<>`." — AaronRobinsonMSFT

### Runtime & VM Patterns

- **Use correct VM contracts and QCall patterns.** QCalls that may throw need `BEGIN_QCALL`/`END_QCALL`. Simple QCalls use `QCALL_CONTRACT_NO_GC_TRANSITION`. All VM methods need `STANDARD_VM_CONTRACT` or `WRAPPER_NO_CONTRACT`.
  > "QCalls need BEGIN_QCALL and END_QCALL if exceptions will be thrown." — AaronRobinsonMSFT

- **Keep GC protection correct around managed references.** Ensure all GC references are `GCPROTECT`-ed before GC-triggering calls. After GC-triggering calls, use `ObjectFromHandle(handle)` for a fresh reference.
  > "MethodDescCallSite is GC triggers, so you need to protect all GC references when it is called." — jkotas

- **Avoid dynamic allocation on fatal error paths.** Use stack-allocated buffers. Use simple synchronization (Interlocked with spin-wait) instead of Monitor/lock.
  > "Given that this is a fatal error path that can be reached due to OOM, I would prefer not allocating memory here." — janvorli

- **Avoid thread-local objects with destructors in CoreCLR.** Destruction order is arbitrary. Tie lifetime to the CoreCLR Thread object. Prefer `PLATFORM_THREAD_LOCAL` from minipal over C++ `thread_local` in perf-critical paths.
  > "Having a thread local object with a destructor is a recipe for possible problems due to the fact that the order of their destruction is arbitrary." — janvorli

- **Use `SET_UNALIGNED` macros for potentially unaligned writes.** In code generation stubs, use `SET_UNALIGNED_32/64` rather than direct pointer dereferencing.
  > "This should use SET_UNALIGNED_64." — jkotas

- **Zero-initialize arrays and buffers that may be partially used.** Zero-init allocated arrays whose elements have destructors. Zero-init EH tables, C arrays, and similar structures.
  > "Should the content of the array be zero-initialized? Otherwise, the destructor may access uninitialized memory if there is an exception thrown mid-flight." — jkotas

- **Add static asserts for hardcoded structural offsets.** When using hardcoded offsets to access struct fields (especially in assembly), add static asserts to verify them.
  > "It would be good to add some static asserts to verify that these offsets are valid. I am worried that some future change could break these." — janvorli

- **Use minipal for new platform abstractions.** Use minipal (new) instead of PAL (legacy) for platform abstraction in new CoreCLR code. Use `ALTERNATE_ENTRY` (not `LOCAL_LABEL`) for assembly labels called from outside their function.
  > "The minipal is the new place for abstracting platform dependencies." — janvorli

- **Use `JITDUMP` and `LOG` macros, not `printf`.** In JIT code use `JITDUMP`. In CoreCLR VM use `LOG()`/`LOGGING` defines. Do not use `printf` or `Console.WriteLine` in production native code.
  > "This should probably be using the JITDUMP or alternative API rather than just calling `printf`." — tannergooding

### P/Invoke & Marshalling

- **Prefer 4-byte `BOOL` for native interop marshalling.** Use `UnmanagedType.Bool`. Verify P/Invoke return types match native signatures exactly—mismatches may work on 64-bit but fail on 32-bit/WASM.
  > "bool marshalling has always been bug prone area. The 4-byte bool (UnmanagedType.Bool) tends to be the least bug-prone option." — jkotas
