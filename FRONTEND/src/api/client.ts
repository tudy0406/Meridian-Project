import axios, { AxiosError } from 'axios';
import { API_BASE, TOKEN_STORAGE_KEY } from '../config';

/** Shared axios instance. Attaches the JWT and normalizes API errors. */
export const api = axios.create({ baseURL: API_BASE });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export interface ApiError {
  status: number;
  error: string;
  message: string;
  fieldErrors?: Record<string, string[]>;
}

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError<{ error?: string; message?: string; errors?: Record<string, string[]> }>) => {
    // On expired/invalid token, drop it so the app returns to the login screen.
    if (error.response?.status === 401) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }
    const apiError: ApiError = {
      status: error.response?.status ?? 0,
      error: error.response?.data?.error ?? 'Error',
      message: error.response?.data?.message ?? error.message ?? 'Unexpected error',
      fieldErrors: error.response?.data?.errors,
    };
    return Promise.reject(apiError);
  },
);
