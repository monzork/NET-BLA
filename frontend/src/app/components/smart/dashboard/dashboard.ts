import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { TaskService, TaskDto, CreateTaskDto, UpdateTaskDto } from '../../../services/task.service';
import { TaskList } from '../../dumb/task-list/task-list';
import { TaskFormDialog } from '../../dumb/task-form-dialog/task-form-dialog';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
  selector: 'app-dashboard-container',
  standalone: true,
  imports: [
    CommonModule,
    TaskList,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatToolbarModule
  ],
  template: `
    <div class="dashboard-page">
      <mat-toolbar class="dashboard-navbar glass-panel">
        <span class="nav-logo gradient-text">TaskSphere</span>
        <div class="spacer"></div>
        <div class="user-profile" *ngIf="currentUser()">
          <mat-icon class="user-avatar">account_circle</mat-icon>
          <span class="username-display">{{currentUser()?.username}}</span>
        </div>
        <button mat-icon-button (click)="onLogout()" matTooltip="Logout" class="logout-btn">
          <mat-icon>logout</mat-icon>
        </button>
      </mat-toolbar>

      <main class="dashboard-content">
        <div class="content-header">
          <div>
            <h2 class="welcome-title">My Workspace</h2>
            <p class="subtle-text">Create, organize, and accomplish your tasks.</p>
          </div>
          <button mat-raised-button color="primary" (click)="openCreateDialog()" class="new-task-btn">
            <mat-icon>add</mat-icon>
            Create Task
          </button>
        </div>

        <div class="error-banner" *ngIf="errorMessage()">
          {{errorMessage()}}
        </div>

        <div class="spinner-wrapper" *ngIf="isLoading()">
          <mat-spinner diameter="50"></mat-spinner>
        </div>

        <app-task-list 
          *ngIf="!isLoading()" 
          [tasks]="tasks()" 
          (toggleStatus)="onToggleStatus($event)"
          (editTask)="openEditDialog($event)"
          (deleteTask)="onDeleteTask($event)">
        </app-task-list>
      </main>
    </div>
  `,
  styles: [`
    .dashboard-page {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }
    .dashboard-navbar {
      height: 70px;
      padding: 0 40px;
      display: flex;
      align-items: center;
      background: rgba(15, 12, 30, 0.4) !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05);
      backdrop-filter: blur(12px);
      position: sticky;
      top: 0;
      z-index: 1000;
    }
    .nav-logo {
      font-size: 24px;
      font-weight: 700;
      letter-spacing: 0.5px;
    }
    .spacer {
      flex: 1 1 auto;
    }
    .user-profile {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-right: 16px;
      font-size: 14px;
      font-weight: 500;
      color: #e2e8f0;
    }
    .user-avatar {
      font-size: 24px;
      width: 24px;
      height: 24px;
      color: #c084fc;
    }
    .logout-btn {
      color: #94a3b8;
      transition: color 0.2s ease;
    }
    .logout-btn:hover {
      color: #ef4444;
      background: rgba(239, 68, 68, 0.05);
    }
    .dashboard-content {
      flex-grow: 1;
      padding: 40px;
      max-width: 1200px;
      width: 100%;
      margin: 0 auto;
      box-sizing: border-box;
    }
    .content-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 32px;
    }
    .welcome-title {
      font-family: 'Outfit', sans-serif;
      font-size: 28px;
      font-weight: 600;
      margin: 0 0 6px 0;
      color: #f1f1f7;
    }
    .new-task-btn {
      padding: 20px 24px;
      font-weight: 600;
      border-radius: 28px;
      background: linear-gradient(135deg, #8b5cf6 0%, #d946ef 100%) !important;
      color: white !important;
      border: none;
      box-shadow: 0 4px 14px 0 rgba(139, 92, 246, 0.3) !important;
      transition: all 0.3s ease;
    }
    .new-task-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px 0 rgba(139, 92, 246, 0.5) !important;
    }
    .spinner-wrapper {
      display: flex;
      justify-content: center;
      padding: 60px 0;
    }
    .error-banner {
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.2);
      color: #fca5a5;
      padding: 12px;
      border-radius: 8px;
      font-size: 14px;
      margin-bottom: 24px;
      text-align: center;
    }
  `]
})
export class TaskDashboardContainer implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly taskService = inject(TaskService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly currentUser = this.authService.currentUser;
  protected readonly tasks = signal<TaskDto[]>([]);
  protected readonly isLoading = signal<boolean>(false);
  protected readonly errorMessage = signal<string>('');

  constructor() {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.loadTasks();
  }

  protected onLogout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  private loadTasks(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.taskService.getAll().subscribe({
      next: (data) => {
        this.tasks.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Could not load tasks. Please verify your server connection.');
      }
    });
  }

  protected openCreateDialog(): void {
    const dialogRef = this.dialog.open(TaskFormDialog, {
      width: '500px',
      data: { task: null }
    });

    dialogRef.afterClosed().subscribe((result: any) => {
      if (result) {
        this.createTask(result);
      }
    });
  }

  protected openEditDialog(task: TaskDto): void {
    const dialogRef = this.dialog.open(TaskFormDialog, {
      width: '500px',
      data: { task }
    });

    dialogRef.afterClosed().subscribe((result: any) => {
      if (result) {
        this.updateTask(task.id, result);
      }
    });
  }

  private createTask(formData: any): void {
    this.isLoading.set(true);
    const dto: CreateTaskDto = {
      title: formData.title,
      description: formData.description,
      status: formData.status,
      dueDate: formData.dueDate ? new Date(formData.dueDate).toISOString() : null
    };

    this.taskService.create(dto).subscribe({
      next: () => {
        this.loadTasks();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.error || 'Failed to create task.');
      }
    });
  }

  private updateTask(id: string, formData: any): void {
    this.isLoading.set(true);
    const dto: UpdateTaskDto = {
      title: formData.title,
      description: formData.description,
      status: formData.status,
      dueDate: formData.dueDate ? new Date(formData.dueDate).toISOString() : null
    };

    this.taskService.update(id, dto).subscribe({
      next: () => {
        this.loadTasks();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.error || 'Failed to update task.');
      }
    });
  }

  protected onToggleStatus(event: { task: TaskDto, newStatus: string }): void {
    this.isLoading.set(true);
    const dto: UpdateTaskDto = {
      title: event.task.title,
      description: event.task.description,
      status: event.newStatus,
      dueDate: event.task.dueDate
    };

    this.taskService.update(event.task.id, dto).subscribe({
      next: () => {
        this.loadTasks();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.error || 'Failed to toggle status.');
      }
    });
  }

  protected onDeleteTask(id: string): void {
    if (confirm('Are you sure you want to delete this task?')) {
      this.isLoading.set(true);
      this.taskService.delete(id).subscribe({
        next: () => {
          this.loadTasks();
        },
        error: () => {
          this.isLoading.set(false);
          this.errorMessage.set('Failed to delete task.');
        }
      });
    }
  }
}
