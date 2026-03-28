import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { User } from '../models/user.model';
import { Observable, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API_URL = 'http://localhost:5000/api/auth';

  currentUser = signal<User | null>(null);
  isAuthenticated = signal<boolean>(false);

  constructor(private http: HttpClient, private router: Router) {
    const stored = localStorage.getItem('vc_user');
    if (stored) {
      const user = JSON.parse(stored) as User;
      this.currentUser.set(user);
      this.isAuthenticated.set(true);
    }
  }

  sendOtp(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/send-otp`, { email });
  }

  verifyOtp(email: string, otp: string): Observable<{ token: string; user: User }> {
    return this.http.post<{ token: string; user: User }>(`${this.API_URL}/verify-otp`, { email, otp }).pipe(
      tap(res => {
        localStorage.setItem('vc_user', JSON.stringify(res.user));
        localStorage.setItem('vc_token', res.token);
        this.currentUser.set(res.user);
        this.isAuthenticated.set(true);
      })
    );
  }

  signInWithGoogle(): void {
    // Redirect to backend Google OAuth
    window.location.href = `${this.API_URL}/google`;
  }

  logout(): void {
    localStorage.removeItem('vc_user');
    localStorage.removeItem('vc_token');
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(['/']);
  }
}
