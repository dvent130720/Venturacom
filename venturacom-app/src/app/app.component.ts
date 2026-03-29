import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { trigger, transition, style, animate, query, group } from '@angular/animations';

export const routeTransition = trigger('routeTransition', [
  transition('* <=> *', [
    query(':enter, :leave', [
      style({ position: 'absolute', width: '100%', top: 0, left: 0 })
    ], { optional: true }),
    group([
      query(':leave', [
        animate('200ms ease', style({ opacity: 0, transform: 'translateY(-12px)' }))
      ], { optional: true }),
      query(':enter', [
        style({ opacity: 0, transform: 'translateY(16px)' }),
        animate('300ms 120ms ease', style({ opacity: 1, transform: 'translateY(0)' }))
      ], { optional: true }),
    ])
  ])
]);

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  animations: [routeTransition]
})
export class AppComponent {
  title = 'venturacom-app';

  getRouteData(outlet: RouterOutlet): string {
    return outlet?.activatedRouteData?.['animation'] ?? outlet?.activatedRoute?.snapshot?.url?.[0]?.path ?? '';
  }
}
