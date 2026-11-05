# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

"""Exercise the provenance checks against isolated copies of the real inputs."""
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
FEATURE = Path('src/Features/CSharp/Portable')


class ProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.tool = self.root / 'eng/format-string-documentation'
        shutil.copytree(HERE, self.tool, ignore=shutil.ignore_patterns('__pycache__'))
        for relative in [FEATURE / 'CSharpFeaturesResources.resx',
                         FEATURE / 'QuickInfo/CSharpFormatStringDocumentation.g.cs',
                         FEATURE / 'QuickInfo/CSharpFormatStringInfo.cs']:
            dest = self.root / relative
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, dest)

    def check(self):
        return subprocess.run([sys.executable, str(self.tool / 'generate.py'), '--check'],
                              capture_output=True, text=True)

    def test_exact_inputs_pass(self):
        result = self.check()
        self.assertEqual(0, result.returncode, result.stderr)

    def test_modified_source_fails(self):
        with (self.tool / 'sources/numeric-standard.md').open('a') as output:
            output.write('\n')
        result = self.check()
        self.assertNotEqual(0, result.returncode)
        self.assertIn('Source snapshot changed', result.stderr)

    def test_rewritten_resource_fails(self):
        path = self.root / FEATURE / 'CSharpFeaturesResources.resx'
        text = path.read_text(encoding='utf-8-sig')
        self.assertIn('A binary string.', text)
        path.write_text(text.replace('A binary string.', 'An invented description.', 1), encoding='utf-8-sig')
        result = self.check()
        self.assertNotEqual(0, result.returncode)
        self.assertIn('Generated output changed', result.stderr)

    def test_modified_lookup_fails(self):
        path = self.root / FEATURE / 'QuickInfo/CSharpFormatStringDocumentation.g.cs'
        with path.open('a') as output:
            output.write('// Changed lookup\n')
        result = self.check()
        self.assertNotEqual(0, result.returncode)
        self.assertIn('Generated output changed', result.stderr)

    def test_new_manual_resource_fails(self):
        path = self.root / FEATURE / 'CSharpFeaturesResources.resx'
        text = path.read_text(encoding='utf-8-sig')
        text = text.replace('</root>', '<data name="Format_manual"><value>Invented text.</value></data></root>')
        path.write_text(text, encoding='utf-8-sig')
        result = self.check()
        self.assertNotEqual(0, result.returncode)
        self.assertIn('Unverified description', result.stderr)



if __name__ == '__main__':
    unittest.main()
