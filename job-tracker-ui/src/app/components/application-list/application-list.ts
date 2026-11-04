import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApplicationService } from '../../services/application.service';
import { Application, ApplicationStatus, STATUSES, STATUS_LABELS } from '../../models/application';
import { StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'app-application-list',
  imports: [DatePipe, RouterLink, StatusBadge],
  templateUrl: './application-list.html',
  styleUrl: './application-list.scss'
})
export class ApplicationList implements OnInit {
  private readonly applicationService = inject(ApplicationService);

  protected readonly applications = signal<Application[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly statusFilter = signal<ApplicationStatus | 'All'>('All');

  protected readonly counts = computed(() => {
    const result = Object.fromEntries(STATUSES.map(s => [s, 0])) as Record<ApplicationStatus, number>;
    for (const app of this.applications()) {
      result[app.status]++;
    }
    return result;
  });

  protected readonly activeStatuses = computed(() =>
    STATUSES.filter(status => this.counts()[status] > 0)
  );

  protected readonly filteredApplications = computed(() => {
    const filter = this.statusFilter();
    const apps = this.applications();
    return filter === 'All' ? apps : apps.filter(app => app.status === filter);
  });

  protected readonly confirmingId = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  ngOnInit(): void {
    this.applicationService.getAll().subscribe({
      next: (apps) => {
        this.applications.set(apps);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load applications. Please try again.');
        this.loading.set(false);
      }
    });
  }

  protected setFilter(filter: ApplicationStatus | 'All'): void {
    this.statusFilter.set(filter);
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
        this.applications.update(apps => apps.filter(app => app.id !== id));
        this.confirmingId.set(null);
        this.deletingId.set(null);
        if (this.statusFilter() !== 'All' && this.counts()[this.statusFilter() as ApplicationStatus] === 0) {
          this.statusFilter.set('All');
        }
      },
      error: () => {
        this.deleteError.set('Could not delete the application. Please try again.');
        this.deletingId.set(null);
      }
    });
  }
}