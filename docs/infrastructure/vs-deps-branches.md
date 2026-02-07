# Why do we have -vs-deps branches?

The Roslyn repo includes projects that reference Visual Studio APIs. These projects create a challenge because we have two tensions pulling us in opposite directions:

1. We often want this to be able to reference the latest Visual Studio APIs, even if the APIs are not yet shipped. If the editor team creates a new API for us to implement or consume, they would
   like us to consume it as soon as they have merged their feature work into the (internal) Visual Studio master branch; this means they can get API feedback and shake out any bugs as soon as possible.
2. We want public contributors to be able to contribute to all of Roslyn, the Visual Studio layers included. Even if the contributor isn't _changing_ the code in the Visual Studio layer, deploying to Visual
   Studio is the only way to actually _test_ any changes made to any of our layers in the middle in an interactive way. A contributor making a change to a code fix or refactoring can of course write unit
   tests, but they can't just "try it locally" to experiment.

To find a balance between these two tensions, we have two branches. Using our master branch as an example, we have two branches named 'master' and 'master-vs-deps':

1. The master branch as a rule must be buildable and runnable against the latest public non-preview build of Visual Studio. This means no change can be made inside of it that is dependent on an preview
   or yet unshipped API in a way that would break the ability to run locally.
2. The master-vs-deps branch only has to run against whatever internal Visual Studio branch we are targeting for insertion. In some cases, if we have a breaking change that must be coordinated with a
   partner team, this means for a small window in time it might be runnable against _no_ build because we are doing a simultaneous insertion. This latter case is rare, but does happen.

We have a bot that regularly merges code from master to master-vs-deps. This means that whether a change goes into master or master-vs-deps, it'll ship in the same Visual Studio release. The distinction
is simply what the code can be locally ran against.

As a concrete example, imagine that the internal builds of Visual Studio are building 16.5. Changes in Roslyn master will ship in 16.5; so will changes in master-vs-deps. However, changes in master must
be runnable by an external contributor, so it must be runnable on 16.4. Changes in master-vs-deps don't have to be runnable anywhere, but obviously cannot depend on something shipping in 16.6, because
the branch is still shipping in 16.5.

master is not the only branch that has -vs-deps branches. We maintain the pair for all our milestone branches. Consider the case where we are beginning to lock down a release. At this time we often
snap master and only approved changes go in. We still have a -vs-deps branch and a regular branch, if for no other reason than we must auto-merge any further changes into the appropriate branches. Why
not branch _just_ the -vs-deps branch? Because then all changes would go into there, and a compiler change which has no reason to be anywhere in -vs-deps would end up getting merged into master-vs-deps.

# Does my change have to go into a -vs-deps branch or not?

Your change should go into a -vs-deps branch only if both of the criteria below are satisfied:

1. The change requires updating to some new NuGet package that bumps us to some higher version of the Visual Studio or related packages (like editor, debugger, etc.)
2. Your change can't practically be done with some sort of dynamic lightup.

For example, let's say in 16.5 the editor has added a new API to Visual Studio, which requires us to implement a new interface and MEF export it. Because you must implement the interface and MEF export it,
we need to move to a newer NuGet package that contains the new APIs which will have a higher assembly version, and you must implement the interface which practically can't be done via something like reflection.
Thus, this change should go into the appropriate -vs-deps branch.

Consider a different case where the editor in 16.5 has added a new API, but we must MEF _import_ it and call a method on it. It's possible to do this via reflection -- you MEF import a raw object,
and do reflection to call the API accordingly. This is now a trade off we must make. If this is a huge feature that churns thousands of lines of code, but only tiny bit needs to use this new API
we would generally ask that it goes into master and we use reflection, as the more master and master-vs-deps diverge, the harder the automatic merge becomes. If this is a feature that must use the new API
everywhere and it'd be vastly more work to do it dynamically than any work we might spend merging stuff, then we'll just have it go into master-vs-deps.

# When do we ever merge from a -vs-deps branch to a regular branch?

Once a version of Visual Studio ships and we are comfortable making that be the new contributor baseline, we do a manual merge from the appropriate -vs-deps branch back. This sometimes means we merge master-vs-deps
back into master, or sometimes not. If we just shipped 16.4, it's possible that master-vs-deps only depends on stuff in 16.4, so we would merge master-vs-deps back to master. It's also possible that feature work
has already started on 16.5, and master-vs-deps now contains 16.5 dependent work. The "safe" choice is to merge back just the -vs-deps branch for 16.4, since we know that runs on 16.4 -- it's shipping in it!

# What prevents us from checking stuff what should have gone into a -vs-deps branch into a regular branch?

Integration tests; our integration tests in master are running against released bits, and not random nightly builds. The common case of us moving to a new API in a mainline scenario would break integration tests. Obviously
if the break is in some untested scenario that won't catch it, but it's also possible it's obscure enough that nobody will ever notice. (If a scenario is broken, but nobody ever tries it, is it really broken?)

# Isn't this all a pain? Can we get rid of the -vs-deps branches?

Maybe, but it potentially replaces one set of problems with another set of problems. So far, the pain of this approach has been the lesser pain, so we've stuck with it. Here's some of the
more common attempts and the problems it creates that we haven't solved yet. This document isn't to say that these problems are unsolvable or are the reason to continue using -vs-deps, but this
is mostly writing down the "here's what you'll run into" to avoid people rediscovering the same reasons.

## What happens if we get rid of our -vs-deps branches, and only check in changes that run on the last released preview?

This solves many of the concerns, but it means we slow down internal feedback cycles. The best case for a change going into the internal Visual Studio master to a public build is around two weeks, the worst case
is two months. If say the editor team internally wants to make a change and they need us to build our part of the feature, we want to be able to dogfood that as early as possible. Having to wait on average a month or
so would slow down that cycle. It would also mean feature development cycles might end up getting stretched across multiple minor releases: if the editor landed their work in Preview 1 but we can't actually implement our side
until it's shipped publicly, they might not be getting feedback until later in the Preview 2 cycle. By then might be too late to get fixed up before that minor version, and we get to finalize everything later.

This also makes things more challenging if the internal team has to make a breaking change. Imagine that while consuming the API we discover we need to pass something new to a method; with the -vs-deps branches they can
make the breaking change to the API and we can update our code in a few days. They may have to leave an Obsolete version of the API for a few days while we get our stuff in, but that's on the order of days. Us only moving to
previews would mean they'd be having to ship already deprecated APIs in previews; and if it stretches across minor versions like before, then across minor versions.

## OK, so we have to check in things sooner. What if we just told our contributors to run preview builds and deal with occasional break?

Right now, we do make things worse on ourselves by generally mandating non-preview builds to contributors. It means the window where a break would go into Roslyn to being resolvable by moving to a new build can be a few months.
If we required contributors to move to preview builds it would mean the "time until a break is fixed" goes down. However:

1. Sometimes one preview build may be quite a while after another. Because of the holidays, 16.5 Preview 1 and 16.5 Preview 2 shipped pretty far apart -- Preview 1 shipped on December 3rd and Preview 2 shipped
   on January 22nd, a difference of 50 days.
2. Even if one preview has gone out, that doesn't mean that master will now work with it. There's usually a two or three week internal stabilization on a preview build, but by then we may have already made further
   breaking changes in master.

These two factors mean that a break wouldn't be a rare thing.

## OK, so can we avoid breaking changes? Can we use more lightup?

Possibly, and when we can easily do that we already do, but the moment the API gets tricky, we run into a few issues.

First, many components in Visual Studio bump assembly versions every minor release. If we move to consuming packages for 16.5, there aren't binding redirects if you're only running this on
16.4. It's busted, _even if we didn't consume any new APIs._ Assuming we updated Visual Studio to have binding redirects that redirect _down_ to older versions, we still run into CLR limitations.
If the API requires an existing type to implement a new interface, the type can't be loaded at all if the interface is missing.

Also, breaking changes aren't always unavoidable. We do sometimes have to do coordinated insertions. Being able to use the -vs-deps branch as a buffer has been helpful.

## What about splitting our build in some other way?

When we were trying to support Dev14 and Dev15 at the same time, Roslyn did a model where we had two VSIXes; there was one that had most of our stuff that ran on Dev14 and Dev15, and one that had the
new features for Dev15. This did mean that Dev14 was still supportable, but had a bunch of limitations:

1. It still didn't solve dealing with API breaks or changes within the Dev15 subset. That was still an all-or-nothing.
2. It assumed that lightup did work well. For an entirely new, isolated feature, it was easy. For existing features, it got messier. The extreme case was where the Visual Studio platform assumined we
   could easily implement a Dev15-only interface on our Dev14 components; this required a bunch of inheritance and light-up trickery to make it work. Put another way, this was great until you had a
   complicated case, and then it got _really_ complicated. With -vs-deps, you can just check in to the -vs-deps branch, do the right thing there, and have confidence you don't have debt to clean up
   later.
3. It caused a bunch of additional assemblies which broke internal performance tooling.
4. It still meant we had to us Dev14 infrastructure. One benefit of this is if there's a new MSBuild feature in some new Visual Studio build, we can potentially use that in -vs-deps as well if we are
   able to get the new MSBuild on the machine for signed builds.

## What if we just tell contributors to sync back to specific commits when contributing that are in sync with what they are running?

This would mean they're not broken, but would impact their workflow. The simple "git clone, build, F5" scenario is now broken which has always been our gold standard that it works. If things break,
it's often very difficult to diagnose why things are broken -- new members to our team often have to ask more experienced developers to sort out these things with internal dogfooding, and that's
when we can sit down side by side. Walking a first time contributor through diagnosing a MEF problem is a great way to not get contributors. We want to be welcoming and that includes our
infrastructure.

Submitting changes also becomes a more complicated workflow. If they submit a change, and there's a later change in master that causes a merge conflict, they cannot resolve this conflict and
test locally with F5. If they break an integration test, it now would now require an internal developer to take their change and do the testing and report it for them. In our early days of being
open source we had VS integration tests only running on internal VMs; this had the awkward situation where contributors could break tests they couldn't see. We weren't proud of that, and were happy
to have the process be open.

We could also imagine contributors fixing things that were already fixed -- they might see a bug in a preview that's trivial and try fixing it, only to discover it's already fixed in master.
It's hard to see if a bug is fixed and has a test covering it since who knows where the fix and test might be; it's easy to F5 and try it interactively.

## Can we bring dependencies along?

One possible solution to some of this is to include VSIX dependencies along with our build. So if you F5, we include a package that includes our dependencies we need. So F5 and run locally
would potentially install the editor package against the older Visual Studio. This works if the underlying dependencies don't take a dependency on anything that isn't easily shipped externally.
If we were consuming a new editor API and that API was constrained to something that only required the editor repo to be updated, than this works. The moment you have that depending on something
else -- or _any_ other unrelated change in the editor repo -- we now have just pushed our dependency problems to other teams.

We have done this in a limited sense in our Setup.Dependencies project, where we could ship higher versions of certain assemblies. That was only used in constrained situations when we were updating
single assemblies that were .NET framework assemblies that didn't have other dependencies.