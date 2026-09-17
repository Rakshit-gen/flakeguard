import { createTheme } from '@mui/material/styles';

export const colors = {
  paper: '#F1F4F3',
  panel: '#14202B',
  panelTrace: '#1C8C7D',
  ink: '#172026',
  inkMuted: '#5B6870',
  hairline: '#D7DDDA',
  stable: '#1C8C7D',
  suspicious: '#C97F1E',
  quarantined: '#C4432B',
  accent: '#33547A',
};

const theme = createTheme({
  palette: {
    mode: 'light',
    background: { default: colors.paper, paper: '#FFFFFF' },
    text: { primary: colors.ink, secondary: colors.inkMuted },
    primary: { main: colors.accent },
    success: { main: colors.stable },
    warning: { main: colors.suspicious },
    error: { main: colors.quarantined },
    divider: colors.hairline,
  },
  typography: {
    fontFamily: '"Space Grotesk", sans-serif',
    h1: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 600 },
    h2: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 600 },
    h3: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 600 },
    button: { textTransform: 'none', fontWeight: 500 },
  },
  shape: { borderRadius: 4 },
  components: {
    MuiButton: {
      styleOverrides: {
        root: { borderRadius: 4 },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' },
      },
    },
  },
});

export default theme;
