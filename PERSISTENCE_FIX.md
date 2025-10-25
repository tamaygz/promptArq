# Data Persistence Fix

## Root Cause Analysis

The persistence issues were caused by **stale closure problems** and **race conditions** when using `useKV` hooks. When update handlers referenced the current state values directly instead of using functional updates, they would capture stale values from when the component first rendered, leading to data loss.

## Key Problems Fixed

### 1. Team Member Invite Links Not Persisting
**Problem:** When users joined a team via invite link, the `handleTeamInvite` function in `App.tsx` was trying to access `teams` and `teamMembers` state before they were loaded from KV storage, and updates weren't being properly persisted.

**Solution:** 
- Changed to directly read from `spark.kv.get()` to ensure we have the latest data
- Use `spark.kv.set()` to explicitly save the updated members array first
- Then update the React state to trigger re-render with the same data

```typescript
// Before (WRONG - race condition with useKV initialization):
setTeamMembers(current => [...(current || []), newMember])

// After (CORRECT - explicit KV write ensures persistence):
const allMembers = await window.spark.kv.get<TeamMember[]>('team-members') || []
const updatedMembers = [...allMembers, newMember]
await window.spark.kv.set('team-members', updatedMembers)
setTeamMembers(updatedMembers)
```

### 2. All Dialog Components Not Using Functional Updates
**Problem:** Components like `TeamDialog`, `SystemPromptDialog`, `ModelConfigDialog`, etc. were passing arrays directly to update handlers, which could cause stale closure issues.

**Solution:** Updated all handlers to use functional updates:

```typescript
// Before (WRONG):
onUpdateTeams([...teams, newTeam])

// After (CORRECT):
onUpdateTeams((current) => [...current, newTeam])
```

### 3. Type Definitions Not Supporting Functional Updates
**Problem:** TypeScript types only accepted array values, not functional updates.

**Solution:** Updated all prop types to support both patterns:

```typescript
// Before:
onUpdate: (configs: ModelConfig[]) => void

// After:
onUpdate: (configs: ModelConfig[] | ((current: ModelConfig[]) => ModelConfig[])) => void
```

## Files Modified

1. **src/App.tsx**
   - Fixed `handleTeamInvite` to use direct KV operations
   - Ensures data is properly saved before updating React state

2. **src/components/TeamDialog.tsx**
   - Updated all handlers to use functional updates
   - Updated TypeScript types to support functional updates

3. **src/components/SystemPromptDialog.tsx**
   - Updated all handlers to use functional updates
   - Updated TypeScript types to support functional updates

4. **src/components/ModelConfigDialog.tsx**
   - Updated all handlers to use functional updates
   - Updated TypeScript types to support functional updates

5. **src/components/ProjectDialog.tsx**
   - Already using functional updates correctly (no changes needed)

## How useKV Works

The `useKV` hook from `@github/spark/hooks` provides persistent storage that survives page refreshes and republishing. It returns a tuple: `[value, setValue, deleteValue]`.

**Best Practice:**
```typescript
// ✅ ALWAYS use functional updates with useKV
setData((currentData) => {
  // currentData is guaranteed to be the latest value
  return [...currentData, newItem]
})

// ❌ NEVER reference state directly (stale closure risk)
setData([...data, newItem])
```

## Testing Checklist

To verify the fix:

1. ✅ Create a team and generate an invite link
2. ✅ Open the invite link in a new browser/incognito window
3. ✅ Verify the new member appears in the team members list
4. ✅ Refresh the page - member should still be there
5. ✅ Create/edit projects, categories, tags
6. ✅ Refresh the page - all data should persist
7. ✅ Create system prompts and model configs
8. ✅ Refresh the page - configurations should remain

## Additional Notes

- All data is stored in the Spark KV (key-value) store
- Data persists across sessions, page refreshes, and republishing
- The KV store is user-specific and project-specific
- Never use `localStorage` or `sessionStorage` - always use `useKV` or `spark.kv` directly
