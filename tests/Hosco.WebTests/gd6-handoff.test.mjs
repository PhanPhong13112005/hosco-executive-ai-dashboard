import assert from 'node:assert/strict'
import { existsSync, readFileSync, readdirSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import test from 'node:test'

const root = path.resolve(fileURLToPath(new URL('../..', import.meta.url)))

test('GD6 public guides have no filled student identity fields or API key/JWT patterns', () => {
  const folder = path.join(root, 'docs/gd6')
  const files = readdirSync(folder).filter(f => f.endsWith('.md'))
  assert.ok(files.length >= 19)
  for (const file of files) {
    const text = readFileSync(path.join(folder, file), 'utf8')
    assert.doesNotMatch(text, /(?:MSSV|Mã số sinh viên)\s*:\s*\d+|GVHD\s*:\s*\S+|(?:Lớp|Họ tên)\s*:\s*\S+/u)
    assert.doesNotMatch(text, /sk-(?:proj-)?[A-Za-z0-9_-]{24,}|AIza[A-Za-z0-9_-]{30,}|eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/)
  }
})

test('GD6 README and guides link to existing local deliverables', () => {
  const paths = ['README.md', ...readdirSync(path.join(root, 'docs/gd6')).filter(f => f.endsWith('.md')).map(f => 'docs/gd6/' + f)]
  for (const file of paths) {
    const text = readFileSync(path.join(root, file), 'utf8')
    for (const [, target] of text.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
      if (/^(https?:|#)/.test(target)) continue
      assert.ok(existsSync(path.resolve(root, path.dirname(file), target.split('#')[0])), file + ': ' + target)
    }
  }
})

test('security patch pins both crypto libraries without disabling vulnerability audit', () => {
  const xml = readFileSync(path.join(root, 'src/Hosco.Infrastructure/Hosco.Infrastructure.csproj'), 'utf8')
  for (const name of ['Pkcs', 'Xml']) assert.match(xml, new RegExp('System\\.Security\\.Cryptography\\.' + name + '" Version="10\\.0\\.13"'))
  assert.doesNotMatch(xml, /<NuGetAudit>false|<NoWarn>[^<]*NU190/)
})
