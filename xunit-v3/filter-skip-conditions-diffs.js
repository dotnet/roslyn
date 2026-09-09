#!/usr/bin/env node

/**
 * Runs on a diff file, e.g. output of
 * ```
 * git log 38d6e7b1eba..6dc2b0a43a0 -p --unified=0 > out.diff
 * ```
 * 
 * Removes all hunks where the only change is the addition of the string:
 * `skipConditions: `
 * (backticks excluded, trailing space included, case-sensitive).
 * 
 * Includes debug logging.
 * 
 * Usage: node ${basename(process.argv[1])} INPUT_DIFF [OUTPUT_DIFF] [DEBUG_LOG]
 * Paths of OUTPUT_DIFF and DEBUG_LOG must not be the same.
 */

import { readFileSync, writeFileSync } from "node:fs";
import { basename, resolve } from "node:path";

const [, , inputPath, outputPath, debugLogPath] = process.argv;

if (!inputPath || process.argv.length > 5) {
	console.error(
		`Usage: node ${basename(process.argv[1])} INPUT_DIFF [OUTPUT_DIFF] [DEBUG_LOG]`,
	);
	process.exit(2);
}

const inputFullPath = resolve(inputPath);
const debugLogFullPath = resolve(
	debugLogPath ?? `${outputPath ?? inputPath}.debug.log`,
);
const text = readFileSync(inputFullPath, "utf8");
const newline = text.includes("\r\n")
	? "\r\n"
	: text.includes("\n")
		? "\n"
		: "\r";
const hasFinalNewline = text.endsWith("\r") || text.endsWith("\n");
const lines = text.split(/\r\n|\n|\r/);
const output = [];
const debugLog = [];

function analyzeHunk(hunk) {
	const removed = [];
	const added = [];

	for (const line of hunk.slice(1)) {
		if (line.startsWith("-")) {
			removed.push(line.slice(1));
		} else if (line.startsWith("+")) {
			added.push(line.slice(1));
		}
	}

	if (removed.length === 0) {
		return {
			remove: false,
			reason: "The hunk has no removed lines.",
		};
	}

	if (removed.length !== added.length) {
		return {
			remove: false,
			reason: `The number of removed lines (${removed.length}) does not match the number of added lines (${added.length}).`,
		};
	}

	for (let index = 0; index < added.length; index++) {
		const addedLine = added[index];
		const removedLine = removed[index];

		if (!addedLine.includes("skipConditions: ")) {
			return {
				remove: false,
				reason: `Added line ${index + 1} does not contain "skipConditions: ": ${JSON.stringify(addedLine)}`,
			};
		}

		const normalizedAddedLine = addedLine.replaceAll("skipConditions: ", "");
		if (normalizedAddedLine !== removedLine) {
			return {
				remove: false,
				reason: [
					`Added line ${index + 1} has changes beyond "skipConditions: ".`,
					`  Removed:    ${JSON.stringify(removedLine)}`,
					`  Normalized: ${JSON.stringify(normalizedAddedLine)}`,
				].join(newline),
			};
		}
	}

	return {
		remove: true,
		reason: `All ${added.length} changed line pair(s) differ only by the insertion of "skipConditions: ".`,
	};
}

function logDecision(fileHeader, hunkHeader, analysis) {
	debugLog.push(
		`${analysis.remove ? "REMOVED" : "PRESERVED"}: ${fileHeader}`,
		`  Hunk: ${hunkHeader}`,
		`  Reason: ${analysis.reason.replaceAll(newline, `${newline}  `)}`,
		"",
	);
}

let lineIndex = 0;

while (
	lineIndex < lines.length &&
	!lines[lineIndex].startsWith("diff --git ")
) {
	output.push(lines[lineIndex]);
	lineIndex++;
}

while (lineIndex < lines.length) {
	const fileDiffStart = lineIndex;
	lineIndex++;

	while (
		lineIndex < lines.length &&
		!lines[lineIndex].startsWith("diff --git ")
	) {
		lineIndex++;
	}

	const fileDiff = lines.slice(fileDiffStart, lineIndex);
	const firstHunk = fileDiff.findIndex((line) => line.startsWith("@@ "));

	if (firstHunk === -1) {
		output.push(...fileDiff);
		debugLog.push(
			`PRESERVED: ${fileDiff[0]}`,
			"  Reason: This file diff has no ordinary hunks to analyze.",
			"",
		);
		continue;
	}

	const keptHunks = [];
	let hunkStart = firstHunk;

	while (hunkStart < fileDiff.length) {
		let nextHunk = hunkStart + 1;

		while (
			nextHunk < fileDiff.length &&
			!fileDiff[nextHunk].startsWith("@@ ")
		) {
			nextHunk++;
		}

		const hunk = fileDiff.slice(hunkStart, nextHunk);
		const analysis = analyzeHunk(hunk);
		logDecision(fileDiff[0], hunk[0], analysis);

		if (!analysis.remove) {
			keptHunks.push(hunk);
		}

		hunkStart = nextHunk;
	}

	if (keptHunks.length > 0) {
		output.push(...fileDiff.slice(0, firstHunk));
		for (const hunk of keptHunks) {
			output.push(...hunk);
		}
	}
}

const removedHunkCount = debugLog.filter((line) =>
	line.startsWith("REMOVED:"),
).length;
const preservedHunkCount = debugLog.filter((line) =>
	line.startsWith("PRESERVED:"),
).length;

debugLog.unshift(
	`Input: ${inputFullPath}`,
	`Removed hunks: ${removedHunkCount}`,
	`Preserved hunks or non-hunk file diffs: ${preservedHunkCount}`,
	"",
);

let result = output.join(newline);
if (hasFinalNewline && !result.endsWith(newline)) {
	result += newline;
}

if (debugLogFullPath === inputFullPath) {
	throw new Error("The debug log path must be different from the input path.");
}

if (outputPath) {
	const outputFullPath = resolve(outputPath);
	if (outputFullPath === inputFullPath) {
		throw new Error("Input and output paths must be different.");
	}

	if (debugLogFullPath === outputFullPath) {
		throw new Error("The debug log path must be different from the output path.");
	}

	writeFileSync(outputFullPath, result);
} else {
	process.stdout.write(result);
}

writeFileSync(debugLogFullPath, `${debugLog.join(newline)}${newline}`);
