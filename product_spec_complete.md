# Product Requirements Document (PRD): arqioly – Prompt Management Web App

## 1. Overview

arqioly is a collaborative webapp for managing, collecting, versioning, improving, and evaluating LLM prompts. It supports login via GitHub and Microsoft accounts, team-based sharing, project and category organization, tagging, versioning, comments, co-editing, and exposes all content via an API. A key feature is “Improve Prompt,” which runs prompts against a preconfigured LLM with a matching system prompt resolved by category, project, or tags.

## 2. Goals

- Centralize prompt lifecycle: creation, organization, versioning, evaluation, and collaboration.
- Streamline team workflows for prompt reuse, quality, and governance.
- Enable automated improvements via a configurable “Improve Prompt” action.
- Provide a comprehensive API to integrate with external tooling and CI/CD.

## 3. Non-Goals

- Not a full experiment-tracking platform (basic run tracking is included, but no hyperparameter sweeps or model training).
- Not a marketplace for public prompt sharing (private/team sharing only in MVP).
- No built-in billing or model cost management in MVP (capture basic run metadata only).

## 4. Personas

- Prompt Engineer: creates and iterates on prompts; uses tags, versions, and the improve button.
- Product Manager: organizes prompts by project/category; reviews versions and comments.
- Developer: integrates the API into apps and CI; retrieves and evaluates prompts.
- Team Admin: manages teams, roles, SSO, provider configs, and system prompts.

## 5. Key Use Cases & User Stories

- As a user, I can sign in with GitHub or Microsoft to access the app.
- As a team member, I can create a project, categories, and tags to organize prompts.
- As a prompt engineer, I can create/edit prompts and save versions with change notes.
- As a collaborator, I can co-edit prompts with presence/locking and comment on versions.
- As a user, I can click “Improve Prompt” to get an AI-suggested improved version and accept or reject it.
- As an admin, I can configure system prompts and model providers at team/project/category/tag level.
- As a developer, I can consume prompt content and metadata via an authenticated API.
- As a reviewer, I can view diffs between versions and leave feedback.
- As an admin, I can invite new users to my team with role-based permissions.

## 6. Scope: MVP vs. V2

- MVP
  - Auth via GitHub/Microsoft OAuth (OIDC).
  - Teams, invites, roles (Owner, Admin, Editor, Viewer).
  - Projects; Project-scoped Categories; Team-scoped Tags.
  - Prompts with versioning, comments, basic co-edit lock, presence.
  - Improve Prompt with resolved system prompt (prompt > project > category > tag > default).
  - REST API for all entities; API tokens and scopes; webhooks.
  - Basic search/filtering (by text, tag, project/category).
  - Audit log of critical actions.
- V2+
  - Real-time CRDT/OT collaborative editing.
  - Granular permissions per project/category.
  - Prompt evaluations/test cases; A/B comparisons.
  - GraphQL API; role templates; SSO org mapping.
  - Public sharing links and template gallery.
  - Usage analytics dashboards and cost tracking.

## 7. Functional Requirements

### 7.1 Authentication & Authorization
- OAuth/OIDC login via:
  - GitHub
  - Microsoft (Azure AD / Microsoft account)
- Session management with refresh tokens; CSRF protection.
- RBAC roles: Owner, Admin, Editor, Viewer.
  - Owner: team-level superuser.
  - Admin: manage members, projects, provider configs.
  - Editor: create/edit prompts, versions, comments.
  - Viewer: read-only access to permitted projects.
- API Tokens
  - Personal (user-scoped) and Team tokens with fine-grained scopes (read:projects, read:prompts, write:prompts, admin:team, etc.).

### 7.2 Teams and Sharing
- Create team, manage members, assign roles.
- Invites via email or shareable tokenized link (configurable expiry, role).
- Project-level visibility: private (team only) or team-visible.
- Optional project membership overrides (e.g., limit to subset of team).

### 7.3 Organization Model
- Team: top-level entity.
- Project: container for prompts, categories, and configs; belongs to a team.
- Category: taxonomy within a project; prompts may belong to one category.
- Tags: many-to-many; team-scoped to ensure reuse and consistent reporting.

### 7.4 Prompts & Versioning
- Prompt has title, description, category, tags, and schema for variables (JSON schema).
- Versioning:
  - Each save creates a new immutable version with diffable content and change note.
  - View diffs across versions; revert by creating a new version from a prior snapshot.
  - Keep snapshot of resolved system prompt and model config used for “Improve Prompt” or runs.
- Archive/unarchive prompts.

### 7.5 Collaboration
- Comments:
  - Threaded comments on prompts and/or specific versions.
  - Mentions (@user), markdown, and basic formatting.
- Co-editing:
  - MVP: soft locking + user presence.
  - V2: real-time CRDT/OT for concurrent edits with conflict resolution.

### 7.6 “Improve Prompt” Feature
- Action on a prompt (or a specific version) that:
  - Resolves the system prompt and model provider config according to precedence:
    1) Prompt-level override (if set)
    2) Project-level
    3) Category-level
    4) Tag-based mapping (first priority match wins; configurable priority on tags)
    5) Team default
  - Submits content to the preconfigured LLM.
  - Returns a suggested improved prompt; user can:
    - Accept: create new version with metadata linking the run.
    - Reject: discard or keep as draft.
- Track run metadata: model, parameters, tokens (if available), latency, status, cost estimates (optional if provider returns).

### 7.7 API Exposure
- REST API covering:
  - Teams, members, invites
  - Projects, categories, tags
  - Prompts, versions, comments
  - System prompts and provider configs
  - Runs (Improve Prompt executions)
  - Search/filter endpoints
  - API tokens and webhooks
- Pagination, filtering, sorting; standard error codes; rate limiting.
- Webhooks for events: prompt.created, prompt.version.created, comment.created, team.member.invited, run.completed.

### 7.8 Search & Filter
- Full-text search on prompt title/description/content.
- Filter by project, category, tag, author, last updated.
- Sort by updated_at, created_at, title.

### 7.9 Model Provider Configuration
- Support multiple providers per team (e.g., OpenAI, Azure OpenAI, Anthropic, local).
- Model configuration: provider, model name, parameters (temperature, max tokens), timeouts, retry policy.
- Secrets stored securely (KMS/Key Vault); not retrievable via API once saved (write-only secrets).
- Assign configs at project/category/tag/prompt level.

### 7.10 Security & Compliance
- Multi-tenant data isolation by team.
- Least-privilege access with scoped tokens.
- Audit logs for sensitive actions (auth, membership changes, config changes, prompt changes).
- Encryption at rest and in transit.
- Configurable data retention for runs and logs.

### 7.11 Notifications
- Email notifications for invites, mentions, comments on watched prompts.
- In-app notifications; notification preferences per user.

### 7.12 Internationalization & Accessibility
- i18n-ready strings; initial English localization.
- WCAG 2.1 AA compliant UI components.

## 8. Data Model (Conceptual)

- User(id, provider, provider_user_id, email, name, avatar_url, created_at)
- Team(id, name, slug, created_at)
- TeamMembership(user_id, team_id, role, created_at)
- Invite(id, team_id, email, role, token, expires_at, created_at, accepted_at)
- Project(id, team_id, name, key, description, visibility, created_by, created_at, updated_at)
- Category(id, project_id, name, description, sort_order, created_at)
- Tag(id, team_id, name, color, priority, created_at)
- Prompt(id, project_id, category_id, title, description, is_archived, created_by, created_at, updated_at)
- PromptTag(prompt_id, tag_id)
- PromptVersion(id, prompt_id, version_number, content, variables_schema_json, change_note, created_by, created_at, resolved_system_prompt_snapshot, resolved_model_config_snapshot_json)
- Comment(id, prompt_id, version_id nullable, user_id, body, reply_to_id, created_at, updated_at)
- SystemPrompt(id, team_id, scope_type enum[prompt, project, category, tag, team_default], scope_id, content, priority, created_by, created_at, updated_at)
- ModelProviderConfig(id, team_id, name, provider, model_name, parameters_json, secret_ref, created_by, created_at, updated_at)
- Run(id, prompt_version_id, model_provider_config_id, inputs_json, output_text, tokens_in, tokens_out, latency_ms, status enum[pending, running, succeeded, failed], error_message, created_by, created_at)
- ApiToken(id, owner_type enum[user, team], owner_id, token_hash, scopes_csv, created_by, created_at, last_used_at)
- Webhook(id, team_id, target_url, secret, events_csv, is_active, created_by, created_at, last_delivery_at)
- AuditLog(id, team_id, actor_user_id, action, target_type, target_id, metadata_json, created_at)
- PresenceLock(id, prompt_id, user_id, acquired_at, expires_at)

Notes:
- resolved_system_prompt_snapshot and resolved_model_config_snapshot_json on PromptVersion capture the exact configuration used to create that version (including from “Improve Prompt”).
- Tag.priority supports tag precedence in resolver.

## 9. System Prompt Resolution Algorithm

Given a prompt context:
1) If prompt has a direct system prompt override, use it.
2) Else if project-level system prompt exists, use it.
3) Else if category-level system prompt exists, use it.
4) Else if any tag-level system prompts exist:
   - Sort tag-level prompts by Tag.priority DESC then creation date DESC.
   - Choose the first match.
5) Else use team default system prompt (if defined).
6) If none found, use a global platform default.

Model provider configuration resolution mirrors the same precedence.

## 10. API Design (Representative Endpoints)

- Auth
  - POST /auth/oauth/github/callback
  - POST /auth/oauth/microsoft/callback
  - POST /auth/logout
- Teams
  - GET /teams
  - POST /teams
  - GET /teams/{teamId}
  - GET /teams/{teamId}/members
  - POST /teams/{teamId}/invites
  - POST /teams/{teamId}/api-tokens
- Projects
  - GET /teams/{teamId}/projects
  - POST /teams/{teamId}/projects
  - GET /projects/{projectId}
  - PATCH /projects/{projectId}
- Categories
  - GET /projects/{projectId}/categories
  - POST /projects/{projectId}/categories
- Tags
  - GET /teams/{teamId}/tags
  - POST /teams/{teamId}/tags
- Prompts
  - GET /projects/{projectId}/prompts?categoryId=&tagIds=&q=
  - POST /projects/{projectId}/prompts
  - GET /prompts/{promptId}
  - PATCH /prompts/{promptId}
  - POST /prompts/{promptId}/archive
  - GET /prompts/{promptId}/versions
  - POST /prompts/{promptId}/versions
  - GET /prompts/{promptId}/versions/{versionId}
  - POST /prompts/{promptId}/improve
- Comments
  - GET /prompts/{promptId}/comments
  - POST /prompts/{promptId}/comments
- System Prompts
  - GET /teams/{teamId}/system-prompts
  - POST /teams/{teamId}/system-prompts
- Provider Configs
  - GET /teams/{teamId}/provider-configs
  - POST /teams/{teamId}/provider-configs
- Runs
  - GET /runs/{runId}
  - GET /prompts/{promptId}/runs
- Search
  - GET /search?teamId=&projectId=&q=&tags=
- Webhooks
  - POST /teams/{teamId}/webhooks
  - GET /teams/{teamId}/webhooks
- Audit
  - GET /teams/{teamId}/audit-logs

Standards:
- Auth via Bearer token for API.
- Pagination: cursor or page/limit.
- Errors: RFC 7807 (problem+json).
- Idempotency keys for writes.

## 11. UI/UX Requirements

- Global nav: Team switcher, Projects, Tags, Settings.
- Project home: categories list, prompt list with filters (category, tag, text).
- Prompt editor:
  - Title, description, category, tags.
  - Content editor with variable placeholders and schema editor.
  - Version history panel with diff and revert.
  - Comments pane with threads and mentions.
  - Improve Prompt button with run status, preview result, Accept/Reject.
- Settings:
  - Team members and invites.
  - Provider configs and system prompts with scope selection and precedence info.
  - API tokens and webhooks.
- Presence indicators and soft lock banners in editor.

## 12. Performance & Reliability

- P95 Improve Prompt run latency target: < 8s (excluding provider service degradation).
- API P95 < 300ms for cached reads, < 800ms for uncached list endpoints.
- Rate limiting per token and per IP; burst limits.
- Background job queue for Improve Prompt; retries with exponential backoff.
- Circuit breakers for provider outages; graceful degradation.

## 13. Security

- OAuth/OIDC best practices; short-lived session tokens; refresh tokens.
- Secret management via KMS/Azure Key Vault; never return raw secrets.
- Row-level authorization checks for all endpoints.
- Webhook signing (HMAC) and delivery retry with DLQ.
- Audit critical actions.

## 14. Analytics & Metrics

- Core metrics:
  - DAU/MAU, active teams
  - Prompts created, versions per prompt
  - Improve Prompt runs per day, acceptance rate
  - Comments, collaborators per prompt
  - API requests by scope, errors, rate limit hits
- Funnel:
  - Sign up → team create → project create → prompt create → first improve run → first version accept
- Quality:
  - Average versions until “approved” tag
  - Revert frequency

## 15. Acceptance Criteria (MVP)

- Users can sign in via GitHub or Microsoft and create/join a team.
- Admin can create a project, categories, tags; invite members and assign roles.
- Users can create prompts, add tags, assign a category, and save multiple versions.
- Users can view version diffs and comments; mentions send notifications.
- Improve Prompt resolves system prompt and provider config according to precedence and produces a suggested new version; accepting creates a new version with snapshot metadata.
- All entities accessible via REST API with scoped tokens.
- Audit logs recorded for auth, membership, prompt changes, and provider config changes.

## 16. Risks & Mitigations

- Provider outages → circuit breakers, retries, fallbacks, status pages.
- Prompt data leakage across teams → enforce strict multi-tenant RBAC and data isolation tests.
- Secrets exposure → write-only secrets, encrypted storage, never log secrets.
- Conflicts in co-edit → MVP locking; plan CRDT in V2.

## 17. Dependencies

- OAuth providers: GitHub, Microsoft.
- LLM providers (configurable): OpenAI/Azure/Anthropic/etc.
- Email service for invites/notifications.
- Queue system (e.g., Redis/Cloud queue).
- Observability (logs, metrics, tracing).

## 18. Open Questions

- Should categories be global to team or scoped to project? Proposed: Project-scoped (MVP).
- Do we need per-project roles in MVP? Proposed: team RBAC only; optional project membership in MVP if time permits.
- Should Improve Prompt be synchronous in UI? Proposed: async with background job; show spinner and notify when ready.
- Do we store model outputs for non-“Improve” executions via API? Proposed: MVP tracks Improve runs; general execution runs could be V2.

## 19. Rollout Plan

- Private alpha with 2–3 teams.
- Harden auth, RBAC, and data isolation with penetration test.
- Expand to beta; collect feedback on Improve Prompt and versioning UX.
- Public GA after stability and performance targets are met.

## 20. Future Enhancements

- Test cases and evaluation metrics for prompts.
- Role templates and permission matrix per project/category.
- GraphQL API and SDKs.
- Public templates and sharing links.
- Cost tracking dashboards across providers.
- Real-time collaborative editing (CRDT/OT).

## 21. Glossary

- Prompt: The user-authored instruction string with optional variable placeholders.
- Version: Immutable snapshot of a prompt at a point in time.
- System Prompt: Instruction prepended to guide LLM behavior, resolved by precedence.
- Improve Prompt: Action that asks an LLM to suggest a better version.
- Run: An execution record of an LLM action (e.g., Improve Prompt).

## Design Direction

The design should feel **professional, trustworthy, and efficient** - like a tool built by developers for developers. Clean typography, generous whitespace, and purposeful use of color communicate quality. The interface is **minimal and functional** - every element serves the core purpose of managing prompts effectively.

## Color Selection

**Analogous blue palette** - Professional blues with warm accents for actions, creating trust and focus.

- **Primary Color**: `oklch(0.55 0.22 250)` - Vibrant blue (#3366FF family) communicates action, trust, and primary CTAs
- **Secondary Colors**: 
  - Light blue `oklch(0.92 0.02 250)` for subtle backgrounds
  - Medium blue `oklch(0.75 0.12 250)` for hover states
- **Accent Color**: `oklch(0.55 0.22 250)` - Same as primary for cohesion, used for buttons and interactive elements
- **Foreground/Background Pairings**:
  - Background (Light gray `oklch(0.98 0 0)`): Dark text `oklch(0.25 0 0)` - Ratio 14.2:1 ✓
  - Card (White `oklch(1 0 0)`): Dark text `oklch(0.25 0 0)` - Ratio 15.4:1 ✓
  - Primary (Blue `oklch(0.55 0.22 250)`): White text `oklch(1 0 0)` - Ratio 6.8:1 ✓
  - Accent (Blue `oklch(0.55 0.22 250)`): White text `oklch(1 0 0)` - Ratio 6.8:1 ✓
  - Muted (Light gray `oklch(0.96 0 0)`): Medium gray text `oklch(0.50 0 0)` - Ratio 4.5:1 ✓