import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import vm from 'node:vm'
import ts from '../../src/Hosco.Web/node_modules/typescript/lib/typescript.js'

// Exercise the actual private formatters without changing dashboard logic or copying their implementation.
const source = readFileSync(new URL('../../src/Hosco.Web/src/pages/DashboardPage.tsx', import.meta.url), 'utf8')
const ast = ts.createSourceFile('DashboardPage.tsx', source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX)
const declarations = ast.statements.filter(statement => ts.isVariableStatement(statement) &&
  statement.declarationList.declarations.some(declaration => ['money', 'percent'].includes(declaration.name.getText(ast))))
assert.equal(declarations.length, 2, 'Both dashboard formatters must be covered')
const compiled = ts.transpileModule(declarations.map(statement => statement.getText(ast)).join('\n') + '\nexport { money, percent }', {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
})
const scope = { exports: {}, Intl }
vm.runInNewContext(compiled.outputText, scope)
const { money, percent } = scope.exports

test('undefined-denominator API null is displayed as N/A', () => {
  assert.equal(money(null), 'N/A')
  assert.equal(percent(null), 'N/A')
})
test('valid zero is not displayed as N/A', () => {
  assert.equal(money(0), new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(0))
  assert.equal(percent(0), '0%')
})
test('real AOV and margin retain current display convention', () => {
  assert.match(money(150779.6), /150\.780/)
  assert.equal(percent(43.7), '43,7%')
})
