import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { RegisterForm } from '../../dumb/register-form/register-form';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-register-container',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RegisterForm,
    MatCardModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="auth-page-container">
      <div class="auth-card-wrapper glass-panel">
        <div class="auth-header">
          <h1 class="gradient-text">Register</h1>
          <p class="subtle-text">Create an account to track your productivity.</p>
        </div>

        <div class="error-banner" *ngIf="errorMessage">
          {{errorMessage}}
        </div>

        <div class="spinner-wrapper" *ngIf="isLoading">
          <mat-spinner diameter="40"></mat-spinner>
        </div>

        <app-register-form *ngIf="!isLoading" (submitForm)="onRegister($event)"></app-register-form>

        <div class="auth-footer">
          <p class="subtle-text">
            Already have an account? 
            <a routerLink="/login" class="auth-link">Sign In</a>
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
export class RegisterContainer {
  protected isLoading = false;
  protected errorMessage = '';

  constructor(private authService: AuthService, private router: Router) {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  protected onRegister(data: any): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.authService.register(data.username, data.email, data.password).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.error || 'Registration failed. Check connection or try another email.';
      }
    });
  }
}
