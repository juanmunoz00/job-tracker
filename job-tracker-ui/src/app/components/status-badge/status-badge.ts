import { Component, computed, input } from '@angular/core';
import { ApplicationStatus, STATUS_LABELS } from '../../models/application';

@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss'
})
export class StatusBadge {
  readonly status = input.required<ApplicationStatus>();

  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
  protected readonly dotClass = computed(() => `dot dot--${this.status().toLowerCase()}`);
}