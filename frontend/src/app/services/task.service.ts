import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TaskDto {
  id: string;
  title: string;
  description: string;
  status: string;
  dueDate: string | null;
  userId: string;
  createdAt: string;
}

export interface CreateTaskDto {
  title: string;
  description: string;
  status: string;
  dueDate: string | null;
}

export interface UpdateTaskDto {
  title: string;
  description: string;
  status: string;
  dueDate: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly apiUrl = 'http://localhost:5000/api/tasks';

  constructor(private http: HttpClient) {}

  getAll(): Observable<TaskDto[]> {
    return this.http.get<TaskDto[]>(this.apiUrl);
  }

  getById(id: string): Observable<TaskDto> {
    return this.http.get<TaskDto>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateTaskDto): Observable<TaskDto> {
    return this.http.post<TaskDto>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateTaskDto): Observable<TaskDto> {
    return this.http.put<TaskDto>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
