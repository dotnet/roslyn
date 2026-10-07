import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import textwrap
import unittest


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


def step_block(path, step_id, key):
    lines = path.read_text(encoding="utf-8").splitlines()
    step_index = next(
        index
        for index, line in enumerate(lines)
        if line.strip() == f"id: {step_id}"
    )
    key_index = next(
        index
        for index, line in enumerate(lines[step_index + 1 :], step_index + 1)
        if line.strip() == f"{key}: |"
    )
    key_indent = len(lines[key_index]) - len(lines[key_index].lstrip())
    block = []
    for line in lines[key_index + 1 :]:
        indent = len(line) - len(line.lstrip())
        if line.strip() and indent <= key_indent:
            break
        block.append(line)
    return textwrap.dedent("\n".join(block)).rstrip()


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
        cls.source_path = WORKFLOWS / f"{WORKFLOW_NAME}.md"
        cls.source = cls.source_path.read_text(encoding="utf-8")
        cls.check_script = step_block(cls.source_path, "command", "script")
        cls.permission_script = step_block(cls.source_path, "permission", "run")
        cls.bash = os.environ.get("BFA_BASH") or (
            r"C:\Program Files\Git\bin\bash.exe"
            if os.name == "nt"
            else shutil.which("bash")
        )
        if not cls.bash:
            raise RuntimeError("Bash is required.")

    def run_check(self, jobs, title="Build failure analysis command 123"):
        fixture = {
            "runs": [{"id": 9, "run_attempt": 2, "display_title": title}],
            "jobs": {"9:2": jobs},
            "script": self.check_script,
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
        for expression in (
            f"github.event.comment.body == '{COMMAND}'",
            f"startsWith(github.event.comment.body, '{COMMAND} ')",
            f"startsWith(github.event.comment.body, '{COMMAND}\\n')",
            f"startsWith(github.event.comment.body, '{COMMAND}\\r')",
        ):
            self.assertEqual(self.source.count(expression), 2)

    def test_collaborator_permissions_use_rest_api_values(self):
        for permission, expected in (
            ("admin", True),
            ("maintain", True),
            ("push", True),
            ("triage", False),
            ("pull", False),
            ("", False),
        ):
            with self.subTest(permission=permission):
                with tempfile.TemporaryDirectory(prefix="roslyn-bfa-permission-") as directory:
                    result = subprocess.run(
                        [self.bash, "--noprofile", "--norc", "-s"],
                        input=(
                            'gh() { printf \'{"permission":"%s"}\' "$PERMISSION"; }\n'
                            'jq() { printf \'%s\' "$PERMISSION"; }\n'
                            + self.permission_script
                        ),
                        text=True,
                        capture_output=True,
                        cwd=directory,
                        env={
                            **os.environ,
                            "COMMENT_BODY": COMMAND,
                            "COMMENTER": "maintainer",
                            "COMMAND_NAME": COMMAND.removeprefix("/"),
                            "GITHUB_OUTPUT": "outputs",
                            "GITHUB_REPOSITORY": "dotnet/roslyn",
                            "PERMISSION": permission,
                        },
                        timeout=15,
                    )
                    self.assertEqual(result.returncode, 0, result.stderr)
                    output = (Path(directory) / "outputs").read_text(encoding="utf-8")
                    self.assertIn(
                        f"authorized={str(expected).lower()}",
                        output,
                    )

    def test_compiled_workflow_preserves_the_source_predicate(self):
        compiled_script = step_block(
            WORKFLOWS / f"{WORKFLOW_NAME}.lock.yml",
            "command",
            "script",
        )
        self.assertEqual(
            compiled_script,
            self.check_script,
        )


if __name__ == "__main__":
    unittest.main()
