import { Component, inject, input, output, signal, ViewChildren, QueryList, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { trigger, style, transition, animate } from '@angular/animations';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-otp-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './otp-modal.component.html',
  styleUrls: ['./otp-modal.component.scss'],
  animations: [
    trigger('floatIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'scale(0.9) translateY(20px)' }),
        animate('300ms cubic-bezier(.34,1.56,.64,1)', style({ opacity: 1, transform: 'scale(1) translateY(0)' }))
      ])
    ])
  ]
})
export class OtpModalComponent implements AfterViewInit {
  auth = inject(AuthService);
  router = inject(Router);

  email = input.required<string>();
  verified = output<void>();
  back = output<void>();

  @ViewChildren('digitInput') digitInputs!: QueryList<ElementRef<HTMLInputElement>>;

  digits = signal<string[]>(['', '', '', '', '', '']);
  loading = signal(false);
  error = signal('');
  resendCountdown = signal(30);
  private countdownTimer?: ReturnType<typeof setInterval>;

  ngAfterViewInit(): void {
    setTimeout(() => this.digitInputs.first?.nativeElement.focus(), 100);
    this.startCountdown();
  }

  private startCountdown(): void {
    this.resendCountdown.set(30);
    clearInterval(this.countdownTimer);
    this.countdownTimer = setInterval(() => {
      const v = this.resendCountdown() - 1;
      if (v <= 0) { clearInterval(this.countdownTimer); this.resendCountdown.set(0); }
      else this.resendCountdown.set(v);
    }, 1000);
  }

  onDigitInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = input.value.replace(/\D/g, '').slice(-1);
    const arr = [...this.digits()];
    arr[index] = val;
    this.digits.set(arr);
    if (val && index < 5) {
      this.digitInputs.get(index + 1)?.nativeElement.focus();
    }
    if (arr.every(d => d) && arr.join('').length === 6) {
      this.verify();
    }
  }

  onKeyDown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && !this.digits()[index] && index > 0) {
      this.digitInputs.get(index - 1)?.nativeElement.focus();
    }
  }

  onPaste(event: ClipboardEvent): void {
    const text = event.clipboardData?.getData('text') ?? '';
    const nums = text.replace(/\D/g, '').slice(0, 6).split('');
    if (nums.length === 6) {
      this.digits.set(nums);
      this.digitInputs.last?.nativeElement.focus();
      setTimeout(() => this.verify(), 100);
    }
    event.preventDefault();
  }

  verify(): void {
    const code = this.digits().join('');
    if (code.length !== 6) { this.error.set('Ingresa el código completo'); return; }
    this.error.set('');
    this.loading.set(true);
    this.auth.verifyOtp(this.email(), code).subscribe({
      next: () => {
        this.loading.set(false);
        this.verified.emit();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Código incorrecto. Intenta de nuevo.');
        this.digits.set(['', '', '', '', '', '']);
        setTimeout(() => this.digitInputs.first?.nativeElement.focus(), 50);
      }
    });
  }

  resend(): void {
    if (this.resendCountdown() > 0) return;
    this.auth.sendOtp(this.email()).subscribe({
      next: () => this.startCountdown(),
      error: () => this.error.set('Error al reenviar. Intenta de nuevo.')
    });
  }

  trackByIndex(index: number): number { return index; }
}
