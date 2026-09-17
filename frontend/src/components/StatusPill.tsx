import Box from '@mui/material/Box';
import type { TestCaseStatus } from '../api/types';
import { colors } from '../theme';

const styles: Record<TestCaseStatus, { bg: string; fg: string }> = {
  Stable: { bg: '#E3F1EE', fg: colors.stable },
  Suspicious: { bg: '#FBEDDA', fg: colors.suspicious },
  Quarantined: { bg: '#F9E1DC', fg: colors.quarantined },
};

export function StatusPill({ status }: { status: TestCaseStatus }) {
  const style = styles[status];
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-block',
        px: 1.1,
        py: 0.3,
        borderRadius: 3,
        fontSize: '0.75rem',
        fontWeight: 500,
        bgcolor: style.bg,
        color: style.fg,
      }}
    >
      {status}
    </Box>
  );
}
