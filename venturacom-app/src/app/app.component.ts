import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  animations: [
    trigger('routeFade', [
      transition('* <=> *', [
        style({ opacity: 0 }),
        animate('220ms ease', style({ opacity: 1 }))
      ])
    ])
  ]
})
export class AppComponent {
  title = 'venturacom-app';

  getRoute(outlet: RouterOutlet): string {
    return outlet?.activatedRoute?.snapshot?.url?.[0]?.path ?? '';
  }
}
