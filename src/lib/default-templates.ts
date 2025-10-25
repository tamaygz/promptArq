export type PromptTemplate = {
  title: string
  description: string
  content: string
  category: string
  tags: string[]
}

export const PROMPT_TEMPLATES: PromptTemplate[] = [
  {
    title: 'Blog Post Writer',
    description: 'Generate engaging blog posts on any topic with SEO optimization',
    content: `Write a comprehensive blog post about {{topic}}.

Requirements:
- Length: {{word_count}} words
- Tone: {{tone}}
- Target audience: {{audience}}
- Include relevant examples and statistics
- Optimize for SEO with proper headings (H2, H3)
- Include a compelling introduction and conclusion
- Add actionable takeaways`,
    category: 'Marketing',
    tags: ['content', 'seo', 'writing']
  },
  {
    title: 'Social Media Caption Generator',
    description: 'Create engaging social media captions for various platforms',
    content: `Create {{count}} engaging social media captions for {{platform}}.

Context: {{context}}
Tone: {{tone}}
Include relevant hashtags and emojis
Keep it concise and attention-grabbing
Add a call-to-action`,
    category: 'Social Media',
    tags: ['social', 'content', 'engagement']
  },
  {
    title: 'Product Description Writer',
    description: 'Write compelling product descriptions that convert',
    content: `Write a compelling product description for {{product_name}}.

Product details:
{{product_details}}

Requirements:
- Highlight key features and benefits
- Address customer pain points
- Include emotional triggers
- Add a strong call-to-action
- Length: {{length}}
- Tone: {{tone}}`,
    category: 'Marketing',
    tags: ['ecommerce', 'copywriting', 'conversion']
  },
  {
    title: 'Email Marketing Campaign',
    description: 'Create effective email marketing campaigns',
    content: `Create an email marketing campaign for {{campaign_goal}}.

Target audience: {{audience}}
Product/Service: {{product}}
Key message: {{message}}

Include:
- Compelling subject line (5 variations)
- Email body with clear structure
- Strong call-to-action
- P.S. line for additional engagement
- Tone: {{tone}}`,
    category: 'Marketing',
    tags: ['email', 'campaign', 'conversion']
  },
  {
    title: 'SEO Keyword Research',
    description: 'Generate keyword ideas and search intent analysis',
    content: `Perform keyword research for {{topic}}.

Requirements:
- Generate primary keywords (high volume)
- Generate long-tail keywords (low competition)
- Analyze search intent for each keyword
- Suggest content structure for top keywords
- Include related questions people ask`,
    category: 'Marketing',
    tags: ['seo', 'research', 'strategy']
  },
  {
    title: 'Twitter Thread Creator',
    description: 'Create engaging Twitter threads that go viral',
    content: `Create a Twitter thread about {{topic}}.

Requirements:
- Start with a hook that grabs attention
- {{tweet_count}} tweets total
- Include relevant data or insights
- Use clear, concise language
- End with a call-to-action
- Add strategic line breaks for readability
- Tone: {{tone}}`,
    category: 'Social Media',
    tags: ['twitter', 'thread', 'viral']
  },
  {
    title: 'LinkedIn Post Writer',
    description: 'Create professional LinkedIn posts that drive engagement',
    content: `Write a LinkedIn post about {{topic}}.

Context: {{context}}
Goal: {{goal}}

Requirements:
- Professional yet conversational tone
- Start with a compelling hook
- Include personal insights or story
- Add relevant hashtags (3-5)
- End with engagement question
- Length: {{length}}`,
    category: 'Social Media',
    tags: ['linkedin', 'professional', 'engagement']
  },
  {
    title: 'Instagram Caption & Hashtags',
    description: 'Generate Instagram captions with optimal hashtags',
    content: `Create an Instagram caption for {{post_description}}.

Brand voice: {{brand_voice}}
Target audience: {{audience}}

Include:
- Engaging caption ({{length}})
- Relevant emojis
- 20-30 mix of popular and niche hashtags
- Call-to-action
- Optional: Question to boost engagement`,
    category: 'Social Media',
    tags: ['instagram', 'hashtags', 'engagement']
  },
  {
    title: 'Code Review Assistant',
    description: 'Comprehensive code review with best practices',
    content: `Review the following code and provide detailed feedback:

\`\`\`{{language}}
{{code}}
\`\`\`

Analyze:
- Code quality and readability
- Performance optimizations
- Security vulnerabilities
- Best practices adherence
- Potential bugs or edge cases
- Suggested improvements with examples`,
    category: 'Developer',
    tags: ['code-review', 'quality', 'best-practices']
  },
  {
    title: 'Bug Fixer',
    description: 'Debug and fix code issues with explanations',
    content: `I'm encountering the following error:

Error: {{error_message}}

Code:
\`\`\`{{language}}
{{code}}
\`\`\`

Context: {{context}}

Please:
1. Identify the root cause
2. Provide the fixed code
3. Explain why the error occurred
4. Suggest how to prevent similar issues`,
    category: 'Developer',
    tags: ['debugging', 'fix', 'troubleshooting']
  },
  {
    title: 'Function Generator',
    description: 'Generate clean, well-documented functions',
    content: `Create a {{language}} function that {{description}}.

Requirements:
- Input: {{inputs}}
- Output: {{outputs}}
- Include error handling
- Add comprehensive comments
- Follow {{language}} best practices
- Include usage example`,
    category: 'Developer',
    tags: ['code-generation', 'function', 'documentation']
  },
  {
    title: 'API Documentation Writer',
    description: 'Generate clear API documentation',
    content: `Create API documentation for the following endpoint:

Endpoint: {{method}} {{endpoint}}
Purpose: {{purpose}}

Include:
- Description
- Authentication requirements
- Request parameters (with types and descriptions)
- Request body example
- Response format and status codes
- Error handling
- Code examples in {{languages}}`,
    category: 'Developer',
    tags: ['documentation', 'api', 'technical-writing']
  },
  {
    title: 'Test Case Generator',
    description: 'Generate comprehensive test cases',
    content: `Generate test cases for {{feature}}.

Code/Feature:
{{code_or_description}}

Include:
- Unit tests
- Integration tests
- Edge cases
- Error scenarios
- Test framework: {{framework}}
- Include setup and teardown if needed`,
    category: 'QA',
    tags: ['testing', 'quality-assurance', 'automation']
  },
  {
    title: 'SQL Query Optimizer',
    description: 'Optimize SQL queries for better performance',
    content: `Optimize the following SQL query:

\`\`\`sql
{{query}}
\`\`\`

Database: {{database_type}}
Table context: {{table_info}}

Provide:
- Optimized query
- Explanation of improvements
- Index recommendations
- Estimated performance gain`,
    category: 'Developer',
    tags: ['sql', 'optimization', 'database']
  },
  {
    title: 'System Architecture Design',
    description: 'Design scalable system architectures',
    content: `Design a system architecture for {{system_description}}.

Requirements:
- Expected scale: {{scale}}
- Key features: {{features}}
- Performance requirements: {{performance}}
- Budget constraints: {{budget}}

Provide:
- High-level architecture diagram description
- Technology stack recommendations
- Data flow and storage strategy
- Scalability considerations
- Security measures
- Deployment strategy`,
    category: 'Software Architect',
    tags: ['architecture', 'design', 'scalability']
  },
  {
    title: 'Database Schema Designer',
    description: 'Design normalized database schemas',
    content: `Design a database schema for {{application}}.

Entities needed: {{entities}}
Relationships: {{relationships}}
Expected data volume: {{volume}}

Provide:
- Table structures with fields and types
- Primary and foreign keys
- Indexes for optimization
- Normalization applied
- Migration considerations`,
    category: 'Software Architect',
    tags: ['database', 'schema', 'design']
  },
  {
    title: 'Microservices Architecture',
    description: 'Design microservices architecture patterns',
    content: `Design a microservices architecture for {{application}}.

Current challenges: {{challenges}}
Scale requirements: {{scale}}
Team structure: {{team_info}}

Include:
- Service boundaries and responsibilities
- Communication patterns
- Data management strategy
- Service discovery approach
- Monitoring and logging
- Deployment pipeline`,
    category: 'Software Architect',
    tags: ['microservices', 'architecture', 'distributed-systems']
  },
  {
    title: 'Test Strategy Document',
    description: 'Create comprehensive test strategies',
    content: `Create a test strategy document for {{project}}.

Project details:
{{project_details}}

Include:
- Test objectives and scope
- Types of testing (unit, integration, E2E, performance)
- Test environment requirements
- Tools and frameworks
- Test data management
- Defect management process
- Metrics and reporting
- Timeline and resources`,
    category: 'QA',
    tags: ['strategy', 'planning', 'documentation']
  },
  {
    title: 'Automated Test Script',
    description: 'Generate automated test scripts',
    content: `Create an automated test script for {{feature}}.

Testing framework: {{framework}}
Application type: {{app_type}}
Test scenario: {{scenario}}

Include:
- Setup and configuration
- Test data preparation
- Test steps with assertions
- Error handling
- Cleanup
- Comments explaining each step`,
    category: 'QA',
    tags: ['automation', 'testing', 'scripting']
  },
  {
    title: 'Business Requirements Document',
    description: 'Create detailed business requirements documents',
    content: `Create a Business Requirements Document for {{project}}.

Background: {{background}}
Stakeholders: {{stakeholders}}
Goals: {{goals}}

Include:
- Executive summary
- Business objectives
- Scope and deliverables
- User stories/use cases
- Functional requirements
- Non-functional requirements
- Success metrics
- Timeline and milestones
- Risks and dependencies`,
    category: 'Business Strategy',
    tags: ['requirements', 'documentation', 'planning']
  },
  {
    title: 'Competitive Analysis',
    description: 'Analyze competitors and market positioning',
    content: `Conduct a competitive analysis for {{company_or_product}}.

Industry: {{industry}}
Competitors to analyze: {{competitors}}
Focus areas: {{focus_areas}}

Provide:
- Competitor overview (strengths/weaknesses)
- Feature comparison matrix
- Pricing analysis
- Market positioning
- Differentiation opportunities
- Threats and opportunities
- Strategic recommendations`,
    category: 'Business Strategy',
    tags: ['analysis', 'competition', 'market-research']
  },
  {
    title: 'SWOT Analysis',
    description: 'Generate comprehensive SWOT analysis',
    content: `Perform a SWOT analysis for {{company_or_product}}.

Context: {{context}}
Industry: {{industry}}
Current position: {{position}}

Analyze:
- Strengths (internal positive factors)
- Weaknesses (internal negative factors)
- Opportunities (external positive factors)
- Threats (external negative factors)
- Strategic implications
- Action items for each quadrant`,
    category: 'Business Strategy',
    tags: ['analysis', 'strategy', 'planning']
  },
  {
    title: 'Landing Page Copy',
    description: 'Write high-converting landing page copy',
    content: `Write landing page copy for {{product_or_service}}.

Target audience: {{audience}}
Value proposition: {{value_prop}}
Key benefits: {{benefits}}

Include:
- Attention-grabbing headline
- Subheadline explaining the value
- Hero section copy
- Feature/benefit sections (3-5)
- Social proof section
- FAQ section (5 questions)
- Strong CTA (multiple variations)
- Tone: {{tone}}`,
    category: 'Marketing',
    tags: ['copywriting', 'conversion', 'landing-page']
  },
  {
    title: 'A/B Test Hypothesis',
    description: 'Generate data-driven A/B test hypotheses',
    content: `Create A/B test hypotheses for {{element}}.

Current performance: {{current_metrics}}
Goal: {{goal}}
Page/Feature: {{context}}

For each hypothesis provide:
- Hypothesis statement
- Rationale based on user behavior/data
- Proposed change (variant)
- Expected impact
- Success metrics
- Estimated test duration`,
    category: 'Acquisition',
    tags: ['testing', 'optimization', 'data-driven']
  },
  {
    title: 'User Acquisition Strategy',
    description: 'Develop comprehensive user acquisition strategies',
    content: `Create a user acquisition strategy for {{product}}.

Target audience: {{audience}}
Budget: {{budget}}
Timeline: {{timeline}}
Current channels: {{current_channels}}

Include:
- Channel selection and prioritization
- Tactics for each channel
- Budget allocation
- Expected CAC and ROI
- Key metrics to track
- Timeline and milestones
- Risk mitigation`,
    category: 'Acquisition',
    tags: ['growth', 'strategy', 'marketing']
  },
  {
    title: 'Growth Experiment Design',
    description: 'Design growth experiments with clear hypotheses',
    content: `Design a growth experiment for {{objective}}.

Current situation: {{current_state}}
Target metric: {{metric}}
Constraint: {{constraint}}

Provide:
- Hypothesis statement
- Experiment design
- Success metrics and targets
- Implementation steps
- Data collection method
- Analysis approach
- Timeline
- Resources needed`,
    category: 'Acquisition',
    tags: ['growth', 'experimentation', 'metrics']
  },
  {
    title: 'Funnel Optimization Analysis',
    description: 'Analyze and optimize conversion funnels',
    content: `Analyze the conversion funnel for {{product_or_feature}}.

Funnel stages: {{stages}}
Current conversion rates: {{rates}}
User feedback: {{feedback}}

Provide:
- Bottleneck identification
- Drop-off analysis for each stage
- Optimization recommendations
- Quick wins vs. long-term improvements
- Expected impact of each recommendation
- Implementation priority`,
    category: 'Acquisition',
    tags: ['conversion', 'optimization', 'analytics']
  },
  {
    title: 'Content Calendar Planner',
    description: 'Plan comprehensive content calendars',
    content: `Create a content calendar for {{duration}}.

Platform(s): {{platforms}}
Content pillars: {{pillars}}
Target audience: {{audience}}
Posting frequency: {{frequency}}

Include:
- Content themes for each week
- Specific post ideas with formats
- Important dates and events to leverage
- Content mix (promotional vs. educational vs. entertaining)
- Hashtag strategies
- Engagement tactics`,
    category: 'Social Media',
    tags: ['planning', 'content', 'strategy']
  },
  {
    title: 'Video Script Writer',
    description: 'Write engaging video scripts',
    content: `Write a video script for {{video_topic}}.

Video length: {{duration}}
Platform: {{platform}}
Target audience: {{audience}}
Goal: {{goal}}

Include:
- Hook (first 3-5 seconds)
- Introduction
- Main content sections with key points
- Visual/B-roll suggestions
- Call-to-action
- Outro
- Estimated timestamps`,
    category: 'Social Media',
    tags: ['video', 'scripting', 'content']
  },
  {
    title: 'Press Release Writer',
    description: 'Write professional press releases',
    content: `Write a press release for {{announcement}}.

Company: {{company}}
Key details: {{details}}
Quote source: {{quote_source}}

Structure:
- Headline (attention-grabbing)
- Subheadline
- Dateline and lead paragraph (5 Ws)
- Body paragraphs with details
- Company quote
- Boilerplate about company
- Contact information template`,
    category: 'Marketing',
    tags: ['pr', 'writing', 'corporate']
  },
  {
    title: 'Customer Journey Map',
    description: 'Map detailed customer journeys',
    content: `Create a customer journey map for {{customer_persona}}.

Product/Service: {{product}}
Journey stages: {{stages}}

For each stage include:
- Customer actions
- Touchpoints
- Emotions and pain points
- Opportunities for improvement
- Metrics to measure
- Team responsible
- Current vs. desired state`,
    category: 'Business Strategy',
    tags: ['customer-experience', 'mapping', 'strategy']
  },
  {
    title: 'Value Proposition Canvas',
    description: 'Create compelling value propositions',
    content: `Create a value proposition for {{product_or_service}}.

Target customer: {{customer_segment}}
Customer jobs: {{jobs_to_be_done}}
Current alternatives: {{alternatives}}

Define:
- Customer pains (ranked by severity)
- Customer gains (ranked by importance)
- Pain relievers (how you address pains)
- Gain creators (how you create gains)
- Products and services
- Value proposition statement
- Differentiation from alternatives`,
    category: 'Business Strategy',
    tags: ['product', 'strategy', 'positioning']
  },
  {
    title: 'Meeting Summary Generator',
    description: 'Generate clear meeting summaries',
    content: `Create a meeting summary from these notes:

{{meeting_notes}}

Include:
- Meeting details (date, attendees, purpose)
- Key discussion points
- Decisions made
- Action items (who, what, when)
- Open questions
- Next steps
- Follow-up required`,
    category: 'Business Strategy',
    tags: ['productivity', 'documentation', 'communication']
  },
  {
    title: 'OKR Framework Builder',
    description: 'Create Objectives and Key Results frameworks',
    content: `Create OKRs for {{team_or_company}}.

Time period: {{period}}
Strategic priorities: {{priorities}}
Current challenges: {{challenges}}

For each Objective provide:
- Inspiring objective statement
- 3-5 measurable Key Results
- Current baseline
- Target achievement
- Initiatives to reach targets
- Owner and team
- Dependencies`,
    category: 'Business Strategy',
    tags: ['goals', 'planning', 'strategy']
  },
  {
    title: 'User Story Writer',
    description: 'Write clear user stories with acceptance criteria',
    content: `Write user stories for {{feature}}.

User type: {{user_type}}
Goal: {{goal}}
Context: {{context}}

For each story include:
- User story (As a [user], I want [goal], so that [benefit])
- Acceptance criteria (Given/When/Then format)
- Edge cases
- Dependencies
- Story points estimate
- Priority (MoSCoW)`,
    category: 'Developer',
    tags: ['agile', 'requirements', 'documentation']
  },
  {
    title: 'Code Documentation Generator',
    description: 'Generate comprehensive code documentation',
    content: `Generate documentation for the following code:

\`\`\`{{language}}
{{code}}
\`\`\`

Include:
- Overview and purpose
- Parameters and return values
- Usage examples
- Edge cases and limitations
- Related functions/classes
- Code complexity notes
- Follow {{documentation_style}} style`,
    category: 'Developer',
    tags: ['documentation', 'code', 'technical-writing']
  },
  {
    title: 'Regex Pattern Generator',
    description: 'Generate and explain regex patterns',
    content: `Create a regex pattern for {{requirement}}.

Examples of valid inputs:
{{valid_examples}}

Examples of invalid inputs:
{{invalid_examples}}

Provide:
- Regex pattern
- Explanation of each part
- Test cases
- Language-specific implementation ({{language}})
- Common edge cases to handle`,
    category: 'Developer',
    tags: ['regex', 'patterns', 'validation']
  },
  {
    title: 'Performance Optimization Guide',
    description: 'Analyze and optimize code performance',
    content: `Analyze performance issues in:

\`\`\`{{language}}
{{code}}
\`\`\`

Context: {{performance_context}}
Current metrics: {{metrics}}

Provide:
- Performance bottleneck analysis
- Optimization recommendations (prioritized)
- Refactored code examples
- Expected improvements
- Trade-offs to consider
- Monitoring suggestions`,
    category: 'Developer',
    tags: ['performance', 'optimization', 'profiling']
  },
  {
    title: 'Git Commit Message Generator',
    description: 'Generate conventional commit messages',
    content: `Generate a commit message for these changes:

Changes made:
{{changes}}

Follow conventional commits format:
- type(scope): subject
- body explaining what and why
- breaking changes if any
- references to issues

Types: feat, fix, docs, style, refactor, test, chore`,
    category: 'Developer',
    tags: ['git', 'version-control', 'best-practices']
  },
  {
    title: 'Security Vulnerability Scanner',
    description: 'Identify security vulnerabilities in code',
    content: `Scan the following code for security vulnerabilities:

\`\`\`{{language}}
{{code}}
\`\`\`

Application context: {{context}}

Analyze for:
- SQL injection
- XSS vulnerabilities
- CSRF issues
- Authentication/authorization flaws
- Data exposure risks
- Input validation issues
- Provide secure code alternatives`,
    category: 'Developer',
    tags: ['security', 'vulnerability', 'code-review']
  },
  {
    title: 'Refactoring Guide',
    description: 'Refactor code for better maintainability',
    content: `Refactor the following code:

\`\`\`{{language}}
{{code}}
\`\`\`

Goals: {{refactoring_goals}}

Provide:
- Code smells identified
- Refactoring strategy
- Step-by-step transformation
- Final refactored code
- Benefits of changes
- Testing considerations`,
    category: 'Developer',
    tags: ['refactoring', 'clean-code', 'maintainability']
  },
  {
    title: 'Chrome Extension Idea Generator',
    description: 'Generate creative Chrome extension ideas',
    content: `Generate Chrome extension ideas for {{use_case}}.

Target users: {{users}}
Problem to solve: {{problem}}

For each idea provide:
- Extension name
- Core functionality
- Key features (3-5)
- Target audience
- Monetization potential
- Technical complexity
- Similar existing extensions`,
    category: 'Business Strategy',
    tags: ['product-ideas', 'innovation', 'chrome']
  },
  {
    title: 'API Design Guidelines',
    description: 'Design RESTful APIs following best practices',
    content: `Design a RESTful API for {{application}}.

Resources: {{resources}}
Operations needed: {{operations}}
Authentication: {{auth_type}}

Provide:
- Endpoint structure
- HTTP methods for each endpoint
- Request/response formats
- Status codes
- Error handling
- Versioning strategy
- Rate limiting approach
- Documentation structure`,
    category: 'Software Architect',
    tags: ['api', 'design', 'rest']
  },
  {
    title: 'Tech Stack Recommendation',
    description: 'Recommend optimal tech stacks',
    content: `Recommend a tech stack for {{project_type}}.

Requirements:
- Project description: {{description}}
- Team expertise: {{team_skills}}
- Scale expectations: {{scale}}
- Budget: {{budget}}
- Timeline: {{timeline}}

Provide recommendations for:
- Frontend framework
- Backend framework
- Database
- Infrastructure/hosting
- DevOps tools
- Third-party services
- Justification for each choice
- Potential alternatives`,
    category: 'Software Architect',
    tags: ['technology', 'stack', 'architecture']
  },
  {
    title: 'Data Migration Plan',
    description: 'Plan safe data migrations',
    content: `Create a data migration plan from {{source}} to {{destination}}.

Data volume: {{volume}}
Downtime tolerance: {{downtime}}
Data types: {{data_types}}

Include:
- Migration strategy (big bang vs. incremental)
- Data mapping and transformation rules
- Validation approach
- Rollback plan
- Testing strategy
- Timeline and phases
- Risk mitigation
- Success criteria`,
    category: 'Software Architect',
    tags: ['migration', 'data', 'planning']
  },
  {
    title: 'Load Testing Strategy',
    description: 'Design comprehensive load testing strategies',
    content: `Design a load testing strategy for {{application}}.

Expected load: {{load}}
Critical user flows: {{flows}}
Performance requirements: {{requirements}}

Include:
- Test scenarios
- Load patterns (ramp-up, sustained, spike)
- Tools and frameworks
- Metrics to monitor
- Success criteria
- Bottleneck identification approach
- Reporting format`,
    category: 'QA',
    tags: ['performance', 'load-testing', 'strategy']
  },
  {
    title: 'Accessibility Audit Checklist',
    description: 'Create WCAG accessibility audit checklists',
    content: `Create an accessibility audit checklist for {{application_type}}.

WCAG level: {{wcag_level}}
Priority areas: {{priorities}}

Include checks for:
- Keyboard navigation
- Screen reader compatibility
- Color contrast
- Alternative text
- Form labels
- Focus indicators
- Semantic HTML
- ARIA attributes
- Testing tools to use
- Remediation priorities`,
    category: 'QA',
    tags: ['accessibility', 'audit', 'wcag']
  },
  {
    title: 'Pricing Strategy Generator',
    description: 'Develop product pricing strategies',
    content: `Create a pricing strategy for {{product}}.

Cost structure: {{costs}}
Target market: {{market}}
Competitors: {{competitors}}
Value proposition: {{value}}

Analyze and recommend:
- Pricing model (subscription, one-time, freemium, etc.)
- Price points and tiers
- Psychological pricing tactics
- Competitor comparison
- Discount strategy
- Price elasticity considerations
- Revenue projections
- A/B testing approach`,
    category: 'Business Strategy',
    tags: ['pricing', 'monetization', 'strategy']
  },
  {
    title: 'Customer Retention Strategy',
    description: 'Build strategies to improve customer retention',
    content: `Create a customer retention strategy for {{product}}.

Current churn rate: {{churn}}
Customer segments: {{segments}}
Common churn reasons: {{reasons}}

Include:
- Retention metrics to track
- Engagement strategies
- Win-back campaigns
- Customer success initiatives
- Product improvements
- Communication cadence
- Loyalty/rewards programs
- Expected impact on retention`,
    category: 'Acquisition',
    tags: ['retention', 'churn', 'customer-success']
  },
  {
    title: 'Influencer Outreach Template',
    description: 'Craft personalized influencer outreach messages',
    content: `Create an influencer outreach message for {{campaign}}.

Influencer details: {{influencer_info}}
Brand: {{brand}}
Campaign goal: {{goal}}
Compensation: {{compensation}}

Include:
- Personalized opening
- Brand introduction
- Campaign details and expectations
- Why they're a good fit
- Deliverables
- Compensation/partnership terms
- Next steps
- Professional closing`,
    category: 'Social Media',
    tags: ['influencer', 'outreach', 'partnership']
  },
  {
    title: 'Chatbot Conversation Flow',
    description: 'Design chatbot conversation flows',
    content: `Design a chatbot conversation flow for {{use_case}}.

Bot personality: {{personality}}
Primary goals: {{goals}}
User types: {{user_types}}

Include:
- Welcome message
- Intent detection examples
- Conversation paths (happy path and alternatives)
- Fallback responses
- Error handling
- Escalation to human
- Closing/farewell messages
- Context management`,
    category: 'Developer',
    tags: ['chatbot', 'conversation', 'ux']
  },
  {
    title: 'Onboarding Flow Designer',
    description: 'Design effective user onboarding flows',
    content: `Design an onboarding flow for {{product}}.

User type: {{user_type}}
Key features to highlight: {{features}}
Time constraint: {{time_limit}}
Activation goal: {{activation}}

Include:
- Onboarding steps (numbered)
- Value demonstration at each step
- Interactive elements
- Progress indicators
- Skip options
- Success metrics
- Drop-off prevention tactics
- Completion reward/next steps`,
    category: 'Business Strategy',
    tags: ['onboarding', 'ux', 'activation']
  },
  {
    title: 'Brand Voice Guidelines',
    description: 'Define consistent brand voice and tone',
    content: `Create brand voice guidelines for {{brand}}.

Brand values: {{values}}
Target audience: {{audience}}
Industry: {{industry}}
Differentiation: {{differentiation}}

Define:
- Core voice characteristics (3-5)
- Tone variations for different contexts
- Do's and Don'ts with examples
- Vocabulary to use/avoid
- Grammar and style preferences
- Example sentences in brand voice
- Voice across different channels`,
    category: 'Marketing',
    tags: ['brand', 'voice', 'guidelines']
  },
  {
    title: 'Crisis Communication Plan',
    description: 'Prepare crisis communication strategies',
    content: `Create a crisis communication plan for {{scenario}}.

Organization: {{organization}}
Stakeholders: {{stakeholders}}
Communication channels: {{channels}}

Include:
- Initial response template
- Holding statement
- FAQ preparation
- Stakeholder-specific messages
- Social media response strategy
- Media contact approach
- Timeline for updates
- Recovery messaging`,
    category: 'Marketing',
    tags: ['crisis', 'pr', 'communication']
  },
  {
    title: 'UX Research Plan',
    description: 'Design user experience research plans',
    content: `Create a UX research plan for {{research_goal}}.

Product/Feature: {{product}}
Users to research: {{users}}
Timeline: {{timeline}}
Budget: {{budget}}

Include:
- Research questions
- Methodology (interviews, surveys, usability testing, etc.)
- Participant recruitment criteria
- Discussion guide/script
- Data collection approach
- Analysis framework
- Deliverables
- Success metrics`,
    category: 'Business Strategy',
    tags: ['ux', 'research', 'user-testing']
  }
]

export function getTemplatesByCategory(category: string): PromptTemplate[] {
  return PROMPT_TEMPLATES.filter(t => t.category === category)
}

export function getTemplatesByTag(tag: string): PromptTemplate[] {
  return PROMPT_TEMPLATES.filter(t => t.tags.includes(tag))
}

export function searchTemplates(query: string): PromptTemplate[] {
  const lowercaseQuery = query.toLowerCase()
  return PROMPT_TEMPLATES.filter(t => 
    t.title.toLowerCase().includes(lowercaseQuery) ||
    t.description.toLowerCase().includes(lowercaseQuery) ||
    t.tags.some(tag => tag.toLowerCase().includes(lowercaseQuery))
  )
}
