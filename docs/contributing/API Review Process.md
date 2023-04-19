# API Review Process

.NET has a long-standing history of taking API usability extremely seriously. Thus, we generally review every single API that is added to the product. This page discusses how we conduct design reviews for the Roslyn components.

## Which APIs should be reviewed?

The rule of thumb is that we (**dotnet/roslyn**) review every public API that is being added to this repository in the CodeAnalysis (compiler), Workspaces, Features, and EditorFeatures libraries. APIs in the LanguageServices dlls are not usually reviewed as they are not intended for general consumption; they are for use in Visual Studio, and we don't have a compat bar for these APIs.

## Steps

1. **Requester files an issue**. The issue description should contain a speclet that represents a sketch of the new APIs, including samples on how the APIs are being used. The goal isn't to get a complete API list, but a good handle on how the new APIs would roughly look like and in what scenarios they are being used. Please use [this template](https://github.com/dotnet/roslyn/issues/new?template=api-suggestion.md). The issue should have the labels `Feature Request` and `Concept-API`. [Here](https://github.com/dotnet/roslyn/issues/53410) is a good example of a strong API proposal.

2. **We assign an owner**. We'll assign a dedicated owner from our side that sponsors the issue. This is usually [the area owner](../area-owners.md#areas) for which the API proposal or design change request was filed.

3. **Discussion**. The goal of the discussion is to help the assignee to decide whether we want to pursue the proposal or not. In this phase, the goal isn't necessarily to perform an in-depth review; rather, we want to make sure that the proposal is actionable, i.e. has a concrete design, a sketch of the APIs and some code samples that show how it should be used. If changes are necessary, the owner will set the label `api-needs-work`. To make the changes, the requester should edit the top-most issue description. This allows folks joining later to understand the most recent proposal. To avoid confusion, the requester can maintain a tiny change log, like a bolded "Updates:" followed by a bullet point list of the updates that were being made. When you the feedback is addressed, the requester should notify the owner to re-review the changes.

4. **Owner makes decision**. When the owner believes enough information is available to make a decision, they will update the issue accordingly:

    * **Mark for review**. If the owner believes the proposal is actionable, they will label the issue with `api-ready-for-review`.
    * **Close as not actionable**. In case the issue didn't get enough traction to be distilled into a concrete proposal, the owner will close the issue.
    * **Close as won't fix as proposed**. Sometimes, the issue that is raised is a good one but the owner thinks the concrete proposal is not the right way to tackle the problem. In most cases, the owner will try to steer the discussion in a direction that results in a design that we believe is appropriate. However, for some proposals the problem is at the heart of the design which can't easily be changed without starting a new proposal. In those cases, the owner will close the issue and explain the issue the design has.
    * **Close as won't fix**. Similarly, if proposal is taking the product in a direction we simply don't want to go, the issue might also get closed. In that case, the problem isn't the proposed design but in the issue itself.

5. **API gets reviewed**. The group conducting the review is called *RAR*, which stands for *Roslyn API Reviewers*. In the review, we'll take notes and provide feedback. After the review, we'll publish the notes in the [API Review repository](https://github.com/dotnet/apireviews) and at the end of the relevant issue. Multiple outcomes are possible:

    * **Approved**. In this case the label `api-ready-for-review` is replaced with `api-approved`.
    * **Needs work**. If we believe the proposal isn't ready yet, we'll replace the label `api-ready-for-review` with `api-needs-work`.
    * **Rejected**. If we believe the proposal isn't a direction we want to pursue, we'll simply write a comment and close the issue.

## Review schedule

 There are three methods to get an API review (this section applies to API area owners, but is included for transparency into the process):

* **Get into the backlog**. Generally speaking, filing an issue in `dotnet/roslyn` and applying the label `api-ready-for-review` on it will make your issue show up during API reviews. The downside is that we generally walk the backlog oldest-newest, so your issue might not be looked at for a while. Progress of issues can be tracked via <https://apireview.net/backlog?g=Roslyn>.
* **Fast track**. If you need to bypass the backlog apply both `api-ready-for-review` and `blocking`. All blocking issues are looked at before we walk the backlog.
* **Dedicated review**. If an issue you are the area owner for needs an hour or longer, send an email to roslynapiowners and we book dedicated time. We also book dedicated time for language feature APIs: any APIs added as part of a new language feature need to have a dedicated review session. Rule of thumb: if the API proposal has more than a dozen APIs, the APIs have complex policy, or the APIs are part of a feature branch, then you need 60 min or more. When in doubt, send mail to roslynapiowners.

The API review board currently meets every 2 weeks.

## Pull requests

Pull requests against **dotnet/roslyn** that add new public API shouldn't be submitted before getting approval. Also, we don't want to get work in progress (WIP) PR's. The reason being that we want to reduce the number pending PRs so that we can focus on the work the community expects we take action on.

If you want to collaborate with other people on the design, feel free to perform the work in a branch in your own fork. If you want to track your TODOs in the description of a PR, you can always submit a PR against your own fork. Also, feel free to advertise your PR by linking it from the issue you filed against **dotnet/roslyn** in the first step above.

For **dotnet/roslyn** team members, PRs are allowed to be submitted and merged before a full API review session, but you should have sign-off from the area owner, and the API is expected to be covered in a formal review session before being shipped. For such issues, please work with the API area owner to make sure that the issue is marked blocking appropriately so it will be covered in short order.
