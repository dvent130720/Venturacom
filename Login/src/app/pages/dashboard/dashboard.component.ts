import { Component, inject, OnInit } from '@angular/core';
import { CommonModule }              from '@angular/common';
import { Router }                    from '@angular/router';
import { AuthService }               from '../../services/auth.service';

@Component({
  selector:    'app-dashboard',
  standalone:  true,
  imports:     [CommonModule],
  template: `
    <div class="dash-wrapper">
      <div class="dash-card">
        @if (isNew) {
          <div class="welcome-banner">🎉 ¡Bienvenido a Venturacom!</div>
        }
        <div class="avatar">
          @if (user?.avatarUrl) {
            <img [src]="user!.avatarUrl" [alt]="user!.name ?? user!.email" />
          } @else {
            <span>{{ initial }}</span>
          }
        </div>
        <h2>{{ user?.name ?? user?.email }}</h2>
        <p class="email">{{ user?.email }}</p>
        <p class="provider">Acceso vía <strong>{{ user?.provider }}</strong></p>
        <button class="btn-logout" (click)="logout()">Cerrar sesión</button>
      </div>
    </div>
  `,
  styles: [`
    .dash-wrapper { min-height:100vh; display:flex; align-items:center; justify-content:center;
      background:linear-gradient(135deg,#eef2ff,#f9fafb,#ede9fe); padding:24px; }
    .dash-card { background:#fff; border-radius:20px; padding:40px; text-align:center;
      max-width:380px; width:100%; box-shadow:0 4px 24px rgba(0,0,0,.08); }
    .welcome-banner { background:#f0fdf4; color:#15803d; padding:10px 16px; border-radius:8px;
      font-weight:600; margin-bottom:24px; font-size:.9rem; }
    .avatar { width:80px; height:80px; border-radius:50%; background:#eef2ff; color:#4f46e5;
      display:flex; align-items:center; justify-content:center; font-size:2rem; font-weight:700;
      margin:0 auto 16px; overflow:hidden;
      img { width:100%; height:100%; object-fit:cover; } }
    h2 { font-size:1.25rem; font-weight:700; margin-bottom:4px; }
    .email { color:#6b7280; font-size:.875rem; margin-bottom:8px; }
    .provider { color:#6b7280; font-size:.8rem; margin-bottom:28px; }
    .btn-logout { background:#ef4444; color:#fff; padding:10px 28px; border-radius:10px;
      font-weight:600; cursor:pointer; border:none; font-size:.9rem;
      &:hover { background:#dc2626; } }
  `]
})
export class DashboardComponent implements OnInit {
  private authSvc = inject(AuthService);
  private router  = inject(Router);

  user   = this.authSvc.currentUser();
  isNew  = false;
  initial = '';

  ngOnInit(): void {
    const nav = this.router.getCurrentNavigation();
    this.isNew = nav?.extras?.state?.['isNew'] ?? false;
    this.initial = (this.user?.name ?? this.user?.email ?? 'U')[0].toUpperCase();
  }

  logout(): void { this.authSvc.logout(); }
}
