import {
  Component, OnInit, inject,
  signal, computed, NgZone,
  ChangeDetectionStrategy, AfterViewInit
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

  readonly currentYear = new Date().getFullYear();

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    if (this.auth.isLoggedIn) { this.router.navigate(['/dashboard']); return; }
  }

  ngAfterViewInit(): void {
    this.initGoogleClient();
  }

  // ── Google (OAuth2 Token Client) ──────────────────────────────────────────
  private tokenClient: any;

  private initGoogleClient(): void {
    const tryInit = () => {
      if (typeof google === 'undefined' || !google?.accounts?.oauth2) {
        setTimeout(tryInit, 300);
        return;
      }
      this.tokenClient = google.accounts.oauth2.initTokenClient({
        client_id: environment.googleClientId,
        scope:     'openid email profile',
        callback:  (tokenResp: any) => {
          this.zone.run(() => this.fetchGoogleUserAndLogin(tokenResp.access_token));
        },
      });
    };
    tryInit();
  }

  onGoogleLogin(): void {
    this.errorMsg.set('');
    if (!this.tokenClient) {
      this.errorMsg.set('El SDK de Google no está listo. Intenta de nuevo.');
      return;
    }
    this.tokenClient.requestAccessToken();
  }

  private fetchGoogleUserAndLogin(accessToken: string): void {
    this.loading.set(true);
    // Obtenemos info del usuario con el access token para construir el id_token
    // En GSI usamos el access_token para llamar a la API de userinfo
    fetch('https://www.googleapis.com/oauth2/v3/userinfo', {
      headers: { Authorization: `Bearer ${accessToken}` },
    })
      .then(r => r.json())
      .then((info: any) => {
        // Enviamos sub + email + name al backend como credencial verificable
        // En producción deberías usar el flujo de id_token con renderButton o
        // el authorization code flow. Para este caso enviamos el access_token
        // y el backend lo valida contra Google's tokeninfo endpoint.
        return this.auth.loginWithGoogleAccessToken(accessToken).toPromise();
      })
      .then((resp: any) => {
        this.loading.set(false);
        this.router.navigate(['/dashboard'], { state: { isNew: resp?.isNewUser } });
      })
      .catch((err: any) => {
        this.loading.set(false);
        this.errorMsg.set(err?.error?.error ?? 'Error al iniciar con Google.');
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
