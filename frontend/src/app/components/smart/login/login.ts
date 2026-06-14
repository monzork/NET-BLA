import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { LoginForm } from '../../dumb/login-form/login-form';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-login-container',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LoginForm,
    MatCardModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="auth-page-container">
      <div class="auth-card-wrapper glass-panel">
        <div class="auth-header">
          <h1 class="gradient-text">Sign In</h1>
          <p class="subtle-text">Manage your day, accomplish your goals.</p>
        </div>

        <div class="error-banner" *ngIf="errorMessage">
          {{errorMessage}}
        </div>

        <div class="spinner-wrapper" *ngIf="isLoading">
          <mat-spinner diameter="40"></mat-spinner>
        </div>

        <app-login-form *ngIf="!isLoading" (submitForm)="onLogin($event)"></app-login-form>

        <div class="auth-footer">
          <p class="subtle-text">
            Don't have an account? 
            <a routerLink="/register" class="auth-link">Create Account</a>
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      padding: 20px;
    }
    .auth-card-wrapper {
      width: 100%;
      max-width: 440px;
      padding: 40px;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }
    .auth-header {
      text-align: center;
    }
    .auth-header h1 {
      font-size: 32px;
      margin: 0 0 8px 0;
    }
    .auth-header p {
      margin: 0;
      font-size: 14px;
    }
    .error-banner {
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.2);
      color: #fca5a5;
      padding: 12px;
      border-radius: 8px;
      font-size: 14px;
      text-align: center;
    }
    .spinner-wrapper {
      display: flex;
      justify-content: center;
      padding: 20px 0;
    }
    .auth-footer {
      text-align: center;
      font-size: 14px;
      margin-top: 12px;
    }
    .auth-link {
      color: #c084fc;
      text-decoration: none;
      font-weight: 600;
      transition: color 0.2s ease;
    }
    .auth-link:hover {
      color: #e879f9;
      text-decoration: underline;
    }
  `]
})
export class LoginContainer {
  protected isLoading = false;
  protected errorMessage = '';

  constructor(private authService: AuthService, private router: Router) {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  protected onLogin(credentials: any): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.authService.login(credentials.email, credentials.password).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.error || 'Invalid credentials or connection issue.';
      }
    });
  }
}
