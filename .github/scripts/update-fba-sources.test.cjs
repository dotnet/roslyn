// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const assert = require('node:assert/strict');
const childProcess = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { test } = require('node:test');
const { discover, fingerprint, isSuppressed, roslynVersion, sdkRoslynVersion, selectSdk, synchronize } = require('./update-fba-sources.cjs');

const sourceRoot = 'src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms';
const commitIdPath = 'src/Workspaces/CSharp/Portable/SyncedSource/commitid.txt';
const sdkCommit = 'a'.repeat(40);
const previousCommit = 'b'.repeat(40);
const context = { repo: { owner: 'dotnet', repo: 'roslyn' } };
const versions = '<Project><MajorVersion>5</MajorVersion><MinorVersion>13</MinorVersion><PatchVersion>1</PatchVersion></Project>';

function temporaryDirectory(t) {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'roslyn-fba-test-'));
  t.after(() => fs.rmSync(directory, { recursive: true, force: true }));
  return directory;
}

function outputs() {
  const values = {};
  return { values, core: { info() {}, warning() {}, setOutput(name, value) { values[name] = value; } } };
}

function sdk(branch, version = '5.13') {
  return { branch, version, commit: sdkCommit };
}

test('reads Roslyn major/minor independently of patch and prerelease versions', () => {
  assert.equal(roslynVersion(versions), '5.13');
  assert.equal(sdkRoslynVersion('<Dependency Version="5.13.0-1.123" Name="Microsoft.CodeAnalysis.CSharp" />'), '5.13');
  assert.equal(sdkRoslynVersion('<Dependency Name="Microsoft.CodeAnalysis.CSharp" Version="5.13.2" />'), '5.13');
  assert.throws(() => roslynVersion('<Project />'), /Cannot read Roslyn/);
  assert.throws(() => sdkRoslynVersion('<Dependency Name="Microsoft.CodeAnalysis.CSharp.Features" Version="5.13.0" />'), /Cannot read Microsoft/);
});

test('prefers SDK main for Roslyn main, but a release SDK for servicing', () => {
  const sdks = [sdk('release/11.0.2xx'), sdk('main')];
  assert.equal(selectSdk('main', '5.13', sdks).branch, 'main');
  assert.equal(selectSdk('release/stable', '5.13', sdks).branch, 'release/11.0.2xx');
  assert.equal(selectSdk('release/stable', '5.13', [sdk('main')]).branch, 'main');
});

test('selects the newest matching feature band, preferring its non-snapshot branch', () => {
  const sdks = [
    sdk('release/11.0.1xx-rc2'), sdk('release/10.0.4xx'),
    sdk('release/11.0.1xx'), sdk('release/11.0.1xx-rc10'),
    sdk('release/11.0.2xx', '5.12'),
  ];
  assert.equal(selectSdk('release/stable', '5.13', sdks).branch, 'release/11.0.1xx');
  sdks.push(sdk('release/11.0.2xx-preview1'));
  assert.equal(selectSdk('release/stable', '5.13', sdks).branch, 'release/11.0.2xx-preview1');
});

test('uses an RC snapshot when it alone matches the Roslyn version', () => {
  const sdks = [sdk('main'), sdk('release/11.0.1xx', '5.12'), sdk('release/11.0.1xx-rc1', '5.11')];
  assert.equal(selectSdk('release/stable', '5.11', sdks).branch, 'release/11.0.1xx-rc1');
  assert.equal(selectSdk('main', '5.14', sdks), undefined);
});

function discoveryFixture() {
  const branches = [
    { name: 'main', commit: { sha: 'roslyn-main' } },
    { name: 'release/stable', commit: { sha: 'roslyn-stable' } },
    { name: 'release/old', commit: { sha: 'roslyn-old' } },
    { name: 'release/unmatched', commit: { sha: 'roslyn-unmatched' } },
    { name: 'feature/test', commit: { sha: 'roslyn-feature' } },
  ];
  const files = new Map([
    ['roslyn/roslyn-main/eng/Versions.props', versions],
    ['roslyn/roslyn-stable/eng/Versions.props', versions.replace('<MinorVersion>13', '<MinorVersion>11')],
    ['roslyn/roslyn-unmatched/eng/Versions.props', versions.replace('<MinorVersion>13', '<MinorVersion>99')],
    ['sdk/sdk-main/eng/Version.Details.xml', '<Dependency Name="Microsoft.CodeAnalysis.CSharp" Version="5.13.0-1.123" />'],
    ['sdk/sdk-rc/eng/Version.Details.xml', '<Dependency Name="Microsoft.CodeAnalysis.CSharp" Version="5.11.0-1.123" />'],
  ]);
  for (const ref of ['roslyn-main', 'roslyn-stable', 'roslyn-unmatched']) {
    files.set(`roslyn/${ref}/${commitIdPath}`, previousCommit);
  }

  const github = {
    rest: { repos: {
      listBranches() {},
      async getContent({ repo, path: file, ref }) {
        const key = `${repo}/${ref}/${file}`;
        if (!files.has(key)) {
          throw Object.assign(new Error(`Not found: ${key}`), { status: 404 });
        }

        return { data: { type: 'file', encoding: 'base64', content: Buffer.from(files.get(key)).toString('base64') } };
      },
    } },
    async paginate(route, { repo, per_page }) {
      assert.equal(per_page, 100);
      return repo === 'roslyn' ? branches : [
        { name: 'main', commit: { sha: 'sdk-main' } },
        { name: 'release/11.0.1xx-rc1', commit: { sha: 'sdk-rc' } },
        { name: 'release/9.0.1xx', commit: { sha: 'unused' } },
        { name: 'release/dnup', commit: { sha: 'unused' } },
      ];
    },
  };
  return { github };
}

test('discovers main and historical release branches with FBA, pinning SDK commits', async () => {
  const { github } = discoveryFixture();
  const { core, values } = outputs();
  const include = await discover({ github, context, core });
  assert.deepEqual(include, [
    { branch: 'main', version: '5.13', sdkBranch: 'main', sdkCommit: 'sdk-main' },
    { branch: 'release/stable', version: '5.11', sdkBranch: 'release/11.0.1xx-rc1', sdkCommit: 'sdk-rc' },
    { branch: 'release/unmatched', version: '5.99', sdkBranch: '', sdkCommit: '' },
  ]);
  assert.deepEqual(values.matrix, { include });
});

test('does not mistake API authorization or rate-limit errors for missing FBA sources', async () => {
  for (const status of [403, 429, 500]) {
    const { github } = discoveryFixture();
    const original = github.rest.repos.getContent;
    github.rest.repos.getContent = async args => {
      if (args.repo === 'roslyn') {
        throw Object.assign(new Error('API failed'), { status });
      }

      return original(args);
    };
    await assert.rejects(discover({ github, context, core: outputs().core }), /API failed/);
  }
});

test('fingerprints source contents and names, normalizing line endings and ignoring translations', t => {
  const directory = temporaryDirectory(t);
  fs.writeFileSync(path.join(directory, 'Helper.cs'), 'first\r\nsecond\r\n');
  const original = fingerprint(directory);
  fs.writeFileSync(path.join(directory, 'Helper.cs'), 'first\nsecond\n');
  fs.mkdirSync(path.join(directory, 'xlf'));
  fs.writeFileSync(path.join(directory, 'xlf', 'Resources.de.xlf'), 'translation');
  assert.equal(fingerprint(directory), original);
  fs.writeFileSync(path.join(directory, '.editorconfig'), 'root = false');
  assert.notEqual(fingerprint(directory), original);
  fs.unlinkSync(path.join(directory, '.editorconfig'));
  fs.renameSync(path.join(directory, 'Helper.cs'), path.join(directory, 'Renamed.cs'));
  assert.notEqual(fingerprint(directory), original);
  fs.unlinkSync(path.join(directory, 'Renamed.cs'));
  assert.throws(() => fingerprint(directory), /No synced FBA sources/);
});

test('suppresses only the exact snapshot from an unmerged closed PR', () => {
  const snapshot = 'a'.repeat(64);
  const body = `Description\n<!-- fba-source-snapshot: ${snapshot} -->`;
  assert.equal(isSuppressed([{ body, merged_at: null }], snapshot), true);
  assert.equal(isSuppressed([{ body, merged_at: '2026-10-19' }], snapshot), false);
  assert.equal(isSuppressed([{ body, merged_at: null }], 'b'.repeat(64)), false);
  assert.equal(isSuppressed([{ body: null, merged_at: null }], snapshot), false);
});

function syncFixture(t) {
  const targetDirectory = temporaryDirectory(t);
  const sources = path.join(targetDirectory, sourceRoot);
  fs.mkdirSync(sources, { recursive: true });
  fs.mkdirSync(path.join(targetDirectory, 'eng'));
  fs.writeFileSync(path.join(targetDirectory, 'eng', 'Versions.props'), versions);
  fs.writeFileSync(path.join(sources, 'Helper.cs'), 'old source');
  fs.writeFileSync(path.join(sources, '..', 'commitid.txt'), previousCommit);
  const { core, values } = outputs();
  const pulls = [];
  const github = {
    rest: { pulls: { list() {} } },
    async paginate(route, args) {
      assert.equal(args.base, 'main');
      assert.equal(args.head, 'dotnet:automation/fba/main');
      assert.equal(args.state, 'closed');
      assert.equal(args.per_page, 100);
      return pulls;
    },
  };
  const options = { github, context, core, targetDirectory, branch: 'main', version: '5.13', sdkBranch: 'main', sdkCommit };
  return { options, sources, pulls, values };
}

test('ignores commit-only updates and invokes the trusted script with strict failure handling', async t => {
  const { options, values } = syncFixture(t);
  t.mock.method(childProcess, 'execFileSync', (command, args, settings) => {
    assert.equal(command, 'dotnet');
    assert.equal(path.basename(args[2]), 'ensure-sources-synced.cs');
    assert.deepEqual(args.slice(3), ['--', '--update', '--repo-root', options.targetDirectory, '--fail-on-rate-limit']);
    assert.equal(settings.cwd, path.resolve(__dirname, '..', '..'));
  });
  await synchronize(options);
  assert.equal(values.changed, false);
  assert.equal(values.body, undefined);
});

test('emits a content-specific PR marker and SDK provenance for changed sources', async t => {
  const { options, sources, values } = syncFixture(t);
  t.mock.method(childProcess, 'execFileSync', () => fs.writeFileSync(path.join(sources, 'Helper.cs'), 'new source'));
  await synchronize(options);
  assert.equal(values.changed, true);
  assert.equal(values.head, 'automation/fba/main');
  assert.ok(values.body.includes(`<!-- fba-source-snapshot: ${fingerprint(sources)} -->`));
  assert.ok(values.body.includes(`https://github.com/dotnet/sdk/compare/${previousCommit}...${sdkCommit}`));
});

test('a closed PR suppresses identical sources even after an unrelated SDK commit', async t => {
  const { options, sources, values, pulls } = syncFixture(t);
  const helper = path.join(sources, 'Helper.cs');
  fs.writeFileSync(helper, 'declined source');
  pulls.push({ body: `<!-- fba-source-snapshot: ${fingerprint(sources)} -->`, merged_at: null });
  fs.writeFileSync(helper, 'old source');
  t.mock.method(childProcess, 'execFileSync', () => fs.writeFileSync(helper, 'declined source'));
  await synchronize({ ...options, sdkCommit: 'c'.repeat(40) });
  assert.equal(values.changed, false);
});

test('a later source change is eligible after closing an earlier update', async t => {
  const { options, sources, values, pulls } = syncFixture(t);
  pulls.push({ body: `<!-- fba-source-snapshot: ${fingerprint(sources)} -->`, merged_at: null });
  t.mock.method(childProcess, 'execFileSync', () => fs.writeFileSync(path.join(sources, 'Helper.cs'), 'new source'));
  await synchronize(options);
  assert.equal(values.changed, true);
});

test('sync failures cannot produce a partial-update PR', async t => {
  const { options, sources, values } = syncFixture(t);
  t.mock.method(childProcess, 'execFileSync', () => {
    fs.writeFileSync(path.join(sources, 'Helper.cs'), 'partial source');
    throw new Error('Sync failed');
  });
  await assert.rejects(synchronize(options), /Sync failed/);
  assert.equal(values.changed, undefined);
});

test('fails explicitly for unmatched SDKs and target versions changed since discovery', async t => {
  const { options } = syncFixture(t);
  t.mock.method(childProcess, 'execFileSync', () => assert.fail('Sync should not run'));
  await assert.rejects(synchronize({ ...options, sdkBranch: '', sdkCommit: '' }), /No matching SDK/);
  await assert.rejects(synchronize({ ...options, version: '5.12' }), /changed since discovery/);
});
