import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { TaskDto } from '../../../services/task.service';

export function futureDateValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    const inputDate = new Date(control.value);
    const today = new Date();
    // Reset hours to compare dates only
    today.setHours(0, 0, 0, 0);
    return inputDate < today ? { pastDate: true } : null;
  };
}

@Component({
  selector: 'app-task-form-dialog',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule
  ],
  template: `
    <h2 mat-dialog-title class="dialog-title">{{isEditMode ? 'Edit Task' : 'New Task'}}</h2>
    <mat-dialog-content class="dialog-content">
      <form [formGroup]="taskForm" class="task-form">
        <mat-form-field appearance="fill" class="form-field">
          <mat-label>Task Title</mat-label>
          <input matInput formControlName="title" placeholder="What needs to be done?" />
          <mat-error *ngIf="taskForm.get('title')?.hasError('required')">Title is required</mat-error>
        </mat-form-field>

        <mat-form-field appearance="fill" class="form-field">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" rows="3" placeholder="Add more details..."></textarea>
        </mat-form-field>

        <div class="row">
          <mat-form-field appearance="fill" class="form-field half-width">
            <mat-label>Status</mat-label>
            <mat-select formControlName="status">
              <mat-option value="Pending">Pending</mat-option>
              <mat-option value="InProgress">In Progress</mat-option>
              <mat-option value="Completed">Completed</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="fill" class="form-field half-width">
            <mat-label>Due Date</mat-label>
            <input matInput [matDatepicker]="picker" formControlName="dueDate" />
            <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
            <mat-datepicker #picker></mat-datepicker>
            <mat-error *ngIf="taskForm.get('dueDate')?.hasError('pastDate')">Due date cannot be in the past</mat-error>
          </mat-form-field>
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end" class="dialog-actions">
      <button mat-button (click)="onCancel()" class="cancel-btn">Cancel</button>
      <button mat-raised-button color="primary" (click)="onSubmit()" [disabled]="taskForm.invalid" class="save-btn">
        {{isEditMode ? 'Save Changes' : 'Create Task'}}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-title {
      font-family: 'Outfit', sans-serif;
      font-weight: 600;
      color: #f1f1f7;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05);
      padding-bottom: 12px;
    }
    .dialog-content {
      padding-top: 20px !important;
      min-width: 400px;
    }
    .task-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    .form-field {
      width: 100%;
    }
    .row {
      display: flex;
      gap: 16px;
    }
    .half-width {
      flex: 1;
    }
    .dialog-actions {
      border-top: 1px solid rgba(255, 255, 255, 0.05);
      padding: 12px 24px !important;
    }
    .cancel-btn {
      color: #94a3b8 !important;
    }
    .save-btn {
      border-radius: 8px;
      font-weight: 600;
      background: linear-gradient(135deg, #8b5cf6 0%, #d946ef 100%) !important;
      color: white !important;
      box-shadow: 0 4px 14px 0 rgba(139, 92, 246, 0.3) !important;
      border: none;
    }
    .save-btn[disabled] {
      background: rgba(255, 255, 255, 0.05) !important;
      color: rgba(255, 255, 255, 0.3) !important;
      box-shadow: none !important;
    }
  `]
})
export class TaskFormDialog implements OnInit {
  protected readonly taskForm: FormGroup;
  protected isEditMode = false;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<TaskFormDialog>,
    @Inject(MAT_DIALOG_DATA) private data: { task: TaskDto | null }
  ) {
    this.taskForm = this.fb.group({
      title: ['', [Validators.required]],
      description: [''],
      status: ['Pending', [Validators.required]],
      dueDate: [null as Date | null, [futureDateValidator()]]
    });
  }

  ngOnInit(): void {
    if (this.data && this.data.task) {
      this.isEditMode = true;
      const t = this.data.task;
      this.taskForm.patchValue({
        title: t.title,
        description: t.description,
        status: t.status,
        dueDate: t.dueDate ? new Date(t.dueDate) : null
      });
    }
  }

  protected onCancel(): void {
    this.dialogRef.close();
  }

  protected onSubmit(): void {
    if (this.taskForm.valid) {
      this.dialogRef.close(this.taskForm.value);
    }
  }
}
