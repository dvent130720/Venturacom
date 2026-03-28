import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-ventas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ventas.component.html',
  styleUrls: ['./ventas.component.scss']
})
export class VentasComponent {
  stats = [
    { label: 'Ventas del mes', value: '$48,320', delta: '+12%', up: true, icon: '📈' },
    { label: 'Órdenes activas', value: '34', delta: '+5', up: true, icon: '🛒' },
    { label: 'Clientes nuevos', value: '18', delta: '+8%', up: true, icon: '👥' },
    { label: 'Devoluciones', value: '3', delta: '-2', up: false, icon: '↩️' },
  ];

  recientes = [
    { cliente: 'Tech Corp', producto: 'Laptop HP ProBook', monto: '$2,400', estado: 'Completado' },
    { cliente: 'Innova SA', producto: 'Monitor Dell 27"', monto: '$850', estado: 'Completado' },
    { cliente: 'Grupo Nexo', producto: 'Switch 24p', monto: '$1,200', estado: 'Pendiente' },
    { cliente: 'Softland', producto: 'Pack Teclados x10', monto: '$640', estado: 'En proceso' },
    { cliente: 'ALEM', producto: 'Router Enterprise', monto: '$3,100', estado: 'Completado' },
  ];

  statusClass(estado: string): string {
    return { 'Completado': 'status--ok', 'Pendiente': 'status--warn', 'En proceso': 'status--info' }[estado] ?? '';
  }
}
