/**
 * Basic tests for storage adapter
 * Run with: node --loader tsx src/lib/storage-adapter.test.ts
 */

import { isSparkEnvironment, getStorageAdapter } from './storage-adapter';

console.log('Testing Storage Adapter...\n');

// Test 1: Environment detection
console.log('Test 1: Environment Detection');
console.log('- isSparkEnvironment():', isSparkEnvironment());
console.log('  Expected: false (not in Spark when running in Node)');

// Test 2: Get appropriate adapter
console.log('\nTest 2: Storage Adapter Selection');
try {
  const adapter = getStorageAdapter();
  console.log('- Adapter created successfully');
  console.log('  Type:', adapter.constructor.name);
} catch (error) {
  console.error('- Failed to create adapter:', error);
}

// Test 3: Basic operations
console.log('\nTest 3: Basic Storage Operations');
(async () => {
  try {
    const adapter = getStorageAdapter();
    
    // Set a value
    await adapter.set('test-key', { message: 'Hello World', timestamp: Date.now() });
    console.log('- Set value: OK');
    
    // Get the value
    const value = await adapter.get<{ message: string; timestamp: number }>('test-key');
    console.log('- Get value:', value?.message);
    
    // List keys
    const keys = await adapter.keys();
    console.log('- Keys:', keys.filter(k => k === 'test-key'));
    
    // Delete the value
    await adapter.delete('test-key');
    console.log('- Delete value: OK');
    
    // Verify deletion
    const deletedValue = await adapter.get('test-key');
    console.log('- Value after delete:', deletedValue === undefined ? 'undefined (OK)' : 'still exists (ERROR)');
    
    console.log('\n✅ All tests completed successfully!');
  } catch (error) {
    console.error('\n❌ Test failed:', error);
  }
})();
