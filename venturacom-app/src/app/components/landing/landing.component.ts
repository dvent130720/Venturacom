import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, style, transition, animate, query, stagger } from '@angular/animations';
import { NavbarComponent } from '../navbar/navbar.component';
import { LoginModalComponent } from '../login-modal/login-modal.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, NavbarComponent, LoginModalComponent],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss'],
  animations: [
    trigger('fadeUp', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(32px)' }),
        animate('0.6s ease', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ]),
    trigger('staggerCards', [
      transition(':enter', [
        query('.card', [
          style({ opacity: 0, transform: 'translateY(40px)' }),
          stagger(120, animate('0.5s ease', style({ opacity: 1, transform: 'translateY(0)' })))
        ], { optional: true })
      ])
    ])
  ]
})
export class LandingComponent {
  showLogin = signal(false);

  products = [
    { icon: '💻', title: 'Laptops & PCs', desc: 'Equipos de alto rendimiento para empresas y profesionales.' },
    { icon: '🖥️', title: 'Monitores', desc: 'Pantallas 4K y ultra-wide para máxima productividad.' },
    { icon: '🖨️', title: 'Periféricos', desc: 'Teclados, ratones y accesorios de última generación.' },
    { icon: '🔌', title: 'Redes', desc: 'Infraestructura de red robusta para tu negocio.' },
  ];

  services = [
    { icon: '🛠️', title: 'Soporte Técnico', desc: 'Asistencia 24/7 para mantener tus sistemas operativos.' },
    { icon: '☁️', title: 'Cloud & Backup', desc: 'Soluciones de respaldo en la nube seguras y confiables.' },
    { icon: '🔒', title: 'Ciberseguridad', desc: 'Protección avanzada contra amenazas digitales.' },
  ];

  team = [
    { name: 'Ana Torres', role: 'CEO & Fundadora', emoji: '👩‍💼' },
    { name: 'Carlos Vega', role: 'Director Técnico', emoji: '👨‍💻' },
    { name: 'María Ríos', role: 'Soporte & Ventas', emoji: '👩‍🔧' },
  ];
}
