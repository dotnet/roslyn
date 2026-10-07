import json
import os
from pathlib import Path
import subprocess
import unittest

import yaml


WORKFLOWS = Path(__file__).resolve().parents[1]
WORKFLOW_NAME = "build-failure-analysis-command.agent"
COMMAND = "/analyze-build-failure"
HARNESS = r"""
const fs = require("node:fs");
const input = JSON.parse(fs.readFileSync(0, "utf8"));
const outputs = {};
const context = {
  repo: {owner: "dotnet", repo: "test"},
  eventName: "issue_comment",
  payload: {comment: {id: 123, created_at: "2026-10-01T00:00:00Z"}},
};
const core = {setOutput: (key, value) => outputs[key] = value, notice() {}};
const github = {
  rest: {actions: {listJobsForWorkflowRunAttempt: "jobs",
    async listWorkflowRuns() {
      return {data: {total_count: input.runs.length, workflow_runs: input.runs}};
    }}},
  async paginate(method, options) {
    return input.jobs[`${options.run_id}:${options.attempt_number}`] || [];
  },
};
const AsyncFunction = Object.getPrototypeOf(async function() {}).constructor;
(async () => {
  await new AsyncFunction("context", "github", "core", input.script)(context, github, core);
  console.log(JSON.stringify(outputs));
})().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
"""


def load(path):
    lines = path.read_text(encoding="utf-8").splitlines()
    return yaml.safe_load("\n".join(lines[1:lines.index("---", 1)]))


def publication_jobs(agent="success", cli="success", safe="success", process="success"):
    return [
        {
            "name": "agent",
            "conclusion": agent,
            "steps": [{"name": "Execute GitHub Copilot CLI", "conclusion": cli}],
        },
        {
            "name": "safe_outputs",
            "conclusion": safe,
            "steps": [{"name": "Process Safe Outputs", "conclusion": process}],
        },
    ]


class CommandPublicationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = load(WORKFLOWS / f"{WORKFLOW_NAME}.md")
        cls.check = next(
            step
            for step in cls.workflow["jobs"]["fetch-binlog"]["steps"]
            if step.get("id") == "command"
        )

    def run_check(self, jobs, title="Build failure analysis command 123"):
        fixture = {
            "runs": [{"id": 9, "run_attempt": 2, "display_title": title}],
            "jobs": {"9:2": jobs},
            "script": self.check["with"]["script"],
        }
        result = subprocess.run(
            ["node", "-e", HARNESS],
            input=json.dumps(fixture),
            capture_output=True,
            text=True,
            env={**os.environ, "WORKFLOW_FILE": f"{WORKFLOW_NAME}.lock.yml"},
            timeout=15,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return json.loads(result.stdout)

    def test_completion_requires_agent_cli_and_safe_output_success(self):
        cases = (
            (publication_jobs(), True),
            (publication_jobs(agent="failure", cli="failure"), False),
            (publication_jobs(cli="failure"), False),
            (publication_jobs(safe="failure", process="failure"), False),
            (publication_jobs(process="failure"), False),
            (publication_jobs()[1:], False),
        )
        for jobs, expected in cases:
            with self.subTest(jobs=jobs):
                self.assertEqual(
                    self.run_check(jobs),
                    {"completed": str(expected).lower()},
                )

    def test_activation_requires_the_exact_command_token(self):
        condition = self.workflow["jobs"]["fetch-binlog"]["if"]
        group = self.workflow["concurrency"]["group"]
        for expression in (
            f"github.event.comment.body == '{COMMAND}'",
            f"startsWith(github.event.comment.body, '{COMMAND} ')",
            f"startsWith(github.event.comment.body, '{COMMAND}\\n')",
            f"startsWith(github.event.comment.body, '{COMMAND}\\r')",
        ):
            self.assertIn(expression, condition)
            self.assertIn(expression, group)

    def test_compiled_workflow_preserves_the_source_predicate(self):
        lock = yaml.safe_load(
            (WORKFLOWS / f"{WORKFLOW_NAME}.lock.yml").read_text(encoding="utf-8")
        )
        compiled = next(
            step
            for step in lock["jobs"]["fetch-binlog"]["steps"]
            if step.get("id") == "command"
        )
        self.assertEqual(
            compiled["with"]["script"].strip(),
            self.check["with"]["script"].strip(),
        )


if __name__ == "__main__":
    unittest.main()
