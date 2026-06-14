import { Routes } from '@angular/router';
import { LoginContainer } from './components/smart/login/login';
import { RegisterContainer } from './components/smart/register/register';
import { TaskDashboardContainer } from './components/smart/dashboard/dashboard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginContainer },
  { path: 'register', component: RegisterContainer },
  { path: 'dashboard', component: TaskDashboardContainer },
  { path: '**', redirectTo: '/login' }
];
