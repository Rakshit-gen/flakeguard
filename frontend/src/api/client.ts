import type { RegisteredRepository, RepositorySummary, TestCaseSummary } from './types';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:4000';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });
  if (!response.ok) {
    throw new Error(`${init?.method ?? 'GET'} ${path} failed with ${response.status}`);
  }
  return response.json() as Promise<T>;
}

export function fetchRepositories(): Promise<RepositorySummary[]> {
  return request('/api/repositories');
}

export function registerRepository(owner: string, name: string): Promise<RegisteredRepository> {
  return request('/api/repositories', {
    method: 'POST',
    body: JSON.stringify({ owner, name, gitHubInstallationToken: null }),
  });
}

export function fetchTests(repositoryId: string): Promise<TestCaseSummary[]> {
  return request(`/api/repositories/${repositoryId}/tests`);
}
