export type Prompt = {
  id: string
  title: string
  description: string
  content: string
  projectId: string
  categoryId: string
  tags: string[]
  createdBy: string
  createdAt: number
  updatedAt: number
  isArchived: boolean
}

export type PromptVersion = {
  id: string
  promptId: string
  versionNumber: number
  content: string
  changeNote: string
  createdBy: string
  createdAt: number
  improvedFrom?: string
}

export type Comment = {
  id: string
  promptId: string
  versionId?: string
  userId: string
  userName: string
  userAvatar: string
  content: string
  createdAt: number
  updatedAt: number
}

export type Project = {
  id: string
  name: string
  description: string
  color: string
}

export type Category = {
  id: string
  projectId: string
  name: string
  description: string
}

export type Tag = {
  id: string
  name: string
  color: string
}

export type Run = {
  id: string
  promptVersionId: string
  input: string
  output: string
  status: 'pending' | 'success' | 'failed'
  createdAt: number
}
