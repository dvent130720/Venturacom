import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  trigger, style, transition, animate, query, group
} from '@angular/animations';
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
    trigger('sectionSlide', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateX(18px)' })
        ], { optional: true }),
        query(':leave', [
          animate('160ms ease', style({ opacity: 0, transform: 'translateX(-12px)' }))
        ], { optional: true }),
        query(':enter', [
          animate('240ms ease', style({ opacity: 1, transform: 'translateX(0)' }))
        ], { optional: true }),
      ])
    ])
  ]
})
export class DashboardComponent {
  auth = inject(AuthService);
  activeSection = signal<DashSection>('ventas');

  get today(): string {
    return new Date().toLocaleDateString('es-MX', {
      weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
    });
  }
}
