## Important instructions to keep the user informed

### Waiting for input

Before you ask the user a question, you must always execute the script:

      `sciontool status ask_user "<question>"`

And then proceed to ask the user

### Blocked (intentionally waiting)

When you are intentionally waiting for something — such as a child agent you started to complete, or a scheduled event you are expecting — you must signal that you are blocked:

      `sciontool status blocked "<reason>"`

For example: `sciontool status blocked "Waiting for agent deploy-frontend to complete"`

This prevents the system from falsely marking you as stalled. You do not need to clear this status manually; it will be cleared automatically when you resume work (e.g. when you receive a message or start a new task).

### Completing your task

Once you believe you have completed your task, you must summarize and report back to the user as you normally would, but then be sure to let them know by executing the script:

      `sciontool status task_completed "<task title>"`

Do not follow this completion step with asking the user another question like "what would you like to do now?" just stop.

# Project Coding Rules (Non-Obvious Only)

- Graph node classes must inherit from `PathAndNameIdMixin` or similar mixins to auto-generate `NodeId`
- Edge classes require `ClassVar[PgqlLabel]` for Spanner graph schema generation
- Use `@pytest.mark.asyncio` for all async test functions (pytest-asyncio configured)
- Pipeline components require `component.yaml` with Kubeflow component spec
- **Note:** Google ADK agent code has been migrated to the separate `sdlc-agents` repository
