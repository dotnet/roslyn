# Developing a Language Feature

Adding a new feature to C# or VB is a very serious undertaking that often takes several iterations to complete for even the (seemingly) simplest of features. This is due to both the inherent complexity of changing languages and the need to consider the effects of new features in all layers of the Roslyn codebase: IDE, debugging, scripting, etc. As such, language work occurs in a separate branch until the feature reaches a point when we are ready to merge it into the main compiler.

This page discusses the process by which language feature *implementations* are considered, prototyped, and fully accepted into the language.  This process is intended to be used by the compiler team and community.  

## Process

1. **Feature specification filed**:  The speclet should be filed as a GitHub issue and contain:

        * A description of the feature (including any syntax changes involved)
        * Discussions about impacted areas, such as overload resolution and type inference. Think through the major areas of the language specification while determine the potential impacts
        * Proposed changes to the API surface area.

    A feature speclet is different from a language design discussion. Discussions are very open-ended and often for features that simply won't work in the language. A feature speclet is a declaration of intent to do the feature as proposed. In many cases, this will come out of a language design discussion.

    Linking to an early prototype of the proposed feature is highly encouraged. It gives the Language Team a clear understanding of the design intent of the feature and can often serve as the initial commit in the feature branch.  

    *Note*: The goal is not to have a 100%-complete design but instead a general understanding of how the language feature is intended to work.

1. **Discussion**: The goal of the discussion is to help clarify the design and determine if the Language Team wants to sanction the proposal. As the discussion evolves the author is encouraged to update the speclet to incorporate the changing design.  This allows folks who join later to understand the most recent proposal by reading the top of the thread.  

1. **Language team makes a decision**: When the Language Team believes enough information is available they will update the issue accordingly:

    * **Ready for prototype**: The feature is worth pursuing as a prototype for the language. Work continues in the "Prototype" phase.
    * **Not actionable**: The discussion didn't lead to a design that was actionable by the team.
    * **Declined as proposed**: The proposal is overall a good idea but the proposed implementation details are not as efficient as they could be. In most cases, the language team will attempt to steer the discussion to a cleaner design but understand that it may be simpler to close and start over with a new issue.  
    * **Backlog**: The feature is a good idea with an acceptable design but the Language Team simply can't allocate the resources at this time for this work.
    * **Won't Fix**: The feature isn't taking the language in a direction the Language Team is interested in pursuing.  

    A Language Team member will be paired with a community-driven feature when it is deemed 'Ready for prototype'. This team member will act as the primary source of contact for the community and will help drive the feature to completion.  

1. **Prototype phase**: All features are initially prototyped in a separate branch. The name of the branch is `features/<feature name>`. These branches are created when a feature speclet is marked as 'Ready for prototype', language team member champions the feature, and [a developer is assigned](#community-contributions) to implement the feature. The developer is responsible for keeping an up-to-date speclet of the feature at `docs/features/<feature name>.md` throughout the development.  

     All features should be off by default until fully accepted into the language. A new feature should only be enabled when explicitly included in the `/features:<feature-name>` compiler option. This helps ensure the language is always in a shippable state no matter what prototypes are in the current branch.

1. **Prototype decision**: When the prototype phase ends, the Language Team will make a decision on the feature:
    * **Ready to integrate**: The feature implementation revealed no blocking issues, has a healthy set of test suites, and is sufficiently complete in the compiler, IDE, debugger, etc. to integrate into the main tree on a selected branch.  The test suites need to focus both on the feature in isolation and when combined with other areas of the language.  
    * **Iterate**: Experience with the prototype has produced valuable feedback suggesting modifications to the
    speclet. Work reverts to the Discussion state, which may lead to speclet and prototype
    changes (or even abandoning the feature!). This typically occurs either when the original speclet
    left some language details unspecified, to be resolved based
    on implementation experience, or when experience with the prototype suggests that some language design
    decisions should be reconsidered. 
    * **Closed**: The feature hit unforeseen design issues, implementation road blocks, etc. that caused it to be removed.

1. **Finishing**: Once a feature is integrated into the main tree, it's time to lock down and finish the implementation. This includes any remaining areas not completed in the prototype, and completing all of the corner cases and tests (lots and lots of tests!).  

    Features which are in the main tree but not tied to a langversion switch are referred to as experimental. They can be pulled or changed at any time.

1. **Acceptance**: When the Language Team feels a feature is complete it will be accepted into the language. The corresponding feature flag will be removed and the feature will be tied to the appropriate `/langversion` option.  

It's important to note there are **no guarantees** about a feature being a part of the language until it has reached the 'Acceptance' state **and** shipped in an official build. Features in any other state--no matter how complete--can be pulled at the last minute if the Language Team deems it's necessary to do so.  

Pulling a feature after the prototype phase completes is not a decision the Langauge Team takes lightly; however, in the past, unforeseen circumstances have caused us to do so. The best example of this was the decision to pull parameterless struct constructors very late in the C# 6.0 timeframe.  

## Community Contributions
The intent of this document is to codify the process used by the Language Team to implement language features so that the community can contribute implementations. Community members can contribute to ongoing prototypes or in some cases be the primary driver of the prototype.  

Community members who wish to drive a particular feature can request so by commenting on an accepted feature speclet. Even when driven by a community member, a language prototype will occupy a non-trivial amount of time by the Language Team.  Hence we will likely only grant this request to community members who have been actively contributing to the project in smaller areas.
## Frequently Asked Questions

- **Can feature branches be rebased?**: This is left to the discretion of the developer working on the feature. Once the feature is merged into a main branch (master, future) then rebasing is no longer allowed.

- **Why was my language feature PR closed?**: Language features will not be accepted as direct PRs to a main branch. Even simple features are too involved for senior Language Team members to complete in this fashion. Features need to follow the above process in order to be incorporated into the language.

- **Why are features disabled by default?**: Language features spend a considerable amount of time in the same branch as our shipping product which is often master.  The master branch of Roslyn is kept in a ship ready state at all times.  To facilitate this all features which haven't been fully accepted into the language must be disabled.

- **What is the difference between the future and master branches?**: The master branch represents code which will ship in updates to the C# 6 compiler.  The future branch represents C# 7.  

- **Why weren't speclets filed for early features?**: Features such as tuples and local functions were done before this process was fully developed.  Their speclets were considered internally and eventually became the `docs/feature/<feature name.md>` file.

- **Why the high bar to create a feature branch?**: Language features, even when driven by the community, require a substantial long term resource investment by the language team.  The team must do regular code reviews, provide guidance, help with architectural issues, contribute, etc ...  
