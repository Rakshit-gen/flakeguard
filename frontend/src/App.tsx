import { Routes, Route } from 'react-router-dom';
import { RepositoriesPage } from './pages/RepositoriesPage';
import { RepositoryDetailPage } from './pages/RepositoryDetailPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<RepositoriesPage />} />
      <Route path="/repositories/:id" element={<RepositoryDetailPage />} />
    </Routes>
  );
}
