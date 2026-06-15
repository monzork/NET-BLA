import { Component, EventEmitter, Input, Output, signal, computed, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TaskDto } from '../../../services/task.service';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatInputModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    FormsModule
  ],
  template: `
    <div class="task-list-container">
      <div class="filters-bar">
        <mat-form-field appearance="outline" class="search-field">
          <mat-label>Search tasks...</mat-label>
          <input matInput [(ngModel)]="searchQuery" (ngModelChange)="onSearchChange($event)" placeholder="Search by title or description" />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        <mat-chip-listbox (change)="onFilterChange($event.value)" class="filter-chips" defaultValue="all">
          <mat-chip-option value="all">All Tasks</mat-chip-option>
          <mat-chip-option value="Pending">Pending</mat-chip-option>
          <mat-chip-option value="InProgress">In Progress</mat-chip-option>
          <mat-chip-option value="Completed">Completed</mat-chip-option>
        </mat-chip-listbox>
      </div>

      <div class="tasks-grid" *ngIf="filteredTasks().length > 0; else noTasks">
        <mat-card *ngFor="let task of filteredTasks()" class="task-card glass-panel" [ngClass]="task.status.toLowerCase()">
          <mat-card-header>
            <div class="card-header-layout">
              <mat-card-title class="task-title">{{task.title}}</mat-card-title>
              <span class="status-indicator" [ngClass]="task.status.toLowerCase()">
                {{getStatusLabel(task.status)}}
              </span>
            </div>
            <mat-card-subtitle class="due-date subtle-text" *ngIf="task.dueDate">
              <mat-icon class="date-icon">event</mat-icon>
              Due: {{task.dueDate | date:'mediumDate'}}
            </mat-card-subtitle>
          </mat-card-header>
          
          <mat-card-content class="task-desc">
            <p>{{task.description || 'No description provided.'}}</p>
          </mat-card-content>

          <mat-card-actions class="card-actions">
            <!-- Cycle Status Toggle -->
            <button mat-icon-button (click)="cycleStatus(task)" matTooltip="Update Status" class="action-btn status-btn">
              <mat-icon>{{getStatusIcon(task.status)}}</mat-icon>
            </button>
            <div class="spacer"></div>
            <button mat-icon-button color="accent" (click)="editTask.emit(task)" matTooltip="Edit Task" class="action-btn edit-btn">
              <mat-icon>edit</mat-icon>
            </button>
            <button mat-icon-button color="warn" (click)="deleteTask.emit(task.id)" matTooltip="Delete Task" class="action-btn delete-btn">
              <mat-icon>delete</mat-icon>
            </button>
          </mat-card-actions>
        </mat-card>
      </div>

      <div #scrollAnchor class="scroll-anchor" [style.display]="hasMore ? 'block' : 'none'">
        <div class="loading-more-spinner" *ngIf="isLoadingMore">
          <mat-spinner diameter="30"></mat-spinner>
          <span class="subtle-text">Loading more tasks...</span>
        </div>
      </div>

      <ng-template #noTasks>
        <div class="no-tasks-state glass-panel">
          <mat-icon class="empty-icon">task_alt</mat-icon>
          <h3>No tasks found</h3>
          <p class="subtle-text">Create a new task or try adjusting your search filters.</p>
        </div>
      </ng-template>
    </div>
  `,
  styles: [`
    .task-list-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }
    .filters-bar {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
    }
    .search-field {
      flex-grow: 1;
      max-width: 400px;
      margin-right: 24px;
    }
    ::ng-deep .search-field .mat-mdc-text-field-wrapper {
      background-color: rgba(255, 255, 255, 0.02) !important;
      border-radius: 28px !important;
      padding: 0 20px !important;
    }
    ::ng-deep .search-field input.mat-mdc-input-element {
      padding: 12px 0 !important;
    }
    ::ng-deep .search-field .mat-mdc-form-field-icon-suffix {
      padding: 6px !important;
    }
    .tasks-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 24px;
    }
    .task-card {
      display: flex;
      flex-direction: column;
      height: 220px;
      justify-content: space-between;
      border-left: 4px solid #8b5cf6 !important; /* Pending color */
      background: rgba(255, 255, 255, 0.02) !important;
    }
    .task-card.inprogress {
      border-left-color: #3b82f6 !important;
    }
    .task-card.completed {
      border-left-color: #10b981 !important;
      opacity: 0.8;
    }
    .card-header-layout {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      width: 100%;
      margin-bottom: 6px;
    }
    .task-title {
      font-size: 18px;
      font-weight: 600;
      color: #f1f1f7;
    }
    .due-date {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 13px;
    }
    .date-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }
    .status-indicator {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      padding: 2px 8px;
      border-radius: 12px;
      letter-spacing: 0.5px;
      background: rgba(139, 92, 246, 0.1);
      color: #a78bfa;
    }
    .status-indicator.inprogress {
      background: rgba(59, 130, 246, 0.1);
      color: #60a5fa;
    }
    .status-indicator.completed {
      background: rgba(16, 185, 129, 0.1);
      color: #34d399;
    }
    .task-desc {
      flex-grow: 1;
      font-size: 14px;
      line-height: 1.5;
      color: #cbd5e1;
      margin-top: 12px;
      overflow: hidden;
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
    }
    .card-actions {
      border-top: 1px solid rgba(255, 255, 255, 0.05);
      padding: 12px 20px !important;
      margin: 0 -16px -16px -16px;
      display: flex;
      align-items: center;
    }
    .spacer {
      flex: 1 1 auto;
    }
    .action-btn {
      transition: all 0.2s ease;
    }
    .action-btn:hover {
      background: rgba(255, 255, 255, 0.05);
    }
    .status-btn {
      color: #a78bfa;
    }
    .task-card.inprogress .status-btn {
      color: #60a5fa;
    }
    .task-card.completed .status-btn {
      color: #34d399;
    }
    .no-tasks-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 60px 20px;
      text-align: center;
      background: rgba(255, 255, 255, 0.02) !important;
    }
    .empty-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #64748b;
      margin-bottom: 16px;
    }
    @media (max-width: 600px) {
      .filters-bar {
        flex-direction: column;
        align-items: stretch;
        gap: 12px;
      }
      .search-field {
        max-width: 100%;
        width: 100%;
        margin-right: 0;
        margin-bottom: 8px;
      }
      .filter-chips {
        width: 100%;
      }
    }
    @media (max-width: 480px) {
      .tasks-grid {
        grid-template-columns: 1fr;
      }
      .task-card {
        height: auto;
        min-height: 200px;
      }
    }
    .scroll-anchor {
      padding: 24px 0;
      display: flex;
      justify-content: center;
      align-items: center;
      width: 100%;
    }
    .loading-more-spinner {
      display: flex;
      align-items: center;
      gap: 12px;
      color: #cbd5e1;
    }
  `]
})
export class TaskList implements AfterViewInit, OnDestroy {
  @Input() set tasks(value: TaskDto[]) {
    this._tasks.set(value);
  }
  @Input() hasMore = false;
  @Input() isLoadingMore = false;

  @Output() toggleStatus = new EventEmitter<{ task: TaskDto, newStatus: string }>();
  @Output() editTask = new EventEmitter<TaskDto>();
  @Output() deleteTask = new EventEmitter<string>();
  @Output() loadMore = new EventEmitter<void>();
  @Output() filterChanged = new EventEmitter<{ status: string, search: string }>();

  @ViewChild('scrollAnchor') scrollAnchor!: ElementRef<HTMLDivElement>;

  private readonly _tasks = signal<TaskDto[]>([]);
  protected searchQuery = '';
  protected activeFilter = 'all';
  private observer?: IntersectionObserver;
  private readonly searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  protected readonly filteredTasks = computed(() => this._tasks());

  constructor() {
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(value => {
      this.filterChanged.emit({ status: this.activeFilter, search: value });
    });
  }

  ngAfterViewInit(): void {
    this.setupIntersectionObserver();
  }

  ngOnDestroy(): void {
    if (this.observer) {
      this.observer.disconnect();
    }
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  private setupIntersectionObserver(): void {
    this.observer = new IntersectionObserver((entries) => {
      const entry = entries[0];
      if (entry.isIntersecting && this.hasMore && !this.isLoadingMore) {
        if (window.innerWidth <= 768) {
          this.loadMore.emit();
        }
      }
    }, {
      rootMargin: '100px'
    });

    if (this.scrollAnchor) {
      this.observer.observe(this.scrollAnchor.nativeElement);
    }
  }

  protected onFilterChange(value: string | undefined): void {
    this.activeFilter = value || 'all';
    this.filterChanged.emit({ status: this.activeFilter, search: this.searchQuery });
  }

  protected onSearchChange(value: string): void {
    this.searchQuery = value;
    this.searchSubject.next(value);
  }

  protected getStatusLabel(status: string): string {
    if (status === 'InProgress') return 'In Progress';
    return status;
  }

  protected getStatusIcon(status: string): string {
    switch (status) {
      case 'Pending': return 'radio_button_unchecked';
      case 'InProgress': return 'play_circle';
      case 'Completed': return 'check_circle';
      default: return 'help';
    }
  }

  protected cycleStatus(task: TaskDto): void {
    let nextStatus = 'Pending';
    if (task.status === 'Pending') nextStatus = 'InProgress';
    else if (task.status === 'InProgress') nextStatus = 'Completed';
    else if (task.status === 'Completed') nextStatus = 'Pending';

    this.toggleStatus.emit({ task, newStatus: nextStatus });
  }
}
