// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const { createHash } = require('node:crypto');
const childProcess = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const syncedRoot = 'src/Workspaces/CSharp/Portable/SyncedSource';
const sourceRoot = `${syncedRoot}/FileBasedPrograms`;

function roslynVersion(xml) {
  const major = xml.match(/<MajorVersion>(\d+)<\/MajorVersion>/)?.[1];
  const minor = xml.match(/<MinorVersion>(\d+)<\/MinorVersion>/)?.[1];
  if (!major || !minor) {
    throw new Error('Cannot read Roslyn major/minor version from eng/Versions.props.');
  }

  return `${major}.${minor}`;
}

function sdkRoslynVersion(xml) {
  const dependency = xml.match(/<Dependency\b[^>]*\bName="Microsoft\.CodeAnalysis\.CSharp"[^>]*>/)?.[0];
  const version = dependency?.match(/\bVersion="(\d+\.\d+)\.\d+[^"]*"/)?.[1];
  if (!version) {
    throw new Error('Cannot read Microsoft.CodeAnalysis.CSharp version from SDK eng/Version.Details.xml.');
  }

  return version;
}

async function readFile(github, owner, repo, file, ref, optional = false) {
  let data;
  try {
    ({ data } = await github.rest.repos.getContent({ owner, repo, path: file, ref }));
  } catch (error) {
    if (optional && error.status === 404) {
      return null;
    }

    throw error;
  }

  if (data.type !== 'file' || data.encoding !== 'base64' || typeof data.content !== 'string') {
    throw new Error(`Expected a file at ${owner}/${repo}/${file}@${ref}.`);
  }

  return Buffer.from(data.content, 'base64').toString('utf8');
}

function selectSdk(branch, version, sdks) {
  const matches = sdks.filter(sdk => sdk.version === version);
  matches.sort((left, right) => {
    if (left.branch === 'main' || right.branch === 'main') {
      const mainFirst = branch === 'main' ? -1 : 1;
      return left.branch === 'main' ? mainFirst : -mainFirst;
    }

    const leftBand = left.branch.split('-')[0];
    const rightBand = right.branch.split('-')[0];
    const bandOrder = rightBand.localeCompare(leftBand, 'en', { numeric: true });
    if (bandOrder !== 0) {
      return bandOrder;
    }

    // Prefer a maintained feature-band branch over its preview/RC snapshots.
    if (left.branch === leftBand || right.branch === rightBand) {
      return left.branch === leftBand ? -1 : 1;
    }

    return right.branch.localeCompare(left.branch, 'en', { numeric: true });
  });

  return matches[0];
}

async function discover({ github, context, core }) {
  const branches = await github.paginate(github.rest.repos.listBranches, { ...context.repo, per_page: 100 });
  const sdkBranches = await github.paginate(github.rest.repos.listBranches, { owner: 'dotnet', repo: 'sdk', per_page: 100 });
  const sdks = [];
  for (const { name, commit } of sdkBranches) {
    const release = name.match(/^release\/(\d+)\.\d+\.\dxx(?:-(?:preview|rc)\.?\d+)?$/);
    // File-based apps first shipped in .NET 10.
    if (name !== 'main' && (!release || Number(release[1]) < 10)) {
      continue;
    }

    const xml = await readFile(github, 'dotnet', 'sdk', 'eng/Version.Details.xml', commit.sha);
    sdks.push({ branch: name, commit: commit.sha, version: sdkRoslynVersion(xml) });
  }

  const include = [];
  for (const { name, commit } of branches) {
    if (name !== 'main' && !name.startsWith('release/')) {
      continue;
    }

    if (await readFile(github, context.repo.owner, context.repo.repo, `${syncedRoot}/commitid.txt`, commit.sha, true) === null) {
      core.info(`Skipping ${name}: no synced FBA sources.`);
      continue;
    }

    const version = roslynVersion(await readFile(github, context.repo.owner, context.repo.repo, 'eng/Versions.props', commit.sha));
    const sdk = selectSdk(name, version, sdks);
    if (!sdk) {
      core.warning(`No SDK branch references Roslyn ${version} for ${name}. Its update job will fail rather than use another version.`);
    }

    include.push({ branch: name, version, sdkBranch: sdk?.branch ?? '', sdkCommit: sdk?.commit ?? '' });
  }

  if (include.length === 0 || include.length > 256) {
    throw new Error(`Expected between 1 and 256 FBA target branches, found ${include.length}.`);
  }

  core.setOutput('matrix', { include });
  return include;
}

function fingerprint(directory) {
  const entries = fs.readdirSync(directory, { withFileTypes: true })
    .filter(entry => entry.isFile() && (entry.name.endsWith('.cs') || entry.name.endsWith('.resx') || entry.name === '.editorconfig'))
    .map(entry => [entry.name, fs.readFileSync(path.join(directory, entry.name), 'utf8').replace(/\r\n?/g, '\n')])
    .sort(([left], [right]) => left.localeCompare(right, 'en'));
  if (entries.length === 0) {
    throw new Error(`No synced FBA sources found in ${directory}.`);
  }

  return createHash('sha256').update(JSON.stringify(entries)).digest('hex');
}

function marker(snapshot) {
  return `<!-- fba-source-snapshot: ${snapshot} -->`;
}

function isSuppressed(pulls, snapshot) {
  return pulls.some(pull => !pull.merged_at && pull.body?.includes(marker(snapshot)));
}

async function synchronize({ github, context, core, targetDirectory, branch, version, sdkBranch, sdkCommit }) {
  if (!sdkBranch || !/^[a-f0-9]{40}$/.test(sdkCommit)) {
    throw new Error(`No matching SDK branch was found for ${branch} (Roslyn ${version}).`);
  }

  const target = path.resolve(targetDirectory);
  if (roslynVersion(fs.readFileSync(path.join(target, 'eng', 'Versions.props'), 'utf8')) !== version) {
    throw new Error(`Roslyn version on ${branch} changed since discovery. Rerun this workflow.`);
  }

  const sources = path.join(target, sourceRoot);
  const before = fingerprint(sources);
  const commitFile = path.join(target, syncedRoot, 'commitid.txt');
  const previousCommit = fs.readFileSync(commitFile, 'utf8').trim();
  fs.writeFileSync(commitFile, sdkCommit);
  childProcess.execFileSync('dotnet', [
    'run', '--file', path.resolve(__dirname, '..', '..', 'eng', 'ensure-sources-synced.cs'),
    '--', '--update', '--repo-root', target, '--fail-on-rate-limit',
  ], { cwd: path.resolve(__dirname, '..', '..'), stdio: 'inherit' });

  const snapshot = fingerprint(sources);
  if (snapshot === before) {
    core.info(`No FBA source changes for ${branch}; ignoring SDK commit-only changes.`);
    core.setOutput('changed', false);
    return;
  }

  const head = `automation/fba/${branch}`;
  const pulls = await github.paginate(github.rest.pulls.list, {
    ...context.repo, base: branch, head: `${context.repo.owner}:${head}`, state: 'closed', per_page: 100,
  });
  if (isSuppressed(pulls, snapshot)) {
    core.info(`Skipping previously declined FBA source snapshot ${snapshot} for ${branch}.`);
    core.setOutput('changed', false);
    return;
  }

  core.setOutput('changed', true);
  core.setOutput('head', head);
  core.setOutput('body', [
    `Update the synced file-based app sources for \`${branch}\` (Roslyn ${version}) from dotnet/sdk \`${sdkBranch}\`.`,
    '',
    `SDK commit: https://github.com/dotnet/sdk/commit/${sdkCommit}`,
    `Upstream changes: https://github.com/dotnet/sdk/compare/${previousCommit}...${sdkCommit}`,
    '',
    'Close this PR without merging to ignore this exact source snapshot. Unrelated SDK commits will not reopen it; new FBA source changes can produce another PR.',
    '',
    marker(snapshot),
  ].join('\n'));
}

module.exports = { discover, fingerprint, isSuppressed, roslynVersion, sdkRoslynVersion, selectSdk, synchronize };
