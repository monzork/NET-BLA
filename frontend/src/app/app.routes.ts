import { Routes } from '@angular/router';
import { LoginContainer } from './components/smart/login/login';
import { TaskDashboardContainer } from './components/smart/dashboard/dashboard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginContainer },
  { path: 'dashboard', component: TaskDashboardContainer },
  { path: '**', redirectTo: '/login' }
];
