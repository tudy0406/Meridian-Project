/** Central runtime configuration derived from Vite env variables. */
export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5286';
export const API_BASE = `${API_URL}/api`;
export const HUB_URL = `${API_URL}/hubs/notifications`;
export const TOKEN_STORAGE_KEY = 'meridian.token';
