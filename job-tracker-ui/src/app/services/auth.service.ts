import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth';

const STORAGE_KEY = 'jobtracker.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<LoginResponse | null>(null);
  private logoutTimer: ReturnType<typeof setTimeout> | null = null;

  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.role === 'Admin');
  readonly username = computed(() => this.session()?.username ?? '');

  constructor() {
    this.restore();
  }

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(tap(response => this.start(response)));
  }

  logout(reason?: 'expired'): void {
    this.clearTimer();
    this.session.set(null);
    try { localStorage.removeItem(STORAGE_KEY); } catch { /* storage unavailable */ }
    this.router.navigate(['/login'], reason ? { queryParams: { reason } } : {});
  }

  private start(session: LoginResponse): void {
    const msLeft = new Date(session.expiresAt).getTime() - Date.now();
    if (msLeft <= 0) {
      this.logout('expired');
      return;
    }

    this.session.set(session);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(session)); } catch { /* storage unavailable */ }

    this.clearTimer();
    this.logoutTimer = setTimeout(() => this.logout('expired'), msLeft);
  }

  private restore(): void {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return;

      const saved = JSON.parse(raw) as LoginResponse;
      if (new Date(saved.expiresAt).getTime() > Date.now()) {
        this.start(saved);
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch { /* corrupt or unavailable storage: start logged out */ }
  }

  private clearTimer(): void {
    if (this.logoutTimer) {
      clearTimeout(this.logoutTimer);
      this.logoutTimer = null;
    }
  }
}