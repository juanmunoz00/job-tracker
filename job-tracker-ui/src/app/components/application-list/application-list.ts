import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { ApplicationService } from '../../services/application.service';
import { ApplicationStatus, PagedApplications, STATUSES, STATUS_LABELS } from '../../models/application';
import { StatusBadge } from '../status-badge/status-badge';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-application-list',
  imports: [DatePipe, RouterLink, StatusBadge],
  templateUrl: './application-list.html',
  styleUrl: './application-list.scss'
})
export class ApplicationList {
  private readonly applicationService = inject(ApplicationService);
    protected readonly auth = inject(AuthService);

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly pageSizes = [5, 10, 20];

  // What the user asked for
  protected readonly search = signal('');
  protected readonly statusFilter = signal<ApplicationStatus | 'All'>('All');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(5);

  // What the server answered
  protected readonly result = signal<PagedApplications | null>(null);
  protected readonly loading = signal(true);      // first load only
  protected readonly refreshing = signal(false);  // later loads
  protected readonly error = signal<string | null>(null);

  protected readonly counts = computed(() => this.result()?.statusCounts ?? {});

  protected readonly visibleStatuses = computed(() =>
    STATUSES.filter(status => (this.counts()[status] ?? 0) > 0 || status === this.statusFilter())
  );

  protected readonly rangeStart = computed(() => {
    const r = this.result();
    return !r || r.totalCount === 0 ? 0 : (r.page - 1) * r.pageSize + 1;
  });

  protected readonly rangeEnd = computed(() => {
    const r = this.result();
    return r ? Math.min(r.page * r.pageSize, r.totalCount) : 0;
  });

  // Delete confirmation
  protected readonly confirmingId = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  constructor() {
    // Search box: wait for a pause in typing, ignore repeats, then reload from page 1
    this.searchInput$.pipe(
      map(text => text.trim()),
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed()
    ).subscribe(text => {
      this.search.set(text);
      this.page.set(1);
      this.reload();
    });

    // Every reload: call the API; a newer reload cancels an older one still in flight
    this.reload$.pipe(
      tap(() => this.refreshing.set(true)),
      switchMap(() =>
        this.applicationService.getPage({
          search: this.search(),
          status: this.statusFilter(),
          page: this.page(),
          pageSize: this.pageSize()
        }).pipe(catchError(() => of(null)))
      ),
      takeUntilDestroyed()
    ).subscribe(result => {
      this.loading.set(false);
      this.refreshing.set(false);

      if (!result) {
        this.error.set('Could not load applications. Please try again.');
        return;
      }
      this.error.set(null);

      // The page became empty (e.g. last row on it was deleted): step back one page
      if (result.items.length === 0 && result.page > 1) {
        this.page.set(result.page - 1);
        this.reload();
        return;
      }

      this.result.set(result);
    });

    this.reload();
  }

  private reload(): void {
    this.reload$.next();
  }

  protected onSearch(text: string): void {
    this.searchInput$.next(text);
  }

  protected clearSearch(input: HTMLInputElement): void {
    input.value = '';
    this.searchInput$.next('');
  }

  protected setFilter(filter: ApplicationStatus | 'All'): void {
    this.statusFilter.set(filter);
    this.page.set(1);
    this.reload();
  }

  protected setPageSize(size: string): void {
    this.pageSize.set(Number(size));
    this.page.set(1);
    this.reload();
  }

  protected goToPage(target: number): void {
    const total = this.result()?.totalPages ?? 1;
    if (target < 1 || target > total) return;
    this.page.set(target);
    this.reload();
  }

  protected askDelete(id: number): void {
    this.deleteError.set(null);
    this.confirmingId.set(id);
  }

  protected cancelDelete(): void {
    this.confirmingId.set(null);
  }

  protected confirmDelete(id: number): void {
    this.deletingId.set(id);

    this.applicationService.delete(id).subscribe({
      next: () => {
        this.confirmingId.set(null);
        this.deletingId.set(null);
        this.reload();
      },
      error: () => {
        this.deleteError.set('Could not delete the application. Please try again.');
        this.deletingId.set(null);
      }
    });
  }
}