import { Component, inject, output, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  trigger, style, transition, animate, keyframes, query, stagger
} from '@angular/animations';
import { AuthService } from '../../services/auth.service';
import { OtpModalComponent } from '../otp-modal/otp-modal.component';
import { environment } from '../../../environments/environment';
import { Router } from '@angular/router';

declare const google: {
  accounts: {
    id: {
      initialize(cfg: { client_id: string; callback: (r: { credential: string }) => void; auto_select?: boolean }): void;
      renderButton(el: HTMLElement, opts: object): void;
      prompt(): void;
    };
  };
};

@Component({
  selector: 'app-login-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, OtpModalComponent],
  templateUrl: './login-modal.component.html',
  styleUrls: ['./login-modal.component.scss'],
  animations: [
    trigger('overlay', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('220ms ease', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('180ms ease', style({ opacity: 0 }))
      ])
    ]),
    trigger('modal', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(40px) scale(0.96)' }),
        animate('320ms cubic-bezier(.34,1.56,.64,1)', style({ opacity: 1, transform: 'translateY(0) scale(1)' }))
      ]),
      transition(':leave', [
        animate('200ms ease', style({ opacity: 0, transform: 'translateY(24px) scale(0.97)' }))
      ])
    ]),
    trigger('shake', [
      transition('* => shake', animate('400ms', keyframes([
        style({ transform: 'translateX(0)', offset: 0 }),
        style({ transform: 'translateX(-10px)', offset: 0.2 }),
        style({ transform: 'translateX(10px)', offset: 0.4 }),
        style({ transform: 'translateX(-8px)', offset: 0.6 }),
        style({ transform: 'translateX(8px)', offset: 0.8 }),
        style({ transform: 'translateX(0)', offset: 1 }),
      ])))
    ]),
    trigger('fadeSlide', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(8px)' }),
        animate('200ms ease', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('150ms ease', style({ opacity: 0, transform: 'translateY(-4px)' }))
      ])
    ]),
  ]
})
export class LoginModalComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  router = inject(Router);
  close = output<void>();

  email = signal('');
  loading = signal(false);
  googleLoading = signal(false);
  error = signal('');
  showOtp = signal(false);
  shakeState = signal('idle');

  ngOnInit(): void {
    this.initGoogleGsi();
  }

  ngOnDestroy(): void {}

  private initGoogleGsi(): void {
    const tryInit = () => {
      if (typeof google === 'undefined') { setTimeout(tryInit, 300); return; }
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback: (response) => this.handleGoogleCredential(response.credential),
        auto_select: false,
      });
    };
    tryInit();
  }

  private handleGoogleCredential(idToken: string): void {
    this.googleLoading.set(true);
    this.error.set('');
    this.auth.loginWithGoogle(idToken).subscribe({
      next: () => {
        this.googleLoading.set(false);
        this.close.emit();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.googleLoading.set(false);
        this.triggerShake();
        this.error.set(err?.error?.message ?? 'Error al iniciar con Google.');
      }
    });
  }

  signInGoogle(): void {
    if (typeof google === 'undefined') {
      this.error.set('Google Sign-In no está disponible. Verifica tu conexión.');
      return;
    }
    google.accounts.id.prompt();
  }

  isValidEmail(e: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(e);
  }

  sendOtp(): void {
    if (!this.isValidEmail(this.email())) {
      this.triggerShake();
      this.error.set('Ingresa un correo válido');
      return;
    }
    this.error.set('');
    this.loading.set(true);
    this.auth.sendOtp(this.email()).subscribe({
      next: () => {
        this.loading.set(false);
        this.showOtp.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.triggerShake();
        this.error.set(err?.error?.message ?? 'No se pudo enviar el código. Intenta de nuevo.');
      }
    });
  }

  private triggerShake(): void {
    this.shakeState.set('shake');
    setTimeout(() => this.shakeState.set('idle'), 420);
  }

  onOtpVerified(): void {
    this.close.emit();
  }
}
