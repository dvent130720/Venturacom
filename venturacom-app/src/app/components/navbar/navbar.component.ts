import { Component, signal, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {
  auth = inject(AuthService);
  router = inject(Router);
  openLogin = output<void>();
  mobileOpen = signal(false);

  sections = [
    { label: 'Productos', anchor: 'productos' },
    { label: 'Servicios', anchor: 'servicios' },
    { label: 'Quiénes Somos', anchor: 'nosotros' },
  ];

  scrollTo(anchor: string): void {
    const el = document.getElementById(anchor);
    if (el) el.scrollIntoView({ behavior: 'smooth' });
    this.mobileOpen.set(false);
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
