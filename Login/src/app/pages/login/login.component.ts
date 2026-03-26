import {
  Component, OnInit, inject,
  signal, computed, NgZone,
  ChangeDetectionStrategy, AfterViewInit
} from '@angular/core';
import { CommonModule }   from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { Router }         from '@angular/router';
import { AuthService }    from '../../services/auth.service';
import { environment }    from '../../../environments/environment';
import { LoginStep }      from '../../models/auth.models';

declare const google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrls:   ['./login.component.scss'],
})
export class LoginComponent implements OnInit, AfterViewInit {
  private auth   = inject(AuthService);
  private router = inject(Router);
  private zone   = inject(NgZone);

  // ── Estado ───────────────────────────────────────────────────────────────
  step       = signal<LoginStep>('initial');
  email      = signal('');
  loading    = signal(false);
  errorMsg   = signal('');
  successMsg = signal('');

  otpDigits  = signal<string[]>(['', '', '', '', '', '']);
  codeString = computed(() => this.otpDigits().join(''));

  // Estado SDK Google
  googleReady = signal(false);
  googleError = signal('');

  readonly currentYear = new Date().getFullYear();

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    if (this.auth.isLoggedIn) { this.router.navigate(['/dashboard']); return; }
  }

  ngAfterViewInit(): void {
    this.initGoogleClient();
  }

  // ── Google ────────────────────────────────────────────────────────────────
  private tokenClient: any;
  private retryCount = 0;
  private readonly MAX_RETRIES = 20; // 6 segundos máximo

  private initGoogleClient(): void {
    if (this.retryCount >= this.MAX_RETRIES) {
      this.zone.run(() =>
        this.googleError.set('No se pudo cargar el SDK de Google. Verifica tu conexión.')
      );
      return;
    }

    if (typeof google === 'undefined' || !google?.accounts?.oauth2) {
      this.retryCount++;
      setTimeout(() => this.initGoogleClient(), 300);
      return;
    }

    try {
      this.tokenClient = google.accounts.oauth2.initTokenClient({
        client_id: environment.googleClientId,
        scope:     'openid email profile',
        callback:  (tokenResp: any) => {
          if (tokenResp.error) {
            this.zone.run(() => {
              this.loading.set(false);
              this.errorMsg.set('Acceso con Google cancelado o denegado.');
            });
            return;
          }
          this.zone.run(() => this.handleGoogleToken(tokenResp.access_token));
        },
        error_callback: (err: any) => {
          this.zone.run(() => {
            this.loading.set(false);
            if (err?.type !== 'popup_closed') {
              this.errorMsg.set('Error al iniciar con Google. Intenta de nuevo.');
            }
          });
        },
      });
      this.zone.run(() => this.googleReady.set(true));
    } catch (err: any) {
      this.zone.run(() =>
        this.googleError.set(
          'Error al inicializar Google Sign-In. ' +
          (environment.googleClientId.includes('CHANGE_ME')
            ? 'Configura el GOOGLE_CLIENT_ID en environment.ts'
            : err?.message ?? '')
        )
      );
    }
  }

  onGoogleLogin(): void {
    this.errorMsg.set('');
    this.loading.set(true);
    this.tokenClient.requestAccessToken({ prompt: 'select_account' });
  }

  private handleGoogleToken(accessToken: string): void {
    this.auth.loginWithGoogleAccessToken(accessToken).subscribe({
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

  // ── Email OTP flow ────────────────────────────────────────────────────────
  onSendOtp(): void {
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

  onVerifyOtp(): void {
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

  // ── Utils ─────────────────────────────────────────────────────────────────
  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }
}
