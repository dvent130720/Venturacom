import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { User } from '../models/user.model';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SendOtpResponse {
  message: string;
}

export interface VerifyOtpResponse {
  token: string;
  user: User;
}

export interface GoogleAuthResponse {
  token: string;
  user: User;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = environment.apiUrl;

  currentUser = signal<User | null>(null);
  isAuthenticated = signal<boolean>(false);

  constructor(private http: HttpClient, private router: Router) {
    const stored = localStorage.getItem('vc_user');
    const token = localStorage.getItem('vc_token');
    if (stored && token) {
      this.currentUser.set(JSON.parse(stored) as User);
      this.isAuthenticated.set(true);
    }
  }

  /** POST /api/auth/login-otp  →  { email } */
  sendOtp(email: string): Observable<SendOtpResponse> {
    return this.http.post<SendOtpResponse>(`${this.API}/auth/login-otp`, { email });
  }

  /** POST /api/auth/validate-otp  →  { email, code } */
  verifyOtp(email: string, code: string): Observable<VerifyOtpResponse> {
    return this.http.post<VerifyOtpResponse>(`${this.API}/auth/validate-otp`, { email, code }).pipe(
      tap(res => this.persistSession(res.token, res.user))
    );
  }

  /** POST /api/auth/google  →  { idToken } */
  loginWithGoogle(idToken: string): Observable<GoogleAuthResponse> {
    return this.http.post<GoogleAuthResponse>(`${this.API}/auth/google`, { idToken }).pipe(
      tap(res => this.persistSession(res.token, res.user))
    );
  }

  private persistSession(token: string, user: User): void {
    localStorage.setItem('vc_token', token);
    localStorage.setItem('vc_user', JSON.stringify(user));
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
  }

  logout(): void {
    localStorage.removeItem('vc_user');
    localStorage.removeItem('vc_token');
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(['/']);
  }
}
