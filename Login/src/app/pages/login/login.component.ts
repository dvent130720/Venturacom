import {
  Component, OnInit, OnDestroy, inject,
  signal, computed, NgZone, ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule }        from '@angular/common';
import { FormsModule }         from '@angular/forms';
import { Router }              from '@angular/router';
import { AuthService }         from '../../services/auth.service';
import { environment }         from '../../../environments/environment';
import { LoginStep }           from '../../models/auth.models';

declare const google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrls:   ['./login.component.scss'],
})
export class LoginComponent implements OnInit, OnDestroy {
  private auth   = inject(AuthService);
  private router = inject(Router);
  private zone   = inject(NgZone);

  // ── Estado ───────────────────────────────────────────────────────────────
  step      = signal<LoginStep>('initial');
  email     = signal('');
  otpCode   = signal('');
  loading   = signal(false);
  errorMsg  = signal('');
  successMsg = signal('');

  otpDigits  = signal<string[]>(['', '', '', '', '', '']);
  codeString = computed(() => this.otpDigits().join(''));

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    if (this.auth.isLoggedIn) { this.router.navigate(['/dashboard']); return; }
    this.initGoogleButton();
  }

  ngOnDestroy(): void { /* cleanup si fuera necesario */ }

  private initGoogleButton(): void {
    const tryInit = () => {
      if (typeof google === 'undefined') { setTimeout(tryInit, 300); return; }
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback:  (resp: any) => this.zone.run(() => this.handleGoogleResponse(resp)),
      });
      google.accounts.id.renderButton(
        document.getElementById('google-btn'),
        { theme: 'outline', size: 'large', width: 360, text: 'continue_with', shape: 'rectangular' }
      );
    };
    tryInit();
  }

  // ── Email OTP flow ────────────────────────────────────────────────────────
  async onSendOtp(): Promise<void> {
    const emailVal = this.email().trim();
    if (!emailVal || !this.isValidEmail(emailVal)) {
      this.errorMsg.set('Ingresa un correo válido.'); return;
    }
    this.loading.set(true);
    this.errorMsg.set('');

    this.auth.sendOtp(emailVal).subscribe({
      next: () => {
        this.loading.set(false);
        this.step.set('otp');
        this.successMsg.set(`Código enviado a ${emailVal}`);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err?.error?.error ?? 'Error al enviar el código.');
      },
    });
  }

  async onVerifyOtp(): Promise<void> {
    const code = this.codeString();
    if (code.length !== 6) { this.errorMsg.set('Ingresa los 6 dígitos.'); return; }
    this.loading.set(true);
    this.errorMsg.set('');

    this.auth.verifyOtp(this.email().trim(), code).subscribe({
      next: (resp) => {
        this.loading.set(false);
        this.router.navigate(['/dashboard'], { state: { isNew: resp.isNewUser } });
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err?.error?.error ?? 'Código incorrecto.');
      },
    });
  }

  onBackToEmail(): void {
    this.step.set('initial');
    this.otpDigits.set(['', '', '', '', '', '']);
    this.errorMsg.set('');
    this.successMsg.set('');
  }

  // ── OTP input helpers ─────────────────────────────────────────────────────
  onDigitInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    const val   = input.value.replace(/\D/g, '').slice(-1);
    const digits = [...this.otpDigits()];
    digits[index] = val;
    this.otpDigits.set(digits);
    if (val && index < 5) {
      (document.getElementById(`otp-${index + 1}`) as HTMLInputElement)?.focus();
    }
  }

  onDigitKeydown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace' && !this.otpDigits()[index] && index > 0) {
      (document.getElementById(`otp-${index - 1}`) as HTMLInputElement)?.focus();
    }
  }

  onDigitPaste(event: ClipboardEvent): void {
    const text = event.clipboardData?.getData('text') ?? '';
    const digits = text.replace(/\D/g, '').slice(0, 6).split('');
    if (digits.length === 6) {
      this.otpDigits.set(digits);
      (document.getElementById('otp-5') as HTMLInputElement)?.focus();
      event.preventDefault();
    }
  }

  // ── Google ────────────────────────────────────────────────────────────────
  private handleGoogleResponse(response: { credential: string }): void {
    this.loading.set(true);
    this.errorMsg.set('');

    this.auth.loginWithGoogle(response.credential).subscribe({
      next: (resp) => {
        this.loading.set(false);
        this.router.navigate(['/dashboard'], { state: { isNew: resp.isNewUser } });
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err?.error?.error ?? 'Error al iniciar con Google.');
      },
    });
  }

  readonly currentYear = new Date().getFullYear();

  // ── Utils ─────────────────────────────────────────────────────────────────
  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }
}
