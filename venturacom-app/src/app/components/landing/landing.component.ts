import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  trigger, style, transition, animate, query, stagger, keyframes, state
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
    trigger('heroEnter', [
      transition(':enter', [
        query('.hero__badge, .hero__title, .hero__subtitle, .hero__ctas', [
          style({ opacity: 0, transform: 'translateY(30px)' }),
          stagger(100, animate('0.55s cubic-bezier(.22,1,.36,1)', style({ opacity: 1, transform: 'translateY(0)' })))
        ], { optional: true })
      ])
    ]),
    trigger('floatCards', [
      transition(':enter', [
        query('.stat-card', [
          style({ opacity: 0, transform: 'translateY(40px) scale(0.95)' }),
          stagger(120, animate('0.5s cubic-bezier(.34,1.56,.64,1)', style({ opacity: 1, transform: 'translateY(0) scale(1)' })))
        ], { optional: true })
      ])
    ]),
    trigger('sectionReveal', [
      state('hidden', style({ opacity: 0, transform: 'translateY(40px)' })),
      state('visible', style({ opacity: 1, transform: 'translateY(0)' })),
      transition('hidden => visible', animate('0.6s cubic-bezier(.22,1,.36,1)'))
    ]),
    trigger('staggerCards', [
      transition('hidden => visible', [
        query('.card', [
          style({ opacity: 0, transform: 'translateY(40px)' }),
          stagger(100, animate('0.5s cubic-bezier(.22,1,.36,1)', style({ opacity: 1, transform: 'translateY(0)' })))
        ], { optional: true })
      ])
    ]),
    trigger('staggerTeam', [
      transition('hidden => visible', [
        query('.card--team', [
          style({ opacity: 0, transform: 'translateY(40px) scale(0.95)' }),
          stagger(120, animate('0.5s cubic-bezier(.34,1.56,.64,1)', style({ opacity: 1, transform: 'translateY(0) scale(1)' })))
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

  ngOnInit(): void {
    const observe = (id: string, setter: (v: 'visible') => void) => {
      const el = document.getElementById(id);
      if (!el) return;
      const obs = new IntersectionObserver(
        ([entry]) => { if (entry.isIntersecting) { setter('visible'); obs.disconnect(); } },
        { threshold: 0.15 }
      );
      obs.observe(el);
      this.observers.push(obs);
    };

    setTimeout(() => {
      observe('productos', () => this.productosVisible.set('visible'));
      observe('servicios', () => this.serviciosVisible.set('visible'));
      observe('nosotros',  () => this.nosotrosVisible.set('visible'));
    }, 100);
  }

  ngOnDestroy(): void {
    this.observers.forEach(o => o.disconnect());
  }
}
