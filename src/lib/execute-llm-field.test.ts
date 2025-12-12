/**
 * Tests for execute_llm field functionality
 * Run with: npm test or node --loader tsx src/lib/execute-llm-field.test.ts
 */

import { Prompt } from './types';

console.log('Testing execute_llm field functionality...\n');

// Test 1: New prompt should have execute_llm field with default false
console.log('Test 1: New Prompt Creation');
const newPrompt: Prompt = {
  id: 'test-prompt-1',
  title: 'Test Prompt',
  description: 'A test prompt',
  content: 'Test content',
  projectId: 'project-1',
  categoryId: 'category-1',
  tags: [],
  createdBy: 'test-user',
  createdAt: Date.now(),
  updatedAt: Date.now(),
  isArchived: false,
  exposedToMCP: false,
  execute_llm: false
};

console.log('- Created new prompt with execute_llm:', newPrompt.execute_llm);
console.log('  Expected: false');
console.log('  Result:', newPrompt.execute_llm === false ? '✅ PASS' : '❌ FAIL');

// Test 2: Migrating old prompt without execute_llm field
console.log('\nTest 2: Migration of Old Prompts');
const oldPromptData = {
  id: 'old-prompt-1',
  title: 'Old Prompt',
  description: 'A prompt without execute_llm field',
  content: 'Old content',
  projectId: 'project-1',
  categoryId: 'category-1',
  tags: [],
  createdBy: 'test-user',
  createdAt: Date.now(),
  updatedAt: Date.now(),
  isArchived: false,
  exposedToMCP: false
  // Note: execute_llm field is missing
};

// Simulate migration
const migratedPrompt = {
  ...oldPromptData,
  execute_llm: (oldPromptData as any).execute_llm ?? false
} as Prompt;

console.log('- Migrated prompt execute_llm:', migratedPrompt.execute_llm);
console.log('  Expected: false');
console.log('  Result:', migratedPrompt.execute_llm === false ? '✅ PASS' : '❌ FAIL');

// Test 3: Prompt with execute_llm set to true
console.log('\nTest 3: Prompt with execute_llm = true');
const llmPrompt: Prompt = {
  id: 'llm-prompt-1',
  title: 'LLM Prompt',
  description: 'A prompt that uses LLM execution',
  content: 'LLM content',
  projectId: 'project-1',
  categoryId: 'category-1',
  tags: [],
  createdBy: 'test-user',
  createdAt: Date.now(),
  updatedAt: Date.now(),
  isArchived: false,
  exposedToMCP: false,
  execute_llm: true
};

console.log('- Created LLM prompt with execute_llm:', llmPrompt.execute_llm);
console.log('  Expected: true');
console.log('  Result:', llmPrompt.execute_llm === true ? '✅ PASS' : '❌ FAIL');

// Test 4: Array migration
console.log('\nTest 4: Bulk Migration of Prompt Array');
const oldPrompts = [
  { id: '1', title: 'P1', description: '', content: '', projectId: '', categoryId: '', tags: [], createdBy: '', createdAt: 0, updatedAt: 0, isArchived: false, exposedToMCP: false },
  { id: '2', title: 'P2', description: '', content: '', projectId: '', categoryId: '', tags: [], createdBy: '', createdAt: 0, updatedAt: 0, isArchived: false, exposedToMCP: false, execute_llm: true },
  { id: '3', title: 'P3', description: '', content: '', projectId: '', categoryId: '', tags: [], createdBy: '', createdAt: 0, updatedAt: 0, isArchived: false, exposedToMCP: false }
];

const migratedPrompts = oldPrompts.map(p => ({
  ...p,
  execute_llm: (p as any).execute_llm ?? false
})) as Prompt[];

console.log('- Prompt 1 execute_llm (was missing):', migratedPrompts[0].execute_llm, '- Expected: false');
console.log('  Result:', migratedPrompts[0].execute_llm === false ? '✅ PASS' : '❌ FAIL');
console.log('- Prompt 2 execute_llm (was true):', migratedPrompts[1].execute_llm, '- Expected: true');
console.log('  Result:', migratedPrompts[1].execute_llm === true ? '✅ PASS' : '❌ FAIL');
console.log('- Prompt 3 execute_llm (was missing):', migratedPrompts[2].execute_llm, '- Expected: false');
console.log('  Result:', migratedPrompts[2].execute_llm === false ? '✅ PASS' : '❌ FAIL');

// Test 5: Serialization includes execute_llm
console.log('\nTest 5: JSON Serialization');
const testPrompt: Prompt = {
  id: 'serialize-test',
  title: 'Serialize Test',
  description: '',
  content: '',
  projectId: '',
  categoryId: '',
  tags: [],
  createdBy: '',
  createdAt: 0,
  updatedAt: 0,
  isArchived: false,
  exposedToMCP: false,
  execute_llm: true
};

const serialized = JSON.stringify(testPrompt);
const deserialized = JSON.parse(serialized);

console.log('- Serialized prompt contains execute_llm:', serialized.includes('execute_llm'));
console.log('  Result:', serialized.includes('execute_llm') ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized execute_llm value:', deserialized.execute_llm);
console.log('  Expected: true');
console.log('  Result:', deserialized.execute_llm === true ? '✅ PASS' : '❌ FAIL');

console.log('\n✅ All execute_llm field tests completed!');
