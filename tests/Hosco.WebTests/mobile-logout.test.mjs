import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

test('mobile sidebar never hides the only logout control', () => {
  const css = readFileSync(new URL('../../src/Hosco.Web/src/styles.css', import.meta.url), 'utf8')
  const rules = [...css.matchAll(/([^{}]+)\{([^{}]*)\}/g)]
  const bottom = rules.filter(([, selector]) => selector.includes('.sidebar-bottom'))
  assert.ok(bottom.length > 0)
  for (const [, , declarations] of bottom) {
    assert.doesNotMatch(declarations, /display\s*:\s*none|visibility\s*:\s*hidden/)
  }
  assert.match(css, /\.sidebar-bottom\s*\{[^}]*display:\s*grid/)
  assert.match(css, /\.logout\s*\{[^}]*font-size:\s*\.65rem/)
})
