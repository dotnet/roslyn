#!/usr/bin/env bash
#region Find all csproj files
# Finds all test project files in the `src` folder of the repo.
# Edits each line to be of the form `[]($1)` where $1 is the original line.
# This allows each line to be clicked as a link to the relevant project file in VS Code
# Results are stored in a file named out.md for later reference.
# Test projects are defined as ones ending in `.UnitTests` or `.IntegrationTests`
# or ones that include <IsTestProject>true</IsTestProject>.
# Test project definition comes from BuildBoss:
# src/Tools/BuildBoss/ProjectData.cs.
{
	find ../src -type f \( -iname '*.UnitTests.csproj' -o -iname '*.IntegrationTests.csproj' -o -iname '*.UnitTests.vbproj' -o -iname '*.IntegrationTests.vbproj' \)
	find ../src -type f \( -iname '*.csproj' -o -iname '*.vbproj' \) -exec grep -IlE '<IsTestProject>[[:space:]]*true[[:space:]]*</IsTestProject>' {} +
} | sort -u | sed 's|^\(.*\)$|[](\1)|' > out.md
#endregion Find all csproj files

#region Summarize test project count
# Summarizes test project count by top-level folder
# Prints into Markdown table and formats table
sed -n 's|^\[\](../src/\([^/]*\)/.*)$|\1|p' out.md |
	awk '{ projects[$0]++ } END { for (folder in projects) printf "%s\t%d\n", folder, projects[folder] }' |
	sort |
	awk -F '\t' '
		{
			folders[NR] = $1
			projects[NR] = $2
			if (length($1) > folder_width) folder_width = length($1)
			if (length($2) > project_width) project_width = length($2)
		}
		END {
			if (folder_width < length("Folder")) folder_width = length("Folder")
			if (project_width < length("Projects")) project_width = length("Projects")

			folder_rule = project_rule = ""
			for (i = 0; i < folder_width + 2; i++) folder_rule = folder_rule "-"
			for (i = 0; i < project_width + 1; i++) project_rule = project_rule "-"

			printf "| %-*s | %*s |\n", folder_width, "Folder", project_width, "Projects"
			printf "|%s|%s:|\n", folder_rule, project_rule
			for (i = 1; i <= NR; i++)
				printf "| %-*s | %*s |\n", folder_width, folders[i], project_width, projects[i]
		}' > summary.md
#endregion Summarize