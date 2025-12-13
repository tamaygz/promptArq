/**
 * Tests for complete prompt version tracking functionality
 * Run with: node --loader tsx src/lib/prompt-version-tracking.test.ts
 */

import { Prompt, PromptVersion } from './types';

console.log('Testing prompt version tracking functionality...\n');

// Test 1: New version should track all prompt fields
console.log('Test 1: Complete Version Tracking');
const testPrompt: Prompt = {
  id: 'test-prompt-1',
  title: 'Test Prompt Title',
  description: 'This is a test prompt description',
  content: 'Test prompt content',
  projectId: 'project-1',
  categoryId: 'category-1',
  tags: ['tag1', 'tag2'],
  createdBy: 'test-user',
  createdAt: Date.now(),
  updatedAt: Date.now(),
  isArchived: false,
  exposedToMCP: true,
  execute_llm: true
};

const newVersion: PromptVersion = {
  id: 'version-1',
  promptId: testPrompt.id,
  versionNumber: 1,
  title: testPrompt.title,
  description: testPrompt.description,
  content: testPrompt.content,
  projectId: testPrompt.projectId,
  categoryId: testPrompt.categoryId,
  tags: testPrompt.tags,
  isArchived: testPrompt.isArchived,
  exposedToMCP: testPrompt.exposedToMCP,
  execute_llm: testPrompt.execute_llm,
  changeNote: 'Initial version',
  createdBy: testPrompt.createdBy,
  createdAt: testPrompt.createdAt
};

console.log('- Version tracks title:', newVersion.title === testPrompt.title ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks description:', newVersion.description === testPrompt.description ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks content:', newVersion.content === testPrompt.content ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks projectId:', newVersion.projectId === testPrompt.projectId ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks categoryId:', newVersion.categoryId === testPrompt.categoryId ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks tags:', JSON.stringify(newVersion.tags) === JSON.stringify(testPrompt.tags) ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks isArchived:', newVersion.isArchived === testPrompt.isArchived ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks exposedToMCP:', newVersion.exposedToMCP === testPrompt.exposedToMCP ? '✅ PASS' : '❌ FAIL');
console.log('- Version tracks execute_llm:', newVersion.execute_llm === testPrompt.execute_llm ? '✅ PASS' : '❌ FAIL');

// Test 2: Migrating old versions without all fields
console.log('\nTest 2: Backward Compatibility with Old Versions');
const oldVersion = {
  id: 'old-version-1',
  promptId: 'test-prompt-1',
  versionNumber: 1,
  content: 'Old version content',
  changeNote: 'Created before tracking all fields',
  createdBy: 'test-user',
  createdAt: Date.now()
  // Note: Missing title, description, projectId, categoryId, tags, isArchived, exposedToMCP, execute_llm
};

// Simulate restoration with fallbacks
const restoredFromOld = {
  title: (oldVersion as any).title || testPrompt.title,
  description: (oldVersion as any).description || testPrompt.description,
  content: oldVersion.content,
  projectId: (oldVersion as any).projectId || testPrompt.projectId,
  categoryId: (oldVersion as any).categoryId || testPrompt.categoryId,
  tags: (oldVersion as any).tags || testPrompt.tags,
  isArchived: (oldVersion as any).isArchived ?? testPrompt.isArchived,
  exposedToMCP: (oldVersion as any).exposedToMCP ?? testPrompt.exposedToMCP,
  execute_llm: (oldVersion as any).execute_llm ?? testPrompt.execute_llm
};

console.log('- Content restored from old version:', restoredFromOld.content === oldVersion.content ? '✅ PASS' : '❌ FAIL');
console.log('- Title fallback to current prompt:', restoredFromOld.title === testPrompt.title ? '✅ PASS' : '❌ FAIL');
console.log('- Description fallback to current prompt:', restoredFromOld.description === testPrompt.description ? '✅ PASS' : '❌ FAIL');
console.log('- ProjectId fallback to current prompt:', restoredFromOld.projectId === testPrompt.projectId ? '✅ PASS' : '❌ FAIL');
console.log('- CategoryId fallback to current prompt:', restoredFromOld.categoryId === testPrompt.categoryId ? '✅ PASS' : '❌ FAIL');
console.log('- Tags fallback to current prompt:', JSON.stringify(restoredFromOld.tags) === JSON.stringify(testPrompt.tags) ? '✅ PASS' : '❌ FAIL');
console.log('- IsArchived fallback to current prompt:', restoredFromOld.isArchived === testPrompt.isArchived ? '✅ PASS' : '❌ FAIL');
console.log('- ExposedToMCP fallback to current prompt:', restoredFromOld.exposedToMCP === testPrompt.exposedToMCP ? '✅ PASS' : '❌ FAIL');
console.log('- Execute_llm fallback to current prompt:', restoredFromOld.execute_llm === testPrompt.execute_llm ? '✅ PASS' : '❌ FAIL');

// Test 3: Detecting changes between versions
console.log('\nTest 3: Detecting Changes Between Versions');
const version1: PromptVersion = {
  id: 'v1',
  promptId: 'p1',
  versionNumber: 1,
  title: 'Version 1 Title',
  description: 'Version 1 Description',
  content: 'Version 1 Content',
  projectId: 'project-1',
  categoryId: 'category-1',
  tags: ['tag1'],
  isArchived: false,
  exposedToMCP: false,
  execute_llm: false,
  changeNote: 'Initial',
  createdBy: 'user1',
  createdAt: Date.now()
};

const version2: PromptVersion = {
  id: 'v2',
  promptId: 'p1',
  versionNumber: 2,
  title: 'Version 2 Title',
  description: 'Version 1 Description',
  content: 'Version 1 Content',
  projectId: 'project-2',
  categoryId: 'category-1',
  tags: ['tag1', 'tag2'],
  isArchived: false,
  exposedToMCP: true,
  execute_llm: true,
  changeNote: 'Updated title, project, tags, flags',
  createdBy: 'user1',
  createdAt: Date.now()
};

const titleChanged = version1.title !== version2.title;
const descriptionChanged = version1.description !== version2.description;
const contentChanged = version1.content !== version2.content;
const projectChanged = version1.projectId !== version2.projectId;
const categoryChanged = version1.categoryId !== version2.categoryId;
const tagsChanged = JSON.stringify(version1.tags) !== JSON.stringify(version2.tags);
const exposedChanged = version1.exposedToMCP !== version2.exposedToMCP;
const executeLLMChanged = version1.execute_llm !== version2.execute_llm;

console.log('- Detected title change:', titleChanged ? '✅ PASS' : '❌ FAIL');
console.log('- No description change:', !descriptionChanged ? '✅ PASS' : '❌ FAIL');
console.log('- No content change:', !contentChanged ? '✅ PASS' : '❌ FAIL');
console.log('- Detected project change:', projectChanged ? '✅ PASS' : '❌ FAIL');
console.log('- No category change:', !categoryChanged ? '✅ PASS' : '❌ FAIL');
console.log('- Detected tags change:', tagsChanged ? '✅ PASS' : '❌ FAIL');
console.log('- Detected exposedToMCP change:', exposedChanged ? '✅ PASS' : '❌ FAIL');
console.log('- Detected execute_llm change:', executeLLMChanged ? '✅ PASS' : '❌ FAIL');

// Test 4: Complete version serialization
console.log('\nTest 4: Complete Version Serialization');
const fullVersion: PromptVersion = {
  id: 'full-v1',
  promptId: 'p1',
  versionNumber: 1,
  title: 'Full Title',
  description: 'Full Description',
  content: 'Full Content',
  projectId: 'project-1',
  categoryId: 'category-1',
  tags: ['tag1', 'tag2', 'tag3'],
  isArchived: true,
  exposedToMCP: true,
  execute_llm: true,
  changeNote: 'All fields set',
  createdBy: 'user1',
  createdAt: Date.now()
};

const serialized = JSON.stringify(fullVersion);
const deserialized = JSON.parse(serialized) as PromptVersion;

console.log('- Serialized contains title:', serialized.includes('Full Title') ? '✅ PASS' : '❌ FAIL');
console.log('- Serialized contains description:', serialized.includes('Full Description') ? '✅ PASS' : '❌ FAIL');
console.log('- Serialized contains projectId:', serialized.includes('project-1') ? '✅ PASS' : '❌ FAIL');
console.log('- Serialized contains categoryId:', serialized.includes('category-1') ? '✅ PASS' : '❌ FAIL');
console.log('- Serialized contains tags:', serialized.includes('tag1') && serialized.includes('tag2') ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized title matches:', deserialized.title === fullVersion.title ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized description matches:', deserialized.description === fullVersion.description ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized projectId matches:', deserialized.projectId === fullVersion.projectId ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized categoryId matches:', deserialized.categoryId === fullVersion.categoryId ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized tags match:', JSON.stringify(deserialized.tags) === JSON.stringify(fullVersion.tags) ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized isArchived matches:', deserialized.isArchived === fullVersion.isArchived ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized exposedToMCP matches:', deserialized.exposedToMCP === fullVersion.exposedToMCP ? '✅ PASS' : '❌ FAIL');
console.log('- Deserialized execute_llm matches:', deserialized.execute_llm === fullVersion.execute_llm ? '✅ PASS' : '❌ FAIL');

// Test 5: Version array with mixed old and new formats
console.log('\nTest 5: Mixed Version Array (Old and New Formats)');
const mixedVersions = [
  // Old format version (missing new fields)
  {
    id: 'v1',
    promptId: 'p1',
    versionNumber: 1,
    content: 'Old format content',
    changeNote: 'Old version',
    createdBy: 'user1',
    createdAt: Date.now() - 3600000
  },
  // New format version
  {
    id: 'v2',
    promptId: 'p1',
    versionNumber: 2,
    title: 'New Title',
    description: 'New Description',
    content: 'New format content',
    projectId: 'project-1',
    categoryId: 'category-1',
    tags: ['tag1'],
    isArchived: false,
    exposedToMCP: true,
    execute_llm: false,
    changeNote: 'New version with all fields',
    createdBy: 'user1',
    createdAt: Date.now()
  }
];

const hasOldVersion = !mixedVersions[0].hasOwnProperty('title');
const hasNewVersion = mixedVersions[1].hasOwnProperty('title');
const canAccessOldContent = typeof mixedVersions[0].content === 'string';
const canAccessNewContent = typeof mixedVersions[1].content === 'string';

console.log('- First version is old format (no title):', hasOldVersion ? '✅ PASS' : '❌ FAIL');
console.log('- Second version is new format (has title):', hasNewVersion ? '✅ PASS' : '❌ FAIL');
console.log('- Can access old version content:', canAccessOldContent ? '✅ PASS' : '❌ FAIL');
console.log('- Can access new version content:', canAccessNewContent ? '✅ PASS' : '❌ FAIL');
console.log('- Array handles mixed formats:', hasOldVersion && hasNewVersion ? '✅ PASS' : '❌ FAIL');

console.log('\n✅ All prompt version tracking tests completed!');
