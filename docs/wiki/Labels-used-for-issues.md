This repo leverages a large set of labels to help the core team understand the priority of issues as well the area in which the issues apply.  Some of the labels are intended to be singular (for example, each issue can only have one priority), while others can overlap (an issue might involve both the Compilers team as well as the IDE team, at least initially).  It’s important to understand how these labels are used by the team at Microsoft to understand not only the state of issues, but also to verify that the issue you’ve encountered (or the feature you’re about to write) isn’t already in the database.

## Workflow

The following labels are used to track issues through their lifecycle:

[![image](https://cloud.githubusercontent.com/assets/3804346/11516889/18b8de2c-983b-11e5-833d-66b62adc32aa.png)](https://github.com/dotnet/roslyn/labels/0%20-%20Backlog)

[![image](https://cloud.githubusercontent.com/assets/3804346/11516908/3e85c502-983b-11e5-8b77-03835a0270a1.png)](https://github.com/dotnet/roslyn/labels/1%20-%20Planning)

[![image](https://cloud.githubusercontent.com/assets/3804346/11516922/56fc7ef0-983b-11e5-92d2-e0f831fbca5d.png)](https://github.com/dotnet/roslyn/labels/2%20-%20Ready)

[![image](https://cloud.githubusercontent.com/assets/3804346/11516927/68b1d442-983b-11e5-834e-ca28bd27e4fe.png)](https://github.com/dotnet/roslyn/labels/3%20-%20Working)

[![image](https://cloud.githubusercontent.com/assets/3804346/11516934/75c29fc2-983b-11e5-9544-db303562d59d.png)](https://github.com/dotnet/roslyn/labels/4%20-%20In%20Review)

A feature will initially end up on the **Backlog**, and may gradually make its way through **Planning**.  At some point, it may be **Ready** to implement.  The assigned developer will start **Working** on the change, and finally put it **In Review** when ready to submit a PR for review.  (Only one of these progress labels should be on the issue at any time.)

## Features

The following labels are feature-specific:

[![image](https://cloud.githubusercontent.com/assets/3804346/11516979/a319bcee-983b-11e5-8df3-af1fabc4f9a2.png)](https://github.com/dotnet/roslyn/labels/Feature%20Specification)

[![image](https://cloud.githubusercontent.com/assets/3804346/11517003/bb9cc93c-983b-11e5-878d-10e07a9cb5eb.png)](https://github.com/dotnet/roslyn/labels/Story)

[![image](https://cloud.githubusercontent.com/assets/3804346/11517022/d1d2b2e8-983b-11e5-89e8-dfa007e27c01.png)](https://github.com/dotnet/roslyn/labels/Design%20Notes)

Each feature has, at the very minimum, a **Feature Request**, and that’s the label you should use to search through planned feature work.

Ideally, features that we work on should be part of a theme, so that we don’t end up delivering an unfocused “bag of hammers” to customers and instead are thoughtful about how new features work together and what problems they are solving.  Therefore, the feature request will be assigned into an overarching **Story** by the team at Microsoft.

Features which are approved will eventually have **Feature Specifications** defined for them, either in the issue itself or in a separate (linked) issue.  Similarly, **Design Notes** may be created for the feature if the design isn’t trivial.

## Bugs

Bugs are, fittingly, described by this label:

![image](https://cloud.githubusercontent.com/assets/20570/11489728/d23b2a72-9786-11e5-8fce-a467a8a1afab.png)

Although more often seen with **Feature Requests**, a **Bug** might also have **Design Notes** if sufficient complex (although it’s arguable that the Bug should probably be considered a feature at that point). 
Sometimes, however, the person opening the issue might not be sure if it’s a bug or not, and indicates that perhaps they might not understand how the product is supposed to work. In that case, the **Question** label is used instead until it’s determined that the issue is instead a bug, or the question is answered:

![image](https://cloud.githubusercontent.com/assets/20570/11489769/29e917e8-9787-11e5-8e4f-4947f9fa53f4.png)

Finally, if the issue is specific to a test (i.e., a bug in the test, as opposed to a bug found by a test), the **Test** label should be used:

![image](https://cloud.githubusercontent.com/assets/20570/11489780/41cbbeec-9787-11e5-86ee-cd733a7fdb1d.png)

Bugs (and questions) can be augmented with other labels that further define their urgency, their impact, or their progress.

## Urgency

![image](https://cloud.githubusercontent.com/assets/20570/11489790/6563c250-9787-11e5-9c4a-63c462d00443.png)

These labels call out bugs that need particular attention:
- **Now**:  People are badly blocked with no workaround; the issue has priority over any other work, and ideally will be resolved within 24 hours.
- **Soon**:  People can work around the issue but it’s painful.  The issue must be resolved during the current sprint.

## Progress pre-resolution

![image](https://cloud.githubusercontent.com/assets/20570/11489812/8ce9d9e0-9787-11e5-8d00-c95d146ec3db.png)

The labels define how a bug was resolved when the issue was closed:
- **Answered**:  This applies only to Questions, and is self-explanatory.
- **By Design**:  The behavior reported in the issue is actually correct.
- **Duplicate**:  An earlier issue is already tracking this behavior (the developer resolving a duplicate should link to the other issue in the text).
- **External**:  It was a real bug, but not in the code in this repo.  (A link to the actionable issue on the alternate database/repo should be listed in the text of the bug.)
- **Fixed**:  Self-explanatory.
- **Not Applicable**: The issue is not relevant to code in this repo and is not an external issue. 
- **Not Reproducible**:  The developer couldn’t reproduce the bug.
- **Won’t Fix**:  A real bug, but Triage feels that the issue is not impactful enough to spend time on.

## Verification post-resolution

![image](https://cloud.githubusercontent.com/assets/20570/11489846/caa201d6-9787-11e5-9e4c-22090b92a666.png)

These labels apply to bugs which have been fixed and closed; second-party validation is subsequently required to ensure that the fix really works.  The verification must be done against a build containing the fix (i.e., a code review is not sufficient).
- **Verification Not Required**:  The fix is validated automatically by metrics, and manual validation isn’t required, so this issue can be safely ignored when determining the list of “closed by not verified” issues.
- **Verified**:  The fix was verified by a second party and so this issue can be safely ignored when determining the list of “closed by not verified” issues.

## Tenets and Exceptions
Certain bugs are filed by Microsoft personnel due to a failure to meet stringent Microsoft metrics before the next milestone completes.  The bugs must be addressed by the team unless an exception is granted by director-level sponsor of the tenet (and, as a result, the bugs are very unlikely to be assigned to anyone outside of the team at Microsoft).

### Types of tenet bugs

![image](https://cloud.githubusercontent.com/assets/20570/11489861/ecaec764-9787-11e5-9c73-3000214ebb5a.png)

- **Acquisition**:  User cannot successfully acquire/install the product in some situation.  These are often setup bugs.
- **Compatibility**:  Violation of forwards/backwards compatibility in a design-time piece.
- **Compliant**:  Violation of compliance with things like signing, security, legality, etc.
- **Localization**:  Also called “World Ready.”  Some piece of UI isn’t localized (or able to be localized), often due to hard-coding of strings or other visible elements.
- **Performance**:  Regression in measured performance of the product from goals.
- **Reliability**:  Customer telemetry indicates that the product is failing consistently in a crash/hang/dataloss manner.
- **Telemetry**:  Our ability to collect telemetry is broken.  (Note: customer privacy is paramount for telemetry.)
- **User Friendly**:  Accessibility-related functionality is broken (e.g. things like high-DPI, key mnemonics, screen reader, etc.)

### Exceptions to Tenet bugs

![image](https://cloud.githubusercontent.com/assets/20570/11489878/223c32cc-9788-11e5-97ce-3fdb9882d819.png)

Sometimes, a tenet bug isn’t a blocking issue (although this is rare), and the team might ask for an exception to fix it in the next milestone. The above labels track the exception process.
- **TenetException-Consider**:  Triage wishes to pursue a Tenet Exception for the bug.
- **TenetException-Open**:  The exception information has been entered into the appropriate internal VS bug database.
- **TenetException-Tracked**:  The exception is granted for the current milestone, and the bug is moved to the next milestone and kept alive to track progress against it.
- **TenetException-Permanent**:  Exceedingly rare; the bug does not need to be fixed even in the future.  This generally happens when the code involved is either slated for replacement, or if the scenario has been removed from the test tracking the metric due to a change of scenario focus.

## Documentation
 
![image](https://cloud.githubusercontent.com/assets/20570/11489888/3a8b8f76-9788-11e5-9499-e7ba3c127aed.png)

**Documentation** should be considered an issue type just like bugs and features.  The label is not intended to be applied to bugs or features that need documentation; rather, it is applied to a separate issue that *tracks* the documentation of the feature or a bug for which it was created and to which it is linked.

The label applies to both new documentation and to documentation that requires changes.

## General
The following labels apply to both feature requests and bugs.

## Areas

![image](https://cloud.githubusercontent.com/assets/20570/11489897/6e1cb9f0-9788-11e5-9123-7f4f006b1d8e.png)

Issues are assigned by the internal Triage team to one of the above areas, which should all be self-explanatory.  Generally, only one **Area** label should be used, with bugs or features being split into two issues if team boundaries are crossed.  This allows the team managers to easily filter on the bugs that their team owns and track their progress against them.  Note that **Area-External** does not correspond to a team – it indicates that some other team owns the issue and that we are simply tracking it. 

In some cases, the team managers prefer to add an additional filter to further narrow down the area or its impact, in which case one of the following tags may be used in addition to the Area tag:
 
![image](https://cloud.githubusercontent.com/assets/20570/11489910/895b85b6-9788-11e5-93a9-5be3d5c2794a.png)

## Approved for next preview

![image](https://cloud.githubusercontent.com/assets/20570/11489916/958b9736-9788-11e5-8f8d-09b5f6acad77.png)

Strictly for internal use, the development manager of team at Microsoft (or his/her delegate) uses this to approve changes going into a milestone that is about to end – i.e., it meets the criteria (the “bar”) for introduction in the endgame and we are reasonably sure that it won’t destabilize our final bits.  Changes that don’t “meet the bar” go into the following milestone instead.

## Blocked

![image](https://cloud.githubusercontent.com/assets/20570/11489920/9e85f1a6-9788-11e5-85c2-32159d0a194b.png)

Progress on the issue is **Blocked**, either due to waiting for another code change, or possibly on customer feedback, or some other thing not fully under the control of the assignee.  The issue will contain text as to why it’s blocked.

## CLAs

![image](https://cloud.githubusercontent.com/assets/20570/11489936/b2f11c24-9788-11e5-8d48-b1add456d6a3.png)

Any pull request involving code requires a Contributor License Agreement to be signed, whereas documentation generally doesn’t.  The appropriate label will be automatically applied by the CI ‘bot.

## Concepts

![image](https://cloud.githubusercontent.com/assets/20570/11489941/be5c6c08-9788-11e5-9fda-3143c066b743.png)

**Concept** labels add context to an issue for easy filtering across features and bugs which touch on a theme:
- **API**:  This issue involves adding, removing, clarification, or modification of an API.  This could be a breaking change and thus is important to call out.
- **CoreCLR**: The issue involves operations and features specific to CoreCLR work, which is the reduced-size .NET framework.  (Other issues are assumed to be relevant regardless of framework except as noted in the text of the issue.)
- **Determinism**: The issue involves our ability to support determinism in binaries and PDBs created at build time (i.e., the same inputs always produce the same outputs).
- **Diagnostic Clarity**:  The issues deals with the ease of understanding of errors and warnings.
- **Portability**: The issue deals with portable code (portable libraries, etc.). 

## Contributor Pain

![image](https://cloud.githubusercontent.com/assets/20570/11489952/e06cc1ee-9788-11e5-9b78-4dc77357a435.png)

Any issue which impedes pull requests is marked with **Contributor Pain**.

## Discussion

![image](https://cloud.githubusercontent.com/assets/20570/11489957/ee4b33ae-9788-11e5-9b7b-fc6cd31d42bb.png)

The intent of the **Discussion** tag is to precisely that – to have a discussion that it not necessarily related to a concrete bug or feature.  The outcome of the discussion may be the creation of one or both of the latter, but otherwise discussion is more of a solo tag, though it might be marked with an **Area**.

## Flaky and Flaky-ExceptionGranted

![image](https://cloud.githubusercontent.com/assets/20570/11489969/0d46a360-9789-11e5-9726-6caa26652946.png)![image](https://cloud.githubusercontent.com/assets/20570/11489972/13e85f42-9789-11e5-94ae-9d1590bfe636.png)
  
The **Flaky** label notes that the issue does not always reproduce.  Typically, this label is applied to **Test** issues, although it can occasionally be applied to a hard-to-repro functional bug.
Flaky issues will have an **Urgency** label attached to them as well, as they are required to be fixed quickly.  However, the manager of a team can grant a temporary exception to repairing the flaky test if it is low-impact and there are other urgent issues that need attention.  In that case, the **ExceptionGranted** label is applied.

## Grabbed By Community

![image](https://cloud.githubusercontent.com/assets/20570/11489982/325fc2d0-9789-11e5-837b-cd5e8f24d710.png)

Applied to any issue that the team at Microsoft put up-for-grabs which is subsequent taken on by a member of our amazing, intelligent, and remarkably attractive community.  This tag and **Up For Grabs** should never be applied at the same time.

## Need More Info

![image](https://cloud.githubusercontent.com/assets/20570/11489989/46b6e100-9789-11e5-9498-e1ee58fb5e2e.png)

The issue lacks enough info to make progress on it (e.g. spec data for a feature, repro steps for a bug).  The developer will attempt to contact the person opening the issue to get more info.  After some reasonable amount of time, if the issue creator does not respond, the issue will be closed (and resolve as **“Not Reproducible”** if a bug). 

## New Language Features

The **New Language Feature - \<Feature Name\>** labels track issues related to language features under active development. These labels are deleted when their corresponding language feature becomes part of the official language and its feature flag has been removed.

## Pedantic
 
![image](https://cloud.githubusercontent.com/assets/20570/11489994/57ea2a86-9789-11e5-868b-db7be68cc87b.png)

Generally applied as a joke (hey, we get bored sometimes), the **Pedantic** label is applied to issues that are getting a bit off track by over-explaining an issue, especially in ways that are already obvious to everyone, lightly warning folks to get the discussion back on track.  The origin of the term *pedantic* is an interesting one, deriving from 17th-century England, and first used (apparently) in the John Donne poem “Sunne Rising.”  The noun form, *pedant*, seems to be much older and likely derives in some form from to the Latin *pædagogus* (English “pedagogue”, meaning “teacher”), although the borrowing is from 16th-century Italian.  The original meaning of *pedant* was, in fact, “teacher” before acquiring its subsequent meaning of a person who trumpets minor points of learning.  The meanings of *pedant* and *pedantic* seem to have been in transition almost from the point when they were coined; Donne subtly blends both meanings together with the verse “Sawcy pedantique wretch, goe chide Late schoolboyes.”  Donne is, of course, using spellings common in Early Modern English (the late modern form being “Saucy pedantic wretch, go chide boys who are late for school,” modernizing not only the spelling but leveraging a clause that removes the possible interpretation that the schoolboys in question are in fact dead).  However, the word form of *pedantic* in Donne’s text is inarguably influenced by French “-ique” suffix construction that was typical in Latinate-derived English adjectives for several centuries, which gradually changed to “-ic”.  (Interestingly, the modern French equivalent of *pedantic* is in fact “pédantesque.”)

## PR For Personal Review Only

![image](https://cloud.githubusercontent.com/assets/20570/11490026/ad00f63a-9789-11e5-9ffe-1615366f90fc.png)

The PR doesn’t require anyone other than the developer to review it – it might simply be a test of the infrastructure, or a minor change to repo documentation, or code changes that are never intended to be actually merged in.

## Up For Grabs

![image](https://cloud.githubusercontent.com/assets/20570/11490030/b5519efc-9789-11e5-8692-e50efef22370.png)

The team believes these issues require minimal context and are well-suited for new contributors. These issues are also aggregated by [Up-For-Grabs.net](http://up-for-grabs.net). Be sure to leave a comment indicating your plan to work on any issue.

Some Up For Grabs issues are in the "Unknown" milestone. These are typically not assigned to anyone on the team at Microsoft but are still interesting issues to solve, and so the community is therefore welcome to take the issue on by themselves.