import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  trigger, style, transition, animate, state, stagger, query
} from '@angular/animations';
import { NavbarComponent } from '../navbar/navbar.component';
import { LoginModalComponent } from '../login-modal/login-modal.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, NavbarComponent, LoginModalComponent],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss'],
  animations: [
    // Simple fade-up for individual hero elements
    trigger('fadeUp', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(28px)' }),
        animate('{{delay}}ms {{duration}}ms cubic-bezier(.22,1,.36,1)',
          style({ opacity: 1, transform: 'translateY(0)' }))
      ], { params: { delay: 0, duration: 550 } })
    ]),
    // Scroll-reveal for section headers
    trigger('sectionReveal', [
      state('hidden',  style({ opacity: 0, transform: 'translateY(36px)' })),
      state('visible', style({ opacity: 1, transform: 'translateY(0)' })),
      transition('hidden => visible', animate('600ms cubic-bezier(.22,1,.36,1)'))
    ]),
    // Stagger for product/service cards
    trigger('staggerCards', [
      transition('hidden => visible', [
        query('.card', [
          style({ opacity: 0, transform: 'translateY(40px)' }),
          stagger(90, animate('500ms cubic-bezier(.22,1,.36,1)',
            style({ opacity: 1, transform: 'translateY(0)' })))
        ], { optional: true })
      ])
    ]),
    // Stagger for team cards
    trigger('staggerTeam', [
      transition('hidden => visible', [
        query('.card--team', [
          style({ opacity: 0, transform: 'translateY(40px) scale(0.95)' }),
          stagger(110, animate('500ms cubic-bezier(.34,1.56,.64,1)',
            style({ opacity: 1, transform: 'translateY(0) scale(1)' })))
        ], { optional: true })
      ])
    ]),
    // Float for stat cards
    trigger('floatIn', [
      transition(':enter', [
        query('.stat-card', [
          style({ opacity: 0, transform: 'translateY(32px) scale(0.95)' }),
          stagger(120, animate('500ms cubic-bezier(.34,1.56,.64,1)',
            style({ opacity: 1, transform: 'translateY(0) scale(1)' })))
        ], { optional: true })
      ])
    ]),
  ]
})
export class LandingComponent implements OnInit, OnDestroy {
  showLogin = signal(false);

  productosVisible = signal<'hidden' | 'visible'>('hidden');
  serviciosVisible = signal<'hidden' | 'visible'>('hidden');
  nosotrosVisible  = signal<'hidden' | 'visible'>('hidden');

  private observers: IntersectionObserver[] = [];

  products = [
    { icon: '💻', title: 'Laptops & PCs',  desc: 'Equipos de alto rendimiento para empresas y profesionales.' },
    { icon: '🖥️', title: 'Monitores',       desc: 'Pantallas 4K y ultra-wide para máxima productividad.' },
    { icon: '🖨️', title: 'Periféricos',     desc: 'Teclados, ratones y accesorios de última generación.' },
    { icon: '🔌', title: 'Redes',           desc: 'Infraestructura de red robusta para tu negocio.' },
  ];

  services = [
    { icon: '🛠️', title: 'Soporte Técnico', desc: 'Asistencia 24/7 para mantener tus sistemas operativos.' },
    { icon: '☁️', title: 'Cloud & Backup',  desc: 'Soluciones de respaldo en la nube seguras y confiables.' },
    { icon: '🔒', title: 'Ciberseguridad',  desc: 'Protección avanzada contra amenazas digitales.' },
  ];

  team = [
    { name: 'Ana Torres',   role: 'CEO & Fundadora',  emoji: '👩‍💼' },
    { name: 'Carlos Vega',  role: 'Director Técnico', emoji: '👨‍💻' },
    { name: 'María Ríos',   role: 'Soporte & Ventas', emoji: '👩‍🔧' },
  ];

  ngOnInit(): void {
    const observe = (id: string, setter: () => void) => {
      const el = document.getElementById(id);
      if (!el) return;
      const obs = new IntersectionObserver(
        ([entry]) => { if (entry.isIntersecting) { setter(); obs.disconnect(); } },
        { threshold: 0.12 }
      );
      obs.observe(el);
      this.observers.push(obs);
    };

    setTimeout(() => {
      observe('productos', () => this.productosVisible.set('visible'));
      observe('servicios', () => this.serviciosVisible.set('visible'));
      observe('nosotros',  () => this.nosotrosVisible.set('visible'));
    }, 200);
  }

  ngOnDestroy(): void {
    this.observers.forEach(o => o.disconnect());
  }
}
