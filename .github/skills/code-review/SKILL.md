---
name: code-review
description: Review code changes in dotnet/roslyn for correctness, performance, and consistency with project conventions. Use when reviewing PRs or code changes.
---

# dotnet/roslyn Code Review

Review code changes against conventions and patterns established by dotnet/roslyn maintainers and the broader .NET compiler platform team. These rules are adapted from dotnet/runtime review practices and tailored to Roslyn's compiler and IDE infrastructure.

**Reviewer mindset:** Be polite but very skeptical. Your job is to help speed the review process for maintainers, which includes not only finding problems the PR author may have missed but also questioning the value of the PR in its entirety. Treat the PR description and linked issues as claims to verify, not facts to accept. Question the stated direction, probe edge cases, and don't hesitate to flag concerns even when unsure.

## When to Use This Skill

Use this skill when:
- Reviewing a PR or code change in dotnet/roslyn
- Checking code for correctness, performance, style, or consistency issues before submitting a PR
- Asked to review, critique, or provide feedback on code changes
- Validating that a change follows dotnet/roslyn conventions

## Review Process

### Step 0: Gather Code Context (No PR Narrative Yet)

Before analyzing anything, collect as much relevant **code** context as you can. **Critically, do NOT read the PR description, linked issues, or existing review comments yet.** You must form your own independent assessment of what the code does, why it might be needed, what problems it has, and whether the approach is sound — before being exposed to the author's framing. Reading the author's narrative first anchors your judgment and makes you less likely to find real problems.

1. **Diff and file list**: Fetch the full diff and the list of changed files.
2. **Full source files**: For every changed file, read the **entire source file** (not just the diff hunks). You need the surrounding code to understand immutability patterns, service contracts, symbol resolution logic, and data flow. Diff-only review is the #1 cause of false positives and missed issues.
3. **Consumers and callers**: If the change modifies a public/internal API, a language service interface, or a syntax/semantic model API, search for how consumers use the functionality. Grep for callers, usages, and test sites. Understanding how the code is consumed reveals whether the change could break existing behavior or violate caller assumptions.
4. **Sibling implementations**: If the change fixes a bug or adds a pattern to C# code, check whether the VB implementation has the same issue. If modifying an analyzer, check if the corresponding code fix needs updating. Fetch and read those files too.
5. **Key utility/helper files**: If the diff calls into shared utilities, read those to understand the contracts (thread-safety, immutability, cancellation).
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
- **Cross-cutting analysis** should be included when relevant: check whether related code (sibling types, callers, VB compiler) is affected by the same issue or needs a similar fix.
- **Test quality** should be assessed as its own finding when tests are part of the PR.
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

- **Every PR must articulate what problem it solves and why.** Don't accept vague or absent motivation. Ask "What's the rationale?" and block progress until the contributor provides a clear answer.
  > "I am not sure why is this needed. ... It's not immediately obvious whether this happens only for the bridge comparison tests or whether it can happen for real-life scenarios too."

- **Challenge every addition with "Do we need this?"** New code, APIs, abstractions, and flags must justify their existence. If an addition can be avoided without sacrificing correctness or meaningful capability, it should be.
  > "I don't think we should take this change, at all. A change which makes the VS runner see the same assets as the CLI runner, sure. But random extra hacking on the side, no."

- **Demand real-world use cases and customer scenarios.** Hypothetical benefits are insufficient motivation for expanding API surface area or adding features. Require evidence that real users need this.
  > "It is not clear to me whether you can hit a real-world scenario on 32-bit platforms where it makes a difference."

---

## Detailed Code Review

### Scope & Focus

- **Require large or mixed PRs to be split into focused changes.** Each PR should address one concern. Mixed concerns make review harder and increase regression risk.
  > "I think I'm going to break this into two pieces, even though that's more work."

- **Defer tangential improvements to follow-up PRs.** Police scope creep by asking contributors to separate concerns. Even good ideas should wait if they're not part of the PR's core purpose.
  > "Should probably be a separate PR."

- **Consider separating bug fixes from feature additions.** It's not uncommon for feature work to reveal existing issues. When that happens, consider whether the bug fix should be merged independently of the feature work and directly to the `main` branch. It's also good to record the existence of the bug in an issue if it doesn't already exist, so that the fix can be tracked and backported as needed.
  > "Since this is a bug fix, should we merge it directly to `main` branch? Please open an issue to track the bug and link it here."

### Evidence & Data

- **Require measurable performance data before accepting optimization PRs.** Demand BenchmarkDotNet results or equivalent proof — never accept performance claims at face value.
  > "Can you please share a benchmark using BenchmarkDotNet against public LookupSymbols API that demonstrates the improvement?"

- **Distinguish real performance wins from micro-benchmark noise.** Trivial benchmarks with predictable inputs overstate gains from jump tables, branch elimination, and similar tricks. Require evidence from realistic, varied inputs.
  > "Try to benchmark it with an input that varies randomly. Jump tables are great for trivial micro-benchmarks, but they are less great for real world code."

- **Investigate and explain regressions before merging.** Even if a PR shows a net improvement, regressions in specific scenarios must be understood and explicitly addressed — not hand-waved.
  > "Could you please inspect the regressions on why exactly it's an improvement there?"

### Approach & Alternatives

- **Check whether the PR solves the right problem at the right layer.** Look for whether it addresses root cause or applies a band-aid. Prefer fixing the actual source of an issue over adding workarounds to production code.
  > "Instead of changing the core logic of the DAG builder to detect redundant patterns, can we instead construct a temporary pattern and running the existing DAG builder on it?"

- **Proactively consult domain experts for risky areas.** When a change touches safety-critical, complex, or historically-problematic code areas (e.g., overload resolution, IVT checks, flow analysis), consult the domain expert rather than waiting for them to discover the issue during review.

- **When a PR takes a fundamentally wrong approach, redirect early.** Don't iterate on implementation details of a flawed design. Push back on the overall direction before the contributor invests more time.

- **Ask "Why not just X?" — always prefer the simplest solution.** When a PR uses a complex approach, challenge it with the simplest alternative that could work. The burden of proof is on the complex solution.

- **Fix root cause, not symptoms.** Investigate and fix the root cause rather than adding workarounds or suppressing warnings. Don't just catch and ignore exceptions without understanding why they occur.

### Cost-Benefit & Complexity

- **Explicitly weigh whether the change is a net positive.** A performance trade-off that shifts costs around is not automatically beneficial. Demand clarity that the change is a win in the typical configuration, not just in a narrow scenario.
  > "It is a performance trade-off. You will shift the costs around. It is not clear to me whether it would be a win at the end in the typical configuration."

- **Reject overengineering — complexity is a first-class cost.** Unnecessary abstraction, extra indirections, and elaborate solutions for marginal gains are actively rejected.
  > "This optimization smells funny. It seems overly complicated for little win. Is this path hot?"

### Reducing Code Review Load

- **No style-only changes to the compiler.** Every compiler code change needs to be reviewed for correctness, so style-only changes are not acceptable. It is okay for added or changed code to take advantage of new C# features, but we do not broadly revisit existing code to adopt new features or change style.
  > "Please avoid style-only changes."

- **Separate refactorings from other changes.** When possible, it is preferrable to separate refactorings into their own commits, especially when restructuring code to separate files, from functional changes. If PR feedback leads to renaming a type, renaming the file for that type should be done as a separate commit **after** the PR has been approved. This allows the reviewer to diff the functional change effectively.
  > "Please avoid renaming files while the PR is under review. If we need to rename, we can do that in a follow-up commit after the PR is approved."

### Error Handling & Assertions

- **Use the null-forgiving operator (`!`) in compiler product code only when null has already been validated.** Nullability suppressions (`!`) should only appear inside assertions or when the null check was already performed earlier in the same method but the compiler couldn't track it through the control flow. In all other cases, prefer `Debug.Assert(value != null)` over silencing the warning.

- **Use `throw` for reachable error paths, `UnreachableException` for exhaustive switches.** When a code path might be hit at runtime, throw an exception rather than asserting. Use `throw new UnreachableException()` for default cases in exhaustive switches.

### Correctness Patterns

- **Prefer allowlists over denylists for safety-critical checks.** To be correct by construction, it is better to only allow recognized/safe constructs than disallow a list of known unsafe constructs.
  > "I think the code will be more robust long term if it enumerates known safe constructs rather than enumerating known unsafe constructs."

- **Don't bail out early on `HasErrors`.** It's normal for a node to be erroneous in some way but for analysis to still produce useful information. Removing overly broad `HasErrors` checks often reveals better diagnostics and nullability warnings that were being suppressed.
  > "I believe the HasErrors condition is also not necessary. Basically, it's normal for a node to be erroneous in some way, but for us to still be able to operate on it, and report the information we do know about it."

- **Question every suspicious change.** When a diff contains changes that look unrelated or accidental (e.g., a variable name change, a reordered parameter), call it out explicitly. Accidental pastes and unintended modifications happen.
  > "This change looks suspicious. Is it intentional?"

- **Prefer `abstract`/`sealed override` over `virtual` for symbol properties.** Using `abstract` (or `sealed override`) instead of `virtual` helps catch derived types that need special handling — the compiler will force implementors to explicitly consider the property.
  > "We generally prefer `abstract`/`sealed override` over `virtual`. That helps catch any derived type that needs special handling."

- **Use proper symbol equality checks.** Don't use reference equality (`==`) for symbols. Use `SymbolEqualityComparer.Default` or the `Equals` method. Be aware of generic substitution.
  > "Symbols can be different instances but semantically equal due to generic instantiation."

### Allocation Avoidance

- **Avoid LINQ in compiler produce code.** In `src/Compilers/`, except for trival operations like `.Any(...)`, use manual loops instead of LINQ. LINQ is acceptable in IDE features and tests, but avoid in performance-critical code.
  > "Avoid LINQ in hot paths - use foreach or for loops"

- **Avoid `foreach` over collections without struct enumerators.** Prefer explicit enumerators or `.AsSpan()` for arrays to avoid allocations.

- **Avoid closures in hot paths.** When a lambda captures locals creating a closure, consider refactoring to avoid the capture or use a static lambda with explicit parameters. Prefer `static` lambdas when possible.

- **Use `ArrayBuilder<T>` and `PooledObjects` helpers.** Roslyn provides pooled collection builders in `Microsoft.CodeAnalysis.PooledObjects`. Use these in hot paths instead of allocating new collections. But be careful to return pooled objects to the pool and avoid leaks.

### Code Structure for Performance

- **Place cheap checks before expensive operations.** Check simple conditions (null checks, kind checks) before calling into semantic models or expensive operations.

- **Short-circuit tree visitors once the result is determined.** When a visitor is searching for a condition (e.g., "does any node match X?"), stop visiting other nodes as soon as the answer is known. Override the common `Visit` method to check the result flag and bail out early.
  > "It looks like as soon as this field is set to `true`, there is no reason to keep visiting other nodes. Consider overriding a common Visit method and implementing the shortcut."

- **Use `StringComparer.Ordinal` for internal comparisons.** Use ordinal comparison for identifiers and internal strings. Only use culture-aware comparison for user-facing text.

### Code Style & Formatting

- **In compiler code, use `var` only when the type is obvious from context.** Use explicit types for casts, method returns, and async infrastructure. Never use `var` for numeric types.
  > "Make the type explicit."

- **Declare each local variable in its own statement.** Don't combine multiple variable declarations on a single line (e.g., `bool a = false, b = false;`). Use separate declaration statements for clarity.
  > "I think we prefer a dedicates declaration statement for each local"

- **Place local functions at method end and avoid nested them.** Local functions go at the end of the containing method to avoid interrupting the logical flow.

- **Place fields first in types.** Fields are the first members declared in a type.

---

## Test Requirements

- **Always add regression tests for bug fixes and behavior changes.** Prefer adding test cases to existing test files rather than creating new files.

- **Add `[WorkItem("https://github.com/dotnet/roslyn/issues/####")]` to regression tests.** This links the test to the original issue for traceability.

- **Test all relevant `LangVersion` boundaries.** When testing language-version-specific behavior, include: the version *before* the feature (a specific version string that disallows it), the version *after* (using `TestOptions.RegularNext` or a specific version that allows it), and the *preview* version (which always rolls forward with nightly builds).

### Test Code Quality

- **Avoid unnecessary intermediary assertions.** Tests should do the minimal amount of work to validate just the core issue being addressed.
  > "Use `.Single()` instead of asserting count then accessing the first element"
  > "No need to separately assert this is not null if the next assertion is going to fail anyway when it is null."

- **For error scenarios, prefer `VerifyEmitDiagnostics` over `VerifyDiagnostics` on the compilation.** `VerifyEmitDiagnostics` exercises a greater portion of the compilation pipeline.

- **For success scenarios, verify execution if possible.** Use `CompileAndVerify` when the test can execute and `VerifyDiagnostics()` on the resulting verifier.
  Since `CompileAndVerify` already emits and collects diagnostics, `VerifyEmitDiagnostics` on the compilation would involve redundant work.
  > "We generally prefer combining `VerifyDiagnostics` with `CompileAndVerify` since it collects diagnostics anyway and collects all diagnostics."

- **Suppress expected unrelated warnings in tests with `#pragma`.** When test code intentionally triggers unrelated warnings (e.g., CS0649 for unassigned fields), suppress them with `#pragma warning disable` to keep test output clean.
  > "Consider suppressing the warning with `#pragma`"

- **Every code path in the changed function should have dedicated test coverage.** Include both positive and negative outcomes. If logic is copied from another place, mention the source.
  > "It feels like all individual code paths in this function should have dedicated code coverage. That should include both positive and negative outcomes, when applicable."

- **Verify that test assertions are strong enough to detect regressions.** Use `SequenceEqual` when order matters, `SetEqual` when it doesn't, but be aware that `SetEqual` may not flag duplicate entries.
  > "Is this going to flag presence of duplicate entries?"

- **Format test diagnostics as they come from test output.** Keep diagnostics formatted with `.WithLocation` on the same line as `Diagnostic(`, matching the style produced by test infrastructure. This keeps style consistent and makes future updates produce smaller diffs.
  > "Consider leaving the diagnostics formatted exactly as they come from the test output"

- **Test metadata scenarios that can't be produced by C#.** Consider testing scenarios with metadata containing combinations that C# can't produce.
  > "Do we test metadata that cannot be produced from C# (the attribute is applied even though it's not allowed in C#)?"

---

## Documentation & Comments

- **Comments should explain why, not restate code.** Delete comments like `// Get the symbol` that just duplicate the code in English. Focus on explaining non-obvious decisions, trade-offs, or algorithm choices.

- **Move important implementation comments to doc comments on the property/method.** If a comment explains critical behavior of a property or method (e.g., "returns true until X is called"), it belongs in the XML doc comment, not buried inside the implementation.
  > "It is probably worth moving/copying this comment into a doc comment on the property"

- **Track deferred work with GitHub issues.** No TODO comment should be added or merged.

### Public API Requirements

- **New public APIs require approved proposals before PR submission.** All new compiler or workspace API surface must go through API review. PRs cannot be merged until the APIs are approved.
See more details about the process at https://github.com/dotnet/roslyn/blob/main/docs/contributing/API%20Review%20Process.md

- **Public API changes must have compiler-level tests.** When modifying or adding public APIs, include tests that exercise the compiler directly — not only higher-level IDE or language-service tests — to verify the observable behavior of the change.

### Breaking Changes

- **Flag breaking changes and require formal process.** Any time a change causes code that compiled with a shipped version of the compiler to now produce a diagnostic, we should consider whether it should be reviewed and documented as a breaking change. Depending on the impact, such as how much time passed for users to adopt the code pattern, we may need explicit approval from the compat council.
  > "Is this diagnostic going to be a breaking change?"

- **File breaking change documentation for behavioral changes.** Open an issue in dotnet/docs if changing public API behavior, even in preview releases.
