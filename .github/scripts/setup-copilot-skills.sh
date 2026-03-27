#!/usr/bin/env bash

set -euo pipefail

skills_root="${HOME}/.copilot/skills"
temp_root="${RUNNER_TEMP:-$(mktemp -d)}"
repo_root="${temp_root%/}/dotnet-skills"

cleanup() {
  rm -rf "$repo_root"
}

trap cleanup EXIT

mkdir -p "$skills_root"

git clone --depth 1 --filter=blob:none --sparse https://github.com/dotnet/skills.git "$repo_root"
git -C "$repo_root" sparse-checkout set plugins

declare -A seen_skill_names=()
installed_count=0

while IFS= read -r -d '' skill_dir; do
  skill_name="$(basename "$skill_dir")"

  if [[ -n "${seen_skill_names[$skill_name]+x}" ]]; then
    echo "Duplicate skill directory name '$skill_name' found in:" >&2
    echo "  ${seen_skill_names[$skill_name]}" >&2
    echo "  $skill_dir" >&2
    exit 1
  fi

  seen_skill_names["$skill_name"]="$skill_dir"

  destination="$skills_root/$skill_name"
  rm -rf "$destination"
  cp -R "$skill_dir" "$destination"
  ((installed_count += 1))
done < <(find "$repo_root/plugins" -mindepth 3 -maxdepth 3 -type d -path '*/skills/*' -print0 | sort -z)

echo "Installed ${installed_count} skills from dotnet/skills into ${skills_root}."
