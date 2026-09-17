import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Stack from '@mui/material/Stack';
import Alert from '@mui/material/Alert';
import { fetchTests } from '../api/client';
import type { TestCaseSummary } from '../api/types';
import { Sparkline } from '../components/Sparkline';
import { StatusPill } from '../components/StatusPill';

export function RepositoryDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [tests, setTests] = useState<TestCaseSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) {
      return;
    }
    fetchTests(id)
      .then(setTests)
      .catch(() => setError('Could not load tests for this repository.'));
  }, [id]);

  return (
    <Container maxWidth="md" sx={{ py: 6 }}>
      <Typography component={Link} to="/" variant="body2" sx={{ color: 'text.secondary', textDecoration: 'none' }}>
        &larr; Repositories
      </Typography>
      <Typography variant="h4" component="h1" sx={{ mt: 1, mb: 4 }}>
        Tests
      </Typography>

      {error && <Alert severity="error">{error}</Alert>}

      {tests?.length === 0 && (
        <Typography color="text.secondary">
          No test results yet. Once CI starts posting to the ingestion endpoint, tests will appear here.
        </Typography>
      )}

      <Stack spacing={1.25}>
        {tests?.map((test) => (
          <Box
            key={test.id}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 2,
              px: 2.5,
              py: 1.75,
              bgcolor: 'background.paper',
              border: '1px solid',
              borderColor: 'divider',
              borderRadius: 1,
            }}
          >
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography
                sx={{
                  fontFamily: '"JetBrains Mono", monospace',
                  fontSize: '0.88rem',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {test.suiteName}::{test.testName}
              </Typography>
              <Stack direction="row" spacing={2} sx={{ mt: 0.5 }}>
                <Typography variant="caption" color="text.secondary">
                  flip rate {(test.flipRate * 100).toFixed(0)}%
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  failure rate {(test.failureRate * 100).toFixed(0)}%
                </Typography>
              </Stack>
            </Box>
            <Sparkline outcomes={test.recentOutcomes} status={test.status} />
            <StatusPill status={test.status} />
          </Box>
        ))}
      </Stack>
    </Container>
  );
}
