import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ApplicationService } from '../../services/application.service';
import { Application, ApplicationStatus, STATUSES, STATUS_LABELS } from '../../models/application';

function today(): string {
  const d = new Date();
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 10);
}

@Component({
  selector: 'app-application-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './application-form.html',
  styleUrl: './application-form.scss'
})
export class ApplicationForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly applicationService = inject(ApplicationService);
  private readonly router = inject(Router);

  readonly id = input<string>();
  protected readonly isEdit = computed(() => !!this.id());

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly statuses = STATUSES;
  protected readonly statusLabels = STATUS_LABELS;

  private original: Application | null = null;

  protected readonly form = this.fb.nonNullable.group({
    company:     ['', [Validators.required, Validators.maxLength(100)]],
    position:    ['', [Validators.required, Validators.maxLength(100)]],
    country:     ['USA', [Validators.required, Validators.maxLength(60)]],
    appliedDate: [today(), Validators.required],
    status:      ['Applied' as ApplicationStatus, Validators.required]
  });

  ngOnInit(): void {
    const id = this.id();
    if (!id) {
      return;   // /add — nothing to load
    }

    this.loading.set(true);
    this.applicationService.getById(Number(id)).subscribe({
      next: (app) => {
        this.original = app;
        this.form.patchValue({
          company: app.company,
          position: app.position,
          country: app.country,
          appliedDate: app.appliedDate,
          status: app.status
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load this application.');
        this.loading.set(false);
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    let request$: Observable<unknown>;

    if (this.original) {
      const statusChanged = v.status !== this.original.status;
      request$ = this.applicationService.update(this.original.id, {
        ...this.original,
        company: v.company,
        position: v.position,
        country: v.country,
        appliedDate: v.appliedDate,
        status: v.status,
        statusDate: statusChanged ? new Date().toISOString() : this.original.statusDate
      });
    } else {
      request$ = this.applicationService.create({
        company: v.company,
        position: v.position,
        country: v.country,
        appliedDate: v.appliedDate
      });
    }

    request$.subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.error.set('Could not save the application. Please try again.');
        this.saving.set(false);
      }
    });
  }
}