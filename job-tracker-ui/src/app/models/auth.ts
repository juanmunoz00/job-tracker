export type UserRole = 'Admin' | 'ReadOnly';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;   // ISO date in UTC, e.g. "2026-11-05T03:10:00Z"
  username: string;
  role: UserRole;
}