import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './services/auth.service';
import { Router } from '@angular/router';
import { catchError, throwError, switchMap, BehaviorSubject, filter, take } from 'rxjs';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const addToken = (request: typeof req, token: string) => {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  };

  const accessToken = localStorage.getItem('token');
  let authReq = req;
  if (accessToken) {
    authReq = addToken(req, accessToken);
  }

  return next(authReq).pipe(
    catchError((error) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !req.url.includes('/api/auth/login') &&
        !req.url.includes('/api/auth/register') &&
        !req.url.includes('/api/auth/refresh')
      ) {
        if (!isRefreshing) {
          isRefreshing = true;
          refreshTokenSubject.next(null);

          return authService.refreshToken().pipe(
            switchMap((res) => {
              isRefreshing = false;
              refreshTokenSubject.next(res.token);
              return next(addToken(req, res.token));
            }),
            catchError((err) => {
              isRefreshing = false;
              authService.clearLocalStorageAndState();
              router.navigate(['/login']);
              return throwError(() => err);
            })
          );
        } else {
          return refreshTokenSubject.pipe(
            filter(token => token !== null),
            take(1),
            switchMap(token => next(addToken(req, token!)))
          );
        }
      }
      return throwError(() => error);
    })
  );
};
