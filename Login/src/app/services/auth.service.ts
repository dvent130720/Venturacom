import { Injectable, inject, signal } from '@angular/core';
import { HttpClient }                 from '@angular/common/http';
import { Router }                     from '@angular/router';
import { Observable, tap }            from 'rxjs';
import { AuthResponse, User }         from '../models/auth.models';
import { environment }                from '../../environments/environment';

const ACCESS_KEY  = 'vc_access';
const REFRESH_KEY = 'vc_refresh';
const USER_KEY    = 'vc_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http   = inject(HttpClient);
  private router = inject(Router);
  private base   = `${environment.apiUrl}/api/auth`;

  // Estado reactivo del usuario actual
  currentUser = signal<User | null>(this.loadUser());

  // ── Email OTP ────────────────────────────────────────────────────────────

  sendOtp(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/send-otp`, { email });
  }

  verifyOtp(email: string, code: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/verify-otp`, { email, code })
      .pipe(tap(r => this.saveSession(r)));
  }

  // ── Google ───────────────────────────────────────────────────────────────

  /** Flujo con ID token (One Tap / renderButton) */
  loginWithGoogle(idToken: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/google`, { idToken })
      .pipe(tap(r => this.saveSession(r)));
  }

  /** Flujo con Access Token (OAuth2 initTokenClient) */
  loginWithGoogleAccessToken(accessToken: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/google/token`, { accessToken })
      .pipe(tap(r => this.saveSession(r)));
  }

  // ── Refresh / Logout ─────────────────────────────────────────────────────

  refresh(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem(REFRESH_KEY) ?? '';
    return this.http
      .post<AuthResponse>(`${this.base}/refresh`, { refreshToken })
      .pipe(tap(r => this.saveSession(r)));
  }

  logout(): void {
    const refreshToken = localStorage.getItem(REFRESH_KEY);
    if (refreshToken) {
      this.http.post(`${this.base}/logout`, { refreshToken }).subscribe();
    }
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  get accessToken(): string | null {
    return localStorage.getItem(ACCESS_KEY);
  }

  get isLoggedIn(): boolean {
    return !!this.accessToken && !!this.currentUser();
  }

  private saveSession(resp: AuthResponse): void {
    localStorage.setItem(ACCESS_KEY,  resp.accessToken);
    localStorage.setItem(REFRESH_KEY, resp.refreshToken);
    localStorage.setItem(USER_KEY,    JSON.stringify(resp.user));
    this.currentUser.set(resp.user);
  }

  private loadUser(): User | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch { return null; }
  }
}
