import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { trigger, style, transition, animate } from '@angular/animations';
import { AuthService } from '../../services/auth.service';
import { OtpModalComponent } from '../otp-modal/otp-modal.component';

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
        animate('200ms ease', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('200ms ease', style({ opacity: 0 }))
      ])
    ]),
    trigger('modal', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(30px) scale(0.97)' }),
        animate('250ms cubic-bezier(.34,1.56,.64,1)', style({ opacity: 1, transform: 'translateY(0) scale(1)' }))
      ]),
      transition(':leave', [
        animate('180ms ease', style({ opacity: 0, transform: 'translateY(20px) scale(0.97)' }))
      ])
    ])
  ]
})
export class LoginModalComponent {
  auth = inject(AuthService);
  close = output<void>();

  email = signal('');
  loading = signal(false);
  error = signal('');
  showOtp = signal(false);

  isValidEmail(e: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(e);
  }

  async sendOtp(): Promise<void> {
    if (!this.isValidEmail(this.email())) {
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
        this.error.set(err?.error?.message ?? 'Error al enviar OTP. Intenta de nuevo.');
      }
    });
  }

  signInGoogle(): void {
    this.auth.signInWithGoogle();
  }

  onOtpVerified(): void {
    this.close.emit();
  }
}
