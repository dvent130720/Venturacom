import { Component, signal, inject, HostListener, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { trigger, style, transition, animate } from '@angular/animations';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  animations: [
    trigger('mobileMenu', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(-10px)' }),
        animate('200ms ease', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('160ms ease', style({ opacity: 0, transform: 'translateY(-8px)' }))
      ])
    ])
  ]
})
export class NavbarComponent {
  auth = inject(AuthService);
  router = inject(Router);
  openLogin = output<void>();

  mobileOpen = signal(false);
  scrolled = signal(false);

  sections = [
    { label: 'Productos', anchor: 'productos' },
    { label: 'Servicios', anchor: 'servicios' },
    { label: 'Quiénes Somos', anchor: 'nosotros' },
  ];

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolled.set(window.scrollY > 24);
  }

  scrollTo(anchor: string): void {
    const el = document.getElementById(anchor);
    if (el) el.scrollIntoView({ behavior: 'smooth' });
    this.mobileOpen.set(false);
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
