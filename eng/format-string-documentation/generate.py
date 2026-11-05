#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

"""Extract hover descriptions from pinned Microsoft documentation without paraphrases."""
import argparse
import hashlib
import html
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from xml.sax.saxutils import escape

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
RESX = ROOT / 'src/Features/CSharp/Portable/CSharpFeaturesResources.resx'
GENERATED = ROOT / 'src/Features/CSharp/Portable/QuickInfo/CSharpFormatStringDocumentation.g.cs'
SOURCES = json.loads((HERE / 'sources.json').read_text())
META = {s['file']: s for s in SOURCES}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def source(file):
    return (HERE / 'sources' / file).read_text(encoding='utf-8-sig')


def plain(text):
    """Remove presentation markup, without rewriting source words or punctuation."""
    text = re.sub(r'<br\s*/?>', ' ', text, flags=re.I)
    text = re.sub(r'<xref:([^>?*]+)[^>]*>', lambda m: m[1], text)
    text = re.sub(r'\[([^\]]+)\]\([^)]*\)', r'\1', text)
    text = text.replace('`', '').replace('*', '')
    text = re.sub(r'\\([\\#$%&{}\[\]()*+.!_>\-])', r'\1', text)
    return ' '.join(html.unescape(text).split())


def tables(text):
    result, rows = [], []
    for line in text.splitlines() + ['']:
        if line.startswith('|'):
            rows.append([x.strip() for x in re.split(r'(?<!\\)\|', line.strip()[1:-1])])
        elif rows:
            if len(rows) >= 2 and all(re.fullmatch(r':?-+:?', x) for x in rows[1]):
                headers = [plain(x) for x in rows[0]]
                for row in rows[2:]:
                    require(len(row) == len(headers), (headers, row))
                result.append([dict(zip(headers, row)) for row in rows[2:]])
            rows = []
    return result


def first_table(file):
    return tables(source(file))[0]


def aliases(cell):
    value = plain(cell)
    if value == '"dd"-"dddddddd"':
        return ['d' * n for n in range(2, 9)]
    return re.findall(r'"([^"\n]+)"', value)


entries = []


def add(family, keys, file, selector, raw, rule='before-more-information'):
    if rule == 'before-more-information':
        raw = re.split(r'More information\s*:', raw, maxsplit=1, flags=re.I)[0]
    elif rule == 'first-break':
        raw = re.split(r'<br\s*/?>', raw, maxsplit=1, flags=re.I)[0]
    elif rule == 'before-example':
        raw = raw.split('The following example', 1)[0]
    elif rule != 'whole':
        raise ValueError(rule)
    text = plain(raw)
    require(text, (family, keys, selector))
    key = 'FormatDoc_' + family.replace('-', '_') + '_' + '_'.join(f'u{ord(c):04X}' for c in keys[0])
    meta = META[file]
    entries.append(dict(resource=key, family=family, specifiers=keys, text=text,
                        source=file, selector=selector, transform=rule,
                        commit=meta['commit'], git_blob=meta['git_blob'],
                        url=f"https://github.com/{meta['repository']}/blob/{meta['commit']}/{meta['path']}"))


for file, family in [('numeric-standard.md', 'numeric-standard'), ('numeric-custom.md', 'numeric-custom'),
                     ('date-standard.md', 'date-standard'), ('date-custom.md', 'date-custom'),
                     ('duration-standard.md', 'duration-standard'), ('duration-custom.md', 'duration-custom')]:
    for row_index, row in enumerate(first_table(file)):
        specifier = row['Format specifier']
        keys = aliases(specifier)
        if not keys:
            keys = {';': [';'], '%': ['%'], '&#92;': ['escape'], 'Other': ['literal'],
                    'Any other character': ['literal']}.get(specifier, [])
        if 'string' in plain(specifier):
            keys = ['quoted']
        if keys and keys[0] in ('\\', '\\\\'):
            keys = ['escape']
        if not keys or 'Any other single' in specifier:
            continue
        selector = {'table': 0, 'row': row_index, 'specifier': specifier, 'column': 'Description'}
        raw = row['Description']
        if family == 'numeric-standard':
            fields = {}
            for field in re.split(r'<br\s*/?>', raw, flags=re.I):
                if ':' in field:
                    label, value = field.strip().split(':', 1)
                    fields[label] = value.strip()
            require('Result' in fields and 'Precision specifier' in fields, 'Invalid documentation source structure')
            add(family, keys, file, selector | {'field': 'Result'}, fields['Result'], 'whole')
            add('numeric-precision', keys, file, selector | {'field': 'Precision specifier'}, fields['Precision specifier'], 'whole')
            if 'B' in keys:
                add('numeric-support', keys, file, selector | {'field': 'Supported by'}, fields['Supported by'], 'whole')
        else:
            add(family, keys, file, selector, raw)

heading = '### Floating-point hex formatting and parsing'
paragraph = source('net11-libraries.md').split(heading + '\n', 1)[1].strip().split('\n\n')[0]
add('numeric-hex-float', ['X', 'x'], 'net11-libraries.md',
    {'heading': heading, 'paragraph': 0, 'sentence': 0}, re.split(r'(?<=\.)\s+', paragraph)[0], 'whole')

# Select the XML overload before its embedded Markdown table.
guid = ET.parse(HERE / 'sources/Guid.xml').getroot()
members = [m for m in guid.findall('./Members/Member')
           if m.attrib['MemberName'] == 'ToString' and
           [p.attrib['Type'] for p in m.findall('./Parameters/Parameter')] == ['System.String']]
require(len(members) == 1, 'Invalid documentation source structure')
remarks = ''.join(members[0].find('./Docs/remarks').itertext())
for row_index, row in enumerate(tables(remarks)[0]):
    add('guid', [plain(row['Specifier'])], 'Guid.xml',
        {'xml_member': 'ToString', 'parameters': ['System.String'], 'element': 'Docs/remarks',
         'table': 0, 'row': row_index, 'column': 'Format of return value'},
        row['Format of return value'], 'first-break')

for specifier in 'GFDX':
    heading = f'## {specifier} or {specifier.lower()}'
    paragraphs = source('enum.md').split(heading + '\n', 1)[1].strip().split('\n\n')
    add('enum', [specifier, specifier.lower()], 'enum.md',
        {'heading': heading, 'paragraph': 0}, paragraphs[0], 'before-example')

row = next(row for row in first_table('interpolation.md') if row.get('Element') == '`interpolationExpression`')
null_sentence = [s for s in re.split(r'(?<=\.)\s+', row['Description']) if s.startswith('When the expression is `null`')]
require(len(null_sentence) == 1, 'Invalid documentation source structure')
add('interpolation', ['null'], 'interpolation.md',
    {'table': 0, 'row_key': 'interpolationExpression', 'column': 'Description', 'sentence_prefix': 'When the expression is `null`'},
    null_sentence[0], 'whole')

for family, key, file, prefix in [
    ('numeric-special', 'G0', 'numeric-standard.md', 'If the precision specifier is omitted or zero,'),
    ('date-custom', 'extended-year', 'date-custom.md', 'If there are additional "y" specifiers,')]:
    matches = [s for s in re.split(r'(?<=\.)\s+', source(file)) if s.startswith(prefix)]
    require(len(matches) == 1, (file, prefix, matches))
    add(family, [key], file, {'sentence_prefix': prefix}, matches[0], 'whole')

require(len({e['resource'] for e in entries}) == len(entries), 'Invalid documentation source structure')
lookup = [(e['family'], k) for e in entries for k in e['specifiers']]
require(len(set(lookup)) == len(lookup), 'Invalid documentation source structure')


def generated_outputs():
    current = RESX.read_text(encoding='utf-8-sig')
    current = re.sub(r'  <data name="FormatDoc_[^"]+".*?</data>\n', '', current, flags=re.S)
    block = ''
    for e in entries:
        comment = 'Generated by eng/format-string-documentation/generate.py; CC-BY-4.0. ' + e['url'] + ' Selector: ' + json.dumps(e['selector'], ensure_ascii=False)
        block += f'  <data name="{e["resource"]}" xml:space="preserve">\n    <value>{escape(e["text"])}</value>\n    <comment>{escape(comment)}</comment>\n  </data>\n'
    resx = current.replace('</root>', block + '</root>')
    code = '// Licensed to the .NET Foundation under one or more agreements.\n// The .NET Foundation licenses this file to you under the MIT license.\n// See the LICENSE file in the project root for more information.\n\n// <auto-generated/>\n#nullable enable\n// Regenerate with: python3 eng/format-string-documentation/generate.py\n\nnamespace Microsoft.CodeAnalysis.CSharp.QuickInfo;\n\ninternal static class CSharpFormatStringDocumentation\n{\n    public static string? Get(string family, string specifier)\n        => (family, specifier) switch\n        {\n'
    for e in entries:
        for key in e['specifiers']:
            code += f'            ({json.dumps(e["family"])}, {json.dumps(key)}) => CSharpFeaturesResources.{e["resource"]},\n'
    code += '            _ => null,\n        };\n}\n'
    return {RESX: ('\ufeff' + resx).encode('utf-8'), GENERATED: ('\ufeff' + code).encode('utf-8'),
            HERE / 'extractions.json': (json.dumps(entries, indent=2, ensure_ascii=False) + '\n').encode()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Verify snapshots and generated output without writes.')
    args = parser.parse_args()
    for s in SOURCES:
        data = (HERE / 'sources' / s['file']).read_bytes()
        blob = hashlib.sha1(b'blob ' + str(len(data)).encode() + b'\0' + data).hexdigest()
        require(blob == s['git_blob'], f'Source snapshot changed: {s["file"]}')
    allowed = {'Format_example_heading', 'Format_preview_heading', 'Format_0_for_1', 'Format_example_omitted', 'Microsoft_Learn_format_strings'}
    for node in ET.parse(RESX).getroot().findall('data'):
        name = node.attrib['name']
        require(not (name.startswith(('Format_', 'Guid_format_', 'This_is_also_the_default_Guid', 'A_null_value')) and name not in allowed), f'Unverified description: {name}')
    for path, data in generated_outputs().items():
        if args.check:
            require(path.read_bytes() == data, f'Generated output changed: {path.relative_to(ROOT)}')
        else:
            path.write_bytes(data)
    consumer = (GENERATED.parent / 'CSharpFormatStringInfo.cs').read_text(encoding='utf-8-sig')
    require(set(re.findall(r'CSharpFeaturesResources\.([A-Za-z0-9_]+)', consumer)) <= {'Format_example_omitted'}, 'Invalid documentation source structure')
    require(not re.search(r'(?<!CSharp)FeaturesResources\.', consumer), 'Unverified Roslyn completion text remains')
    print(f'Verified {len(entries)} direct documentation extracts, {len(lookup)} lookups, {len(SOURCES)} pinned source files.')


if __name__ == '__main__':
    main()
