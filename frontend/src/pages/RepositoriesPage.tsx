import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import TextField from '@mui/material/TextField';
import Alert from '@mui/material/Alert';
import { fetchRepositories, registerRepository } from '../api/client';
import type { RegisteredRepository, RepositorySummary } from '../api/types';
import { colors } from '../theme';

export function RepositoriesPage() {
  const navigate = useNavigate();
  const [repositories, setRepositories] = useState<RepositorySummary[] | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [owner, setOwner] = useState('');
  const [name, setName] = useState('');
  const [registered, setRegistered] = useState<RegisteredRepository | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = () => {
    fetchRepositories()
      .then(setRepositories)
      .catch(() => setError('Could not reach the FlakeGuard API.'));
  };

  useEffect(load, []);

  const handleRegister = async () => {
    setError(null);
    try {
      const result = await registerRepository(owner.trim(), name.trim());
      setRegistered(result);
      load();
    } catch {
      setError('Registration failed. Check the owner/name are not already registered.');
    }
  };

  const closeDialog = () => {
    setDialogOpen(false);
    setOwner('');
    setName('');
    setRegistered(null);
  };

  return (
    <Container maxWidth="md" sx={{ py: 6 }}>
      <Stack direction="row" sx={{ mb: 4, justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <Box>
          <Typography variant="h4" component="h1" sx={{ mb: 0.5 }}>
            FlakeGuard
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Repositories reporting test results
          </Typography>
        </Box>
        <Button variant="contained" onClick={() => setDialogOpen(true)}>
          Register repository
        </Button>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {repositories?.length === 0 && (
        <Typography color="text.secondary">
          No repositories yet. Register one to get a webhook secret and start sending test results.
        </Typography>
      )}

      <Stack spacing={1.5}>
        {repositories?.map((repo) => (
          <Box
            key={repo.id}
            onClick={() => navigate(`/repositories/${repo.id}`)}
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              px: 2.5,
              py: 2,
              bgcolor: 'background.paper',
              border: '1px solid',
              borderColor: 'divider',
              borderRadius: 1,
              cursor: 'pointer',
              '&:hover': { borderColor: colors.accent },
            }}
          >
            <Box>
              <Typography sx={{ fontFamily: '"JetBrains Mono", monospace', fontSize: '0.95rem' }}>
                {repo.owner}/{repo.name}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {repo.totalTests} test{repo.totalTests === 1 ? '' : 's'} tracked
              </Typography>
            </Box>
            {repo.quarantinedCount > 0 ? (
              <Box
                sx={{
                  px: 1.4,
                  py: 0.5,
                  borderRadius: 3,
                  bgcolor: '#F9E1DC',
                  color: colors.quarantined,
                  fontSize: '0.8rem',
                  fontWeight: 600,
                }}
              >
                {repo.quarantinedCount} quarantined
              </Box>
            ) : (
              <Typography variant="body2" color="text.secondary">
                clean
              </Typography>
            )}
          </Box>
        ))}
      </Stack>

      <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="xs">
        <DialogTitle>Register a repository</DialogTitle>
        <DialogContent>
          {registered ? (
            <Stack spacing={1.5} sx={{ pt: 1 }}>
              <Alert severity="success">Registered. Save this webhook secret, it is shown only once.</Alert>
              <Box
                sx={{
                  p: 1.5,
                  bgcolor: colors.panel,
                  color: '#DDE7E4',
                  borderRadius: 1,
                  fontFamily: '"JetBrains Mono", monospace',
                  fontSize: '0.8rem',
                  wordBreak: 'break-all',
                }}
              >
                {registered.webhookSecret}
              </Box>
              <Typography variant="body2" color="text.secondary">
                Sign ingestion requests to <code>/api/ingest/{registered.owner}/{registered.name}</code> with an{' '}
                <code>X-FlakeGuard-Signature</code> header: <code>sha256=&lt;HMAC-SHA256 of the raw body&gt;</code>.
              </Typography>
            </Stack>
          ) : (
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField label="Owner" value={owner} onChange={(e) => setOwner(e.target.value)} autoFocus />
              <TextField label="Repository name" value={name} onChange={(e) => setName(e.target.value)} />
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDialog}>{registered ? 'Done' : 'Cancel'}</Button>
          {!registered && (
            <Button variant="contained" onClick={handleRegister} disabled={!owner.trim() || !name.trim()}>
              Register
            </Button>
          )}
        </DialogActions>
      </Dialog>
    </Container>
  );
}
