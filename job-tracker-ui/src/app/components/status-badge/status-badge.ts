import { Component, computed, input } from '@angular/core';
import { ApplicationStatus } from '../../models/application';

const LABELS: Record<ApplicationStatus, string> = {
  Applied: 'Applied',
  PhoneScreen: 'Phone screen',
  Interview: 'Interview',
  TakeHome: 'Take-home',
  Offer: 'Offer',
  Rejected: 'Rejected',
  Withdrawn: 'Withdrawn'
};

@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss'
})
export class StatusBadge {
  readonly status = input.required<ApplicationStatus>();

  protected readonly label = computed(() => LABELS[this.status()]);
  protected readonly cssClass = computed(() => `badge badge--${this.status().toLowerCase()}`);
}