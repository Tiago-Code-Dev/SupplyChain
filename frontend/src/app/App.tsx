import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '../features/auth/store/auth.store';
import { LoginPage } from '../pages/LoginPage';
import { EmployeesPage } from '../pages/EmployeesPage';
import { EmployeeFormPage } from '../pages/EmployeeFormPage';
import { EmployeeDetailPage } from '../pages/EmployeeDetailPage';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated, isLoading, checkAuth } = useAuthStore();

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  if (isLoading) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
};

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/employees"
          element={
            <ProtectedRoute>
              <EmployeesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/employees/new"
          element={
            <ProtectedRoute>
              <EmployeeFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/employees/:id"
          element={
            <ProtectedRoute>
              <EmployeeDetailPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/employees/:id/edit"
          element={
            <ProtectedRoute>
              <EmployeeFormPage />
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<Navigate to="/employees" replace />} />
        <Route path="*" element={<Navigate to="/employees" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;


