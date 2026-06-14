import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="register-form">
      <mat-form-field appearance="fill" class="form-field">
        <mat-label>Username</mat-label>
        <input matInput type="text" formControlName="username" placeholder="e.g. johndoe" />
        <mat-icon matSuffix>person</mat-icon>
        <mat-error *ngIf="registerForm.get('username')?.hasError('required')">Username is required</mat-error>
      </mat-form-field>

      <mat-form-field appearance="fill" class="form-field">
        <mat-label>Email Address</mat-label>
        <input matInput type="email" formControlName="email" placeholder="e.g. john@example.com" />
        <mat-icon matSuffix>email</mat-icon>
        <mat-error *ngIf="registerForm.get('email')?.hasError('required')">Email is required</mat-error>
        <mat-error *ngIf="registerForm.get('email')?.hasError('email')">Please enter a valid email address</mat-error>
      </mat-form-field>

      <mat-form-field appearance="fill" class="form-field">
        <mat-label>Password</mat-label>
        <input matInput [type]="hidePassword ? 'password' : 'text'" formControlName="password" />
        <button type="button" mat-icon-button matSuffix (click)="hidePassword = !hidePassword" [attr.aria-label]="'Hide password'" [attr.aria-pressed]="hidePassword">
          <mat-icon>{{hidePassword ? 'visibility_off' : 'visibility'}}</mat-icon>
        </button>
        <mat-error *ngIf="registerForm.get('password')?.hasError('required')">Password is required</mat-error>
        <mat-error *ngIf="registerForm.get('password')?.hasError('minlength')">Password must be at least 6 characters</mat-error>
      </mat-form-field>

      <button mat-raised-button color="primary" type="submit" class="submit-btn" [disabled]="registerForm.invalid">
        Create Account
      </button>
    </form>
  `,
  styles: [`
    .register-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
      width: 100%;
    }
    .form-field {
      width: 100%;
    }
    .submit-btn {
      padding: 24px;
      font-size: 16px;
      font-weight: 600;
      border-radius: 8px;
      background: linear-gradient(135deg, #8b5cf6 0%, #d946ef 100%) !important;
      color: white !important;
      border: none;
      box-shadow: 0 4px 14px 0 rgba(139, 92, 246, 0.4) !important;
      transition: all 0.3s ease;
    }
    .submit-btn:hover:not([disabled]) {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px 0 rgba(139, 92, 246, 0.6) !important;
    }
    .submit-btn[disabled] {
      background: rgba(255, 255, 255, 0.05) !important;
      color: rgba(255, 255, 255, 0.3) !important;
      box-shadow: none !important;
    }
  `]
})
export class RegisterForm {
  @Output() submitForm = new EventEmitter<any>();

  protected readonly registerForm: FormGroup;
  protected hidePassword = true;

  constructor(private fb: FormBuilder) {
    this.registerForm = this.fb.group({
      username: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  protected onSubmit(): void {
    if (this.registerForm.valid) {
      this.submitForm.emit(this.registerForm.value);
    }
  }
}
