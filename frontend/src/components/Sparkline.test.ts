import { describe, expect, it } from 'vitest';
import { buildPath } from './Sparkline';

describe('buildPath', () => {
  it('draws a flat line when there is no history', () => {
    expect(buildPath([])).toBe('M 4 18 L 156 18');
  });

  it('draws a single point at the top for one pass', () => {
    expect(buildPath(['Passed'])).toBe('M 4.0 4');
  });

  it('alternates between top and bottom for alternating outcomes', () => {
    const path = buildPath(['Passed', 'Failed', 'Passed']);
    expect(path).toBe('M 4.0 4 L 80.0 32 L 156.0 4');
  });
});
