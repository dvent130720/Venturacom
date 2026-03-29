import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-balances',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './balances.component.html',
  styleUrls: ['./balances.component.scss']
})
export class BalancesComponent {
  summary = [
    { label: 'Ingresos totales', value: '$128,400', icon: '💵', color: '#22c55e' },
    { label: 'Gastos del mes', value: '$31,200', icon: '📤', color: '#ef4444' },
    { label: 'Balance neto', value: '$97,200', icon: '🏦', color: '#a78bfa' },
    { label: 'Pendiente cobro', value: '$12,500', icon: '⏳', color: '#eab308' },
  ];

  movimientos = [
    { fecha: '28 Mar', descripcion: 'Pago Tech Corp', tipo: 'ingreso', monto: '+$2,400' },
    { fecha: '27 Mar', descripcion: 'Compra inventario HP', tipo: 'egreso', monto: '-$8,200' },
    { fecha: '26 Mar', descripcion: 'Pago Innova SA', tipo: 'ingreso', monto: '+$850' },
    { fecha: '25 Mar', descripcion: 'Servicios cloud mensual', tipo: 'egreso', monto: '-$450' },
    { fecha: '24 Mar', descripcion: 'Pago Grupo Nexo', tipo: 'ingreso', monto: '+$3,100' },
    { fecha: '22 Mar', descripcion: 'Mantenimiento equipos', tipo: 'egreso', monto: '-$750' },
  ];
}
