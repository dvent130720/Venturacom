import { Component, signal, inject, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  time: string;
}

@Component({
  selector: 'app-ia-module',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ia-module.component.html',
  styleUrls: ['./ia-module.component.scss']
})
export class IaModuleComponent implements AfterViewChecked {
  http = inject(HttpClient);
  @ViewChild('chatEnd') chatEnd!: ElementRef;

  messages = signal<Message[]>([
    {
      role: 'assistant',
      content: '¡Hola! Soy el asistente IA de VenturaComp. Puedo ayudarte con consultas sobre ventas, inventario, clientes y más. ¿En qué te puedo ayudar?',
      time: this.now()
    }
  ]);

  input = signal('');
  loading = signal(false);

  private readonly API = 'http://localhost:5000/api/ia/chat';

  suggestions = [
    '¿Cuántas ventas tuvimos este mes?',
    '¿Qué productos tienen más demanda?',
    'Genera un reporte de balance',
    '¿Cómo puedo mejorar mis ventas?',
  ];

  sendSuggestion(text: string): void {
    this.input.set(text);
    this.send();
  }

  send(): void {
    const msg = this.input().trim();
    if (!msg || this.loading()) return;

    this.messages.update(m => [...m, { role: 'user', content: msg, time: this.now() }]);
    this.input.set('');
    this.loading.set(true);

    this.http.post<{ reply: string }>(this.API, { message: msg }).subscribe({
      next: (res) => {
        this.messages.update(m => [...m, { role: 'assistant', content: res.reply, time: this.now() }]);
        this.loading.set(false);
      },
      error: () => {
        // Fallback demo response
        const demos = [
          'Basado en los datos del sistema, este mes las ventas han aumentado un 12% respecto al mes anterior.',
          'Los productos con mayor demanda son Laptops HP ProBook y Monitores Dell 27".',
          'Tu balance neto del mes es positivo: $97,200 en ingresos netos.',
          'Te recomiendo enfocarte en los clientes con órdenes pendientes y activar campañas de seguimiento.',
        ];
        const reply = demos[Math.floor(Math.random() * demos.length)];
        this.messages.update(m => [...m, { role: 'assistant', content: reply, time: this.now() }]);
        this.loading.set(false);
      }
    });
  }

  private now(): string {
    return new Date().toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit' });
  }

  ngAfterViewChecked(): void {
    this.chatEnd?.nativeElement?.scrollIntoView({ behavior: 'smooth' });
  }
}
