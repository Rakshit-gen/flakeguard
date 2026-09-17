export type TestCaseStatus = 'Stable' | 'Suspicious' | 'Quarantined';
export type TestOutcome = 'Passed' | 'Failed';

export interface RepositorySummary {
  id: string;
  owner: string;
  name: string;
  totalTests: number;
  quarantinedCount: number;
}

export interface RegisteredRepository {
  id: string;
  owner: string;
  name: string;
  webhookSecret: string;
}

export interface TestCaseSummary {
  id: string;
  suiteName: string;
  testName: string;
  status: TestCaseStatus;
  flipRate: number;
  failureRate: number;
  lastUpdatedAt: string;
  quarantinedAt: string | null;
  recentOutcomes: TestOutcome[];
}
