import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface AuthResponse {
  token: string;
  refreshToken: string;
  username: string;
  email: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5000/api/auth';
  
  // Signals for user state
  private readonly _currentUser = signal<{ username: string; email: string } | null>(null);
  public readonly currentUser = computed(() => this._currentUser());
  public readonly isLoggedIn = computed(() => this._currentUser() !== null);

  constructor(private http: HttpClient) {
    const token = localStorage.getItem('token');
    const username = localStorage.getItem('username');
    const email = localStorage.getItem('email');

    if (token && username && email) {
      this._currentUser.set({ username, email });
    }
  }

  register(username: string, email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, { username, email, password }).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, password }).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      tap({
        next: () => this.clearLocalStorageAndState(),
        error: () => this.clearLocalStorageAndState()
      })
    );
  }

  public clearLocalStorageAndState(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('username');
    localStorage.removeItem('email');
    this._currentUser.set(null);
  }

  private handleAuthSuccess(res: AuthResponse): void {
    localStorage.setItem('token', res.token);
    localStorage.setItem('refreshToken', res.refreshToken);
    localStorage.setItem('username', res.username);
    localStorage.setItem('email', res.email);
    this._currentUser.set({ username: res.username, email: res.email });
  }
}
