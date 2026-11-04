import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Application, ApplicationQuery, ApplicationStatus, CreateApplication, PagedApplications } from '../models/application';

@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/applications`;

  getPage(query: ApplicationQuery): Observable<PagedApplications> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search.trim()) {
      params = params.set('search', query.search.trim());
    }
    if (query.status !== 'All') {
      params = params.set('status', query.status);
    }

    return this.http.get<PagedApplications>(this.baseUrl, { params });
  }

  getById(id: number): Observable<Application> {
    return this.http.get<Application>(`${this.baseUrl}/${id}`);
  }

  create(app: CreateApplication): Observable<Application> {
    return this.http.post<Application>(this.baseUrl, app);
  }

  update(id: number, app: Application): Observable<Application> {
    return this.http.put<Application>(`${this.baseUrl}/${id}`, app);
  }

  patchStatus(id: number, status: ApplicationStatus): Observable<Application> {
    return this.http.patch<Application>(`${this.baseUrl}/${id}/status`, { status });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}