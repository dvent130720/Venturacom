import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashSection } from '../dashboard.component';
import { User } from '../../../models/user.model';

interface NavItem {
  id: DashSection;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
  active = input.required<DashSection>();
  user = input<User | null>(null);
  navigate = output<DashSection>();
  logout = output<void>();

  collapsed = false;

  navItems: NavItem[] = [
    { id: 'ventas', label: 'Ventas', icon: '📊' },
    { id: 'balances', label: 'Balances', icon: '💰' },
    { id: 'ia', label: 'Módulo IA', icon: '🤖' },
  ];

  initials(): string {
    const name = this.user()?.name ?? this.user()?.email ?? 'U';
    return name.charAt(0).toUpperCase();
  }
}
