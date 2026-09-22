import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

import yaml


WORKFLOWS = Path(os.environ.get("BFA_WORKFLOW_DIR", Path(__file__).resolve().parents[1]))
HEAD = "a" * 40
BOT = {"login": "github-actions[bot]", "type": "Bot"}
ITEMS = [
    {"type": "add_comment", "body": "Root cause"},
    {"type": "create_pull_request_review_comment", "body": "Fix the call", "path": "file.cs", "line": 10},
]
HARNESS = r"""
const fs = require("node:fs");
const input = JSON.parse(fs.readFileSync(0, "utf8"));
const outputs = {};
const calls = [];
const context = {
  repo: {owner: "dotnet", repo: "test"}, eventName: "issue_comment",
  payload: {comment: {id: input.request, created_at: "2026-09-01T00:00:00Z"}, issue: {number: 42}},
};
const core = {setOutput: (key, value) => outputs[key] = value, info() {}, notice() {}};
const github = {
  rest: {
    actions: {
      listJobsForWorkflowRunAttempt: "jobs",
      async listWorkflowRuns(options) {
        calls.push(["runs", options]);
        if (input.fail === "runs") throw new Error("Injected run-list failure");
        return {data: {total_count: input.total ?? input.runs.length,
          workflow_runs: input.runs.slice((options.page - 1) * 100, options.page * 100)}};
      },
    },
    issues: {listComments: "comments"},
    pulls: {listReviews: "reviews", listReviewComments: "inline"},
  },
  async paginate(method, options) {
    calls.push([method, options]);
    if (input.fail === method) throw new Error("Injected " + method + " failure");
    return method === "jobs" ? input.jobs[`${options.run_id}:${options.attempt_number}`] : input[method];
  },
};
const AsyncFunction = Object.getPrototypeOf(async function() {}).constructor;
(async () => {
  try {
    await new AsyncFunction("require", "context", "github", "core", input.script)(require, context, github, core);
  } catch (error) {
    process.exitCode = 1;
    console.error(error.message);
  }
  console.log(JSON.stringify({outputs, calls, items: JSON.parse(fs.readFileSync(process.env.GH_AW_AGENT_OUTPUT)).items}));
})();
"""


def load(name):
    lines = (WORKFLOWS / name).read_text(encoding="utf-8").splitlines()
    return yaml.safe_load("\n".join(lines[1:lines.index("---", 1)]))


class CommandPublicationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.name = "build-failure-analysis-command"
        if (WORKFLOWS / (cls.name + ".agent.md")).exists():
            cls.name += ".agent"
        cls.workflow = load(cls.name + ".md")
        cls.check = next(s for s in cls.workflow["jobs"]["fetch-binlog"]["steps"] if s.get("id") == "command")
        steps = cls.workflow["safe-outputs"].get("steps")
        if steps is None:
            steps = load("shared/build-failure-analysis-shared.md")["safe-outputs"]["steps"]
        cls.prepare = next(s for s in steps if s["name"] == "Prepare retry-safe command outputs")

    def run_script(self, which, state=None, items=None, request=123, head=HEAD, **changes):
        fixture = {
            "request": request, "runs": [], "jobs": {}, "comments": [], "reviews": [], "inline": [],
            **(state or {}), **changes,
            "script": (self.check if which == "check" else self.prepare)["with"]["script"],
        }
        with tempfile.TemporaryDirectory(prefix="command-publication-") as directory:
            path = Path(directory) / "output.json"
            path.write_text(json.dumps({"items": ITEMS if items is None else items}), encoding="utf-8")
            result = subprocess.run(
                ["node", "-e", HARNESS], input=json.dumps(fixture), capture_output=True, text=True,
                env={**os.environ, "GH_AW_AGENT_OUTPUT": str(path), "EXPECTED_HEAD": head,
                     "WORKFLOW_FILE": self.name + ".lock.yml"}, timeout=15,
            )
            return result, json.loads(result.stdout)

    def check_completed(self, state, expected, **changes):
        result, actual = self.run_script("check", state, **changes)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(actual["outputs"], {"completed": str(expected).lower()})
        return actual

    def prepare_items(self, state=None, **changes):
        result, actual = self.run_script("prepare", state, **changes)
        self.assertEqual(result.returncode, 0, result.stderr)
        return actual["items"]

    def publish(self, state, items, fail_summary=False, fail_review=False):
        # gh-aw writes summaries during processing and submits buffered inline
        # comments afterward, even when an earlier handler has failed.
        buffered = []
        for item in items:
            if item["type"] == "add_comment" and not fail_summary:
                state["comments"].append({"body": item["body"], "user": BOT})
            elif item["type"] == "create_pull_request_review_comment":
                buffered.append(item)
        if buffered and not fail_review:
            review_id = len(state["reviews"]) + 1
            state["reviews"].append({"id": review_id, "user": BOT, "state": "COMMENTED", "submitted_at": "2026-09-01"})
            state["inline"].extend({**item, "user": BOT, "pull_request_review_id": review_id} for item in buffered)
        run_id = len(state["runs"]) + 1
        state["runs"].append({"id": run_id, "run_attempt": 1, "display_title": "Build failure analysis command 123"})
        conclusion = "failure" if fail_summary or fail_review else "success"
        state["jobs"][f"{run_id}:1"] = [{"name": "safe_outputs", "conclusion": conclusion,
                                       "steps": [{"name": "Process Safe Outputs", "conclusion": conclusion}]}]

    def test_partial_failure_retry_publishes_only_missing_outputs(self):
        for fail_summary, fail_review in ((True, False), (False, True), (True, True), (False, False)):
            with self.subTest(fail_summary=fail_summary, fail_review=fail_review):
                state = {"runs": [], "jobs": {}, "comments": [], "reviews": [], "inline": []}
                self.check_completed(state, False)
                self.publish(state, self.prepare_items(state), fail_summary, fail_review)
                self.assertEqual(len(state["comments"]), int(not fail_summary))
                self.assertEqual(len(state["inline"]), int(not fail_review))
                self.check_completed(state, not (fail_summary or fail_review))
                retry = self.prepare_items(state)
                self.assertEqual([i["type"] for i in retry],
                                 (["add_comment"] if fail_summary else []) +
                                 (["create_pull_request_review_comment"] if fail_review else []))
                self.publish(state, retry)
                self.check_completed(state, True)
                self.assertEqual(len(state["comments"]), 1)
                self.assertEqual(len(state["inline"]), 1)
                self.assertEqual(self.prepare_items(state), [])

    def test_completion_requires_successful_finalization_for_exact_request_and_attempt(self):
        for job_status, step_status, title, expected in (
            ("failure", "failure", "123", False), ("cancelled", "success", "123", False),
            ("skipped", "skipped", "123", False), ("success", "skipped", "123", False),
            ("success", "failure", "123", False), ("success", "success", "1234", False),
            ("success", "success", "123", True),
        ):
            with self.subTest(job_status=job_status, step_status=step_status, title=title):
                state = {
                    "runs": [{"id": 9, "run_attempt": 2, "display_title": "Build failure analysis command " + title}],
                    "jobs": {"9:2": [{"name": "safe_outputs", "conclusion": job_status,
                                     "steps": [{"name": "Process Safe Outputs", "conclusion": step_status}]}]},
                }
                self.check_completed(state, expected)

    def test_failed_job_after_writes_retries_without_republishing(self):
        state = {"runs": [], "jobs": {}, "comments": [], "reviews": [], "inline": []}
        self.publish(state, self.prepare_items(state))
        state["jobs"]["1:1"][0]["conclusion"] = "failure"
        self.check_completed(state, False)
        retry = self.prepare_items(state)
        self.assertEqual(retry, [])
        self.publish(state, retry)
        self.check_completed(state, True)
        self.assertEqual(len(state["comments"]), 1)
        self.assertEqual(len(state["inline"]), 1)

    def test_run_history_is_paginated_and_bounded(self):
        unrelated = {"id": 1, "run_attempt": 1, "display_title": "another command"}
        state = {"runs": [unrelated] * 100 + [{"id": 2, "run_attempt": 1,
                  "display_title": "Build failure analysis command 123"}],
                 "jobs": {"2:1": [{"name": "safe_outputs", "conclusion": "success",
                          "steps": [{"name": "Process Safe Outputs", "conclusion": "success"}]}]}}
        actual = self.check_completed(state, True)
        self.assertEqual([options["page"] for method, options in actual["calls"] if method == "runs"], [1, 2])
        for method, options in actual["calls"]:
            if method == "runs":
                self.assertEqual(options["workflow_id"], self.name + ".lock.yml")
                self.assertEqual(options["event"], "issue_comment")
                self.assertEqual(options["status"], "completed")
                self.assertEqual(options["created"], ">=2026-09-01T00:00:00Z")
        actual = self.check_completed({"runs": [unrelated] * 1000}, False)
        self.assertEqual([options["page"] for method, options in actual["calls"] if method == "runs"], list(range(1, 11)))
        result, actual = self.run_script("check", total=1001)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(actual["outputs"], {})

    def test_retry_identity_preserves_distinct_findings_and_requests(self):
        prepared = self.prepare_items()
        self.assertEqual(self.prepare_items(items=prepared), prepared)
        state = {"comments": [{"body": prepared[0]["body"], "user": BOT}],
                 "reviews": [{"id": 1, "user": BOT, "state": "COMMENTED", "submitted_at": "2026-09-01"}],
                 "inline": [{"body": prepared[1]["body"], "user": BOT, "pull_request_review_id": 1}]}
        for changes in ({"request": 124}, {"head": "b" * 40}):
            self.assertEqual(len(self.prepare_items(state, **changes)), 2)
        for changes in ({"body": "A different finding"}, {"line": 11}, {"path": "other.cs"},
                        {"side": "LEFT"}, {"start_line": 9}):
            items = [ITEMS[0], {**ITEMS[1], **changes}]
            self.assertEqual(len(self.prepare_items(state, items=items)), 1)
        self.assertEqual(self.prepare_items(state, items=[{**ITEMS[0], "body": "Reworded summary"}]), [])
        self.assertEqual(len(self.prepare_items(items=[ITEMS[1], ITEMS[1]])), 1)

    def test_pending_reviews_and_untrusted_authors_do_not_suppress_outputs(self):
        prepared = self.prepare_items()
        for user in ({"login": "contributor", "type": "User"}, {**BOT, "type": "User"}):
            state = {"comments": [{"body": prepared[0]["body"], "user": user}]}
            self.assertEqual(len(self.prepare_items(state)), 2)
        pending = {"reviews": [{"id": 1, "body": prepared[1]["body"], "user": BOT, "state": "PENDING"}],
                   "inline": [{"body": prepared[1]["body"], "user": BOT, "pull_request_review_id": 1}]}
        self.assertEqual(len(self.prepare_items(pending)), 2)
        fallback = {"reviews": [{"id": 1, "body": "Unanchored finding:\n\n" + prepared[1]["body"],
                     "user": BOT, "state": "COMMENTED", "submitted_at": "2026-09-01"}]}
        self.assertEqual([i["type"] for i in self.prepare_items(fallback)], ["add_comment"])

    def test_api_and_identity_failures_leave_outputs_untouched(self):
        state = {"runs": [{"id": 1, "run_attempt": 1, "display_title": "Build failure analysis command 123"}]}
        for which, fail in (("check", "runs"), ("check", "jobs"), ("prepare", "comments"),
                            ("prepare", "reviews"), ("prepare", "inline")):
            with self.subTest(which=which, fail=fail):
                result, actual = self.run_script(which, state, fail=fail)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(actual["outputs"], {})
                self.assertEqual(actual["items"], ITEMS)
        for changes in ({"request": "123"}, {"head": ""}):
            result, actual = self.run_script("prepare", **changes)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(actual["items"], ITEMS)

    def test_compiled_workflow_preserves_guards_and_ordering(self):
        lock = yaml.safe_load((WORKFLOWS / (self.name + ".lock.yml")).read_text(encoding="utf-8"))
        self.assertEqual(lock["run-name"], "Build failure analysis command ${{ github.event.comment.id }}")
        fetch = lock["jobs"]["fetch-binlog"]
        self.assertEqual(fetch["permissions"]["actions"], "read")
        self.assertNotIn("actions", lock["jobs"]["agent"]["permissions"])
        steps = fetch["steps"]
        check = next(s for s in steps if s.get("id") == "command")
        download = next(s for s in steps if s.get("id") == "fetch")
        self.assertLess(steps.index(check), steps.index(download))
        self.assertIn("steps.command.outputs.completed == 'false'", download["if"])
        self.assertEqual(check["with"]["script"].strip(), self.check["with"]["script"].strip())
        safe_steps = lock["jobs"]["safe_outputs"]["steps"]
        prepare = next(s for s in safe_steps if s["name"] == self.prepare["name"])
        names = [s["name"] for s in safe_steps]
        self.assertEqual(prepare["with"]["script"].strip(), self.prepare["with"]["script"].strip())
        self.assertLess(names.index("Setup agent output environment variable"), names.index(prepare["name"]))
        self.assertLess(names.index(prepare["name"]), names.index("Revalidate PR revision before applying queued outputs"))
        self.assertLess(names.index(prepare["name"]), names.index("Process Safe Outputs"))


if __name__ == "__main__":
    unittest.main()
