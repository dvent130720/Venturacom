import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, style, transition, animate } from '@angular/animations';
import { AuthService } from '../../services/auth.service';
import { SidebarComponent } from './sidebar/sidebar.component';
import { VentasComponent } from './ventas/ventas.component';
import { BalancesComponent } from './balances/balances.component';
import { IaModuleComponent } from './ia-module/ia-module.component';

export type DashSection = 'ventas' | 'balances' | 'ia';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, SidebarComponent, VentasComponent, BalancesComponent, IaModuleComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  animations: [
    trigger('fadeSection', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(12px)' }),
        animate('250ms ease', style({ opacity: 1, transform: 'translateX(0)' }))
      ])
    ])
  ]
})
export class DashboardComponent {
  auth = inject(AuthService);
  activeSection = signal<DashSection>('ventas');

  get today(): string {
    return new Date().toLocaleDateString('es-MX', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
  }
}
