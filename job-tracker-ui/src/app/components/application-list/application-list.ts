import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApplicationService } from '../../services/application.service';
import { Application, ApplicationStatus, STATUSES, STATUS_LABELS } from '../../models/application';
import { StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'app-application-list',
  imports: [DatePipe, FormsModule, StatusBadge],
  templateUrl: './application-list.html',
  styleUrl: './application-list.scss'
})
export class ApplicationList implements OnInit {
  private readonly applicationService = inject(ApplicationService);

  protected readonly applications = signal<Application[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly statuses = STATUSES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly statusFilter = signal<ApplicationStatus | 'All'>('All');

  protected readonly filteredApplications = computed(() => {
    const filter = this.statusFilter();
    const apps = this.applications();
    return filter === 'All' ? apps : apps.filter(app => app.status === filter);
  });

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
}