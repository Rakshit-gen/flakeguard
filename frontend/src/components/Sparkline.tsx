import Box from '@mui/material/Box';
import type { TestOutcome } from '../api/types';
import { colors } from '../theme';

const WIDTH = 160;
const HEIGHT = 36;
const PAD = 4;

export function buildPath(outcomes: TestOutcome[]): string {
  if (outcomes.length === 0) {
    return `M ${PAD} ${HEIGHT / 2} L ${WIDTH - PAD} ${HEIGHT / 2}`;
  }
  const top = PAD;
  const bottom = HEIGHT - PAD;
  const step = outcomes.length > 1 ? (WIDTH - PAD * 2) / (outcomes.length - 1) : 0;

  return outcomes
    .map((outcome, index) => {
      const x = PAD + step * index;
      const y = outcome === 'Passed' ? top : bottom;
      return `${index === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y}`;
    })
    .join(' ');
}

const traceColor: Record<'Stable' | 'Suspicious' | 'Quarantined', string> = {
  Stable: colors.stable,
  Suspicious: colors.suspicious,
  Quarantined: colors.quarantined,
};

export function Sparkline({
  outcomes,
  status,
}: {
  outcomes: TestOutcome[];
  status: 'Stable' | 'Suspicious' | 'Quarantined';
}) {
  return (
    <Box
      component="svg"
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      sx={{ width: WIDTH, height: HEIGHT, display: 'block', bgcolor: colors.panel, borderRadius: 1 }}
    >
      <path
        d={buildPath(outcomes)}
        fill="none"
        stroke={traceColor[status]}
        strokeWidth={1.6}
        strokeLinecap="round"
        strokeLinejoin="round"
        opacity={0.95}
      />
    </Box>
  );
}
