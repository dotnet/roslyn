**Customer scenario**

What does the customer do to get into this situation, and why do we think this
is common enough to address for this release.  (Granted, sometimes this will be
obvious "Open project, VS crashes" but in general, I need to understand how
common a scenario is)

**Bugs this fixes:**

(either VSO or GitHub links)

**Workarounds, if any**

Also, why we think they are insufficient for RC vs. RC2, RC3, or RTW

**Risk**

This is generally a measure our how central the affected code is to adjacent
scenarios and thus how likely your fix is to destabilize a broader area of code

**Performance impact**

(with a brief justification for that assessment (e.g. "Low perf impact because no extra allocations/no complexity changes" vs. "Low")

**Is this a regression from a previous update?**

**Root cause analysis:**

How did we miss it?  What tests are we adding to guard against it in the future?

**How was the bug found?**

(E.g. customer reported it vs. ad hoc testing)

**Test documentation updated?**

If this is a new non-compiler feature, update https://github.com/dotnet/roslyn/wiki/Manual-Testing once you know which release it is targeting.		 +If this is a new non-compiler feature, or a significant improvement to an existing feature, update https://github.com/dotnet/roslyn/wiki/Manual-Testing once you know which release it is targeting.
