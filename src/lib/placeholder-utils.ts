export type Placeholder = {
  name: string
  value: string
}

export function extractPlaceholders(content: string): string[] {
  const regex = /\{\{([^}]+)\}\}/g
  const matches = new Set<string>()
  let match
  
  while ((match = regex.exec(content)) !== null) {
    matches.add(match[1].trim())
  }
  
  return Array.from(matches)
}

export function extractProjectVariables(content: string): string[] {
  const regex = /\{\{\{([^}]+)\}\}\}/g
  const matches = new Set<string>()
  let match
  
  while ((match = regex.exec(content)) !== null) {
    matches.add(match[1].trim())
  }
  
  return Array.from(matches)
}

export function replacePlaceholders(content: string, placeholders: Placeholder[]): string {
  let result = content
  
  placeholders.forEach(({ name, value }) => {
    const regex = new RegExp(`\\{\\{\\s*${name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\s*\\}\\}`, 'g')
    result = result.replace(regex, value)
  })
  
  return result
}

export function replaceProjectVariables(content: string, variables: Record<string, string>): string {
  let result = content
  
  Object.entries(variables || {}).forEach(([name, value]) => {
    const regex = new RegExp(`\\{\\{\\{\\s*${name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\s*\\}\\}\\}`, 'g')
    result = result.replace(regex, value)
  })
  
  return result
}
